const BOARD_WIDTH = 390;
const BOARD_HEIGHT = 620;
const FLOOR_TOP = 604;
const COLORS = {
    easy: "#3B82F6",
    medium: "#7667D8",
    hard: "#E77732",
    veryhard: "#D83A3A"
};

const boards = new Map();
let nextBoardId = 1;

export function initializeGameBoard(element, dotNetRef, debug = false) {
    if (!window.Matter) throw new Error("Matter.js was not loaded.");
    const id = `physics-board-${nextBoardId++}`;
    boards.set(id, new CubePhysicsBoard(element, dotNetRef, debug));
    return id;
}

export function spawnCube(boardId, ...args) { getBoard(boardId).spawnCube(...args); }
export function markCubeAnswer(boardId, ...args) { getBoard(boardId).markCubeAnswer(...args); }
export function resetBoard(boardId) { getBoard(boardId).resetBoard(); }
export function setWireframes(boardId, enabled) { getBoard(boardId).setWireframes(enabled); }
export function disposeGameBoard(boardId) {
    const board = boards.get(boardId);
    if (!board) return;
    board.disposeGameBoard();
    boards.delete(boardId);
}

function getBoard(id) {
    const board = boards.get(id);
    if (!board) throw new Error("Physics board was not initialized.");
    return board;
}

class CubePhysicsBoard {
    constructor(element, dotNetRef, debug) {
        this.element = element;
        this.dotNetRef = dotNetRef;
        this.debug = debug;
        this.cubes = new Map();
        this.active = null;
        this.disposed = false;
        this.lastSpawnX = null;

        const M = window.Matter;
        this.engine = M.Engine.create({
            enableSleeping: true,
            positionIterations: 14,
            velocityIterations: 12,
            constraintIterations: 4
        });
        this.engine.gravity.y = 1.75;
        this.engine.gravity.scale = 0.001;

        this.render = M.Render.create({
            element,
            engine: this.engine,
            options: {
                width: BOARD_WIDTH,
                height: BOARD_HEIGHT,
                wireframes: false,
                background: "transparent",
                pixelRatio: Math.min(window.devicePixelRatio || 1, 2)
            }
        });
        this.render.canvas.className = "physics-canvas";

        this.floor = M.Bodies.rectangle(BOARD_WIDTH / 2, FLOOR_TOP + 18, BOARD_WIDTH + 40, 36, this.boundaryOptions());
        this.leftWall = M.Bodies.rectangle(-12, BOARD_HEIGHT / 2, 24, BOARD_HEIGHT * 2, this.boundaryOptions());
        this.rightWall = M.Bodies.rectangle(BOARD_WIDTH + 12, BOARD_HEIGHT / 2, 24, BOARD_HEIGHT * 2, this.boundaryOptions());
        M.Composite.add(this.engine.world, [this.floor, this.leftWall, this.rightWall]);

        this.runner = M.Runner.create({ delta: 1000 / 60, maxFrameTime: 1000 / 20 });
        this.onCollisionStart = event => this.handleCollisionStart(event);
        this.onAfterUpdate = () => this.afterUpdate();
        this.onAfterRender = () => this.drawCubes();
        M.Events.on(this.engine, "collisionStart", this.onCollisionStart);
        M.Events.on(this.engine, "afterUpdate", this.onAfterUpdate);
        M.Events.on(this.render, "afterRender", this.onAfterRender);
        M.Render.run(this.render);
        M.Runner.run(this.runner, this.engine);
        this.setDebug("Ready", 0, 0);
    }

    boundaryOptions() {
        return { isStatic: true, restitution: 0.05, friction: 0.9, render: { visible: false } };
    }

    spawnCube(cubeId, questionId, topicId, importance, difficulty) {
        if (this.disposed) throw new Error("Physics board is disposed.");
        if (this.active && !this.active.notified) throw new Error("A cube is already falling.");

        const M = window.Matter;
        const size = this.mapImportance(importance);
        const margin = size / 2 + 5;
        let x = margin + Math.random() * (BOARD_WIDTH - margin * 2);
        if (this.lastSpawnX !== null && Math.abs(x - this.lastSpawnX) < 38) {
            x = x < BOARD_WIDTH / 2
                ? Math.min(BOARD_WIDTH - margin, x + 58)
                : Math.max(margin, x - 58);
        }
        this.lastSpawnX = x;

        const body = M.Bodies.rectangle(x, -size / 2 - 8, size, size, {
            label: `cube:${cubeId}`,
            density: 0.0035,
            restitution: 0.08,
            friction: 0.82,
            frictionStatic: 1.1,
            frictionAir: 0.008,
            sleepThreshold: 35,
            chamfer: { radius: Math.min(7, size * 0.08) },
            render: { visible: false }
        });
        M.Body.setAngle(body, this.degreesToRadians(-8 + Math.random() * 16));
        M.Body.setVelocity(body, { x: -0.45 + Math.random() * 0.9, y: 0 });

        const cube = {
            cubeId, questionId, topicId, importance,
            difficulty: String(difficulty).toLowerCase(),
            color: COLORS[String(difficulty).toLowerCase()] || COLORS.medium,
            size, body, marker: null, impacted: false, impactAt: 0,
            stableSince: null, spawnedAt: performance.now(), notified: false
        };
        this.cubes.set(String(cubeId), cube);
        this.active = cube;
        M.Composite.add(this.engine.world, body);
        this.setDebug("Falling", body.speed, body.angularSpeed);
    }

    markCubeAnswer(cubeId, isCorrect) {
        const cube = this.cubes.get(String(cubeId));
        if (cube) cube.marker = isCorrect ? "✓" : "×";
    }

    resetBoard() {
        const M = window.Matter;
        for (const cube of this.cubes.values()) M.Composite.remove(this.engine.world, cube.body);
        this.cubes.clear();
        this.active = null;
        this.lastSpawnX = null;
        this.element.classList.remove("physics-impact");
        this.setDebug("Ready", 0, 0);
    }

    setWireframes(enabled) {
        this.render.options.wireframes = Boolean(enabled);
    }

    handleCollisionStart(event) {
        if (!this.active || this.active.notified || this.active.impacted) return;
        for (const pair of event.pairs) {
            if (pair.bodyA !== this.active.body && pair.bodyB !== this.active.body) continue;
            const strength = Math.min(1, Math.max(0, this.active.body.speed / 13));
            if (strength < 0.18) continue;
            this.active.impacted = true;
            this.active.impactAt = performance.now();
            this.setDebug("Impact", this.active.body.speed, this.active.body.angularSpeed);
            this.element.style.setProperty("--impact-strength", strength.toFixed(2));
            this.element.classList.remove("physics-impact");
            void this.element.offsetWidth;
            this.element.classList.add("physics-impact");
            window.setTimeout(() => this.element?.classList.remove("physics-impact"), 90);
            break;
        }
    }

    afterUpdate() {
        if (!this.active || this.active.notified) return;
        const M = window.Matter;
        const body = this.active.body;

        if (body.velocity.y > 18) M.Body.setVelocity(body, { x: body.velocity.x, y: 18 });
        if (Math.abs(body.velocity.x) > 3) M.Body.setVelocity(body, { x: Math.sign(body.velocity.x) * 3, y: body.velocity.y });

        const now = performance.now();
        const slow = body.speed < 0.24 && body.angularSpeed < 0.018;
        if (this.active.impacted && slow) {
            this.active.stableSince ??= now;
        } else {
            this.active.stableSince = null;
        }

        this.setDebug(this.active.impacted ? "Settling" : "Falling", body.speed, body.angularSpeed);
        if (this.active.stableSince && now - this.active.stableSince >= 250) {
            this.finishActiveCube();
        } else if (this.active.impacted && now - this.active.spawnedAt >= 2500) {
            M.Sleeping.set(body, true);
            this.finishActiveCube();
        }
    }

    finishActiveCube() {
        const cube = this.active;
        if (!cube || cube.notified) return;
        cube.notified = true;
        this.setDebug("Settled", cube.body.speed, cube.body.angularSpeed);
        this.dotNetRef.invokeMethodAsync("OnCubeSettled", {
            cubeId: cube.cubeId,
            questionId: cube.questionId,
            topicId: cube.topicId
        }).catch(() => {});
    }

    drawCubes() {
        const ctx = this.render.context;
        for (const cube of this.cubes.values()) this.drawCube(ctx, cube);
    }

    drawCube(ctx, cube) {
        const { x, y } = cube.body.position;
        const s = cube.size;
        ctx.save();
        ctx.translate(x, y);
        ctx.rotate(cube.body.angle);
        const impactAge = performance.now() - cube.impactAt;
        const squash = cube.impacted && impactAge < 100 ? 1 - Math.sin(impactAge / 100 * Math.PI) * 0.035 : 1;
        ctx.scale(1 / squash, squash);
        ctx.shadowColor = "rgba(29, 47, 70, .3)";
        ctx.shadowBlur = 9;
        ctx.shadowOffsetY = 6;
        const gradient = ctx.createLinearGradient(-s / 2, -s / 2, s / 2, s / 2);
        gradient.addColorStop(0, this.lighten(cube.color, 20));
        gradient.addColorStop(0.48, cube.color);
        gradient.addColorStop(1, this.darken(cube.color, 18));
        ctx.fillStyle = gradient;
        ctx.strokeStyle = this.darken(cube.color, 24);
        ctx.lineWidth = 2;
        ctx.beginPath();
        ctx.roundRect(-s / 2, -s / 2, s, s, Math.min(7, s * 0.08));
        ctx.fill();
        ctx.shadowColor = "transparent";
        ctx.stroke();
        ctx.strokeStyle = "rgba(255,255,255,.28)";
        ctx.lineWidth = 2;
        ctx.beginPath();
        ctx.moveTo(-s / 2 + 8, -s / 2 + 3);
        ctx.lineTo(s / 2 - 8, -s / 2 + 3);
        ctx.stroke();
        if (cube.marker) {
            ctx.fillStyle = "white";
            ctx.beginPath();
            ctx.arc(-s / 2 + 8, -s / 2 + 8, 10, 0, Math.PI * 2);
            ctx.fill();
            ctx.fillStyle = "#17365f";
            ctx.font = "bold 15px Tahoma";
            ctx.textAlign = "center";
            ctx.textBaseline = "middle";
            ctx.fillText(cube.marker, -s / 2 + 8, -s / 2 + 8);
        }
        ctx.restore();
    }

    setDebug(state, speed, angularSpeed) {
        if (!this.debug) return;
        this.element.dataset.physicsState = state;
        this.element.dataset.physicsVelocity = `${Number(speed).toFixed(2)} / ${Number(angularSpeed).toFixed(3)}`;
        const stateNode = this.element.querySelector(".debug-state");
        const velocityNode = this.element.querySelector(".debug-velocity");
        if (stateNode) stateNode.textContent = state;
        if (velocityNode) velocityNode.textContent = this.element.dataset.physicsVelocity;
    }

    mapImportance(value) {
        const v = Math.max(0, Math.min(1, Number(value) || 0));
        if (v <= 0.08) return 38;
        if (v <= 0.16) return 48;
        if (v <= 0.25) return 60;
        if (v <= 0.36) return 72;
        return 86;
    }

    degreesToRadians(value) { return value * Math.PI / 180; }
    lighten(hex, amount) { return this.adjust(hex, amount); }
    darken(hex, amount) { return this.adjust(hex, -amount); }
    adjust(hex, amount) {
        const n = parseInt(hex.slice(1), 16);
        const channel = shift => Math.max(0, Math.min(255, (n >> shift & 255) + amount));
        return `rgb(${channel(16)},${channel(8)},${channel(0)})`;
    }

    disposeGameBoard() {
        if (this.disposed) return;
        this.disposed = true;
        const M = window.Matter;
        M.Events.off(this.engine, "collisionStart", this.onCollisionStart);
        M.Events.off(this.engine, "afterUpdate", this.onAfterUpdate);
        M.Events.off(this.render, "afterRender", this.onAfterRender);
        M.Render.stop(this.render);
        M.Runner.stop(this.runner);
        M.Composite.clear(this.engine.world, false, true);
        M.Engine.clear(this.engine);
        this.render.canvas.remove();
        this.render.textures = {};
        this.dotNetRef = null;
        this.element = null;
        this.cubes.clear();
    }
}
