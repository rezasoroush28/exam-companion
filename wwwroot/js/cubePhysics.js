const BOARD_WIDTH = 390;
const BOARD_HEIGHT = 620;
const FLOOR_TOP = 604;
const CATEGORY_CUBE = 0x0001;
const CATEGORY_FLOOR = 0x0002;
const CATEGORY_WALL = 0x0004;
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
export function startResultSequence(boardId, cubes) { getBoard(boardId).startResultSequence(cubes); }
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
        this.result = null;
        this.audioContext = null;
        this.vacuumAudio = null;
        this.reducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;

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

        this.floor = M.Bodies.rectangle(BOARD_WIDTH / 2, FLOOR_TOP + 18, BOARD_WIDTH + 40, 36,
            this.boundaryOptions(CATEGORY_FLOOR));
        this.leftWall = M.Bodies.rectangle(-12, BOARD_HEIGHT / 2, 24, BOARD_HEIGHT * 2,
            this.boundaryOptions(CATEGORY_WALL));
        this.rightWall = M.Bodies.rectangle(BOARD_WIDTH + 12, BOARD_HEIGHT / 2, 24, BOARD_HEIGHT * 2,
            this.boundaryOptions(CATEGORY_WALL));
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

    boundaryOptions(category) {
        return { isStatic: true, restitution: 0.05, friction: 0.9,
            collisionFilter: { category, mask: CATEGORY_CUBE }, render: { visible: false } };
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
            collisionFilter: { category: CATEGORY_CUBE, mask: CATEGORY_CUBE | CATEGORY_FLOOR | CATEGORY_WALL },
            chamfer: { radius: Math.min(7, size * 0.08) },
            render: { visible: false }
        });
        M.Body.setAngle(body, this.degreesToRadians(-8 + Math.random() * 16));
        M.Body.setVelocity(body, { x: -0.45 + Math.random() * 0.9, y: 0 });

        const cube = {
            cubeId, questionId, topicId, importance,
            difficulty: String(difficulty).toLowerCase(),
            color: COLORS[String(difficulty).toLowerCase()] || COLORS.medium,
            size, body, marker: null, isCorrect: null, impacted: false, impactAt: 0,
            resultMode: null, removalNotified: false,
            stableSince: null, spawnedAt: performance.now(), notified: false
        };
        this.cubes.set(String(cubeId), cube);
        this.active = cube;
        M.Composite.add(this.engine.world, body);
        this.setDebug("Falling", body.speed, body.angularSpeed);
    }

    markCubeAnswer(cubeId, isCorrect) {
        const cube = this.cubes.get(String(cubeId));
        if (cube) {
            cube.marker = isCorrect ? "✓" : "×";
            cube.isCorrect = Boolean(isCorrect);
        }
        this.unlockAudio();
    }

    resetBoard() {
        const M = window.Matter;
        for (const cube of this.cubes.values()) M.Composite.remove(this.engine.world, cube.body);
        this.cubes.clear();
        this.active = null;
        this.lastSpawnX = null;
        this.cancelResultTimers();
        this.stopVacuumAudio();
        this.result = null;
        this.engine.gravity.y = 1.75;
        this.element.classList.remove("physics-impact");
        this.setDebug("Ready", 0, 0);
    }

    setWireframes(enabled) {
        this.render.options.wireframes = Boolean(enabled);
    }

    startResultSequence(items) {
        if (this.result || this.disposed) return;
        const M = window.Matter;
        const records = Array.from(items || []);
        for (const item of records) {
            const cube = this.cubes.get(String(item.cubeId));
            if (!cube) continue;
            cube.isCorrect = Boolean(item.isCorrect);
            cube.importanceTier = Number(item.importance) || 1;
            cube.difficulty = String(item.difficulty || cube.difficulty).toLowerCase();
        }
        const existing = records.map(x => this.cubes.get(String(x.cubeId))).filter(Boolean);
        const incorrect = existing.filter(cube => !cube.isCorrect);
        const correct = existing.filter(cube => cube.isCorrect);
        this.result = {
            state: "ChallengeFinished",
            timers: [],
            incorrectRemaining: new Set(incorrect.map(x => String(x.cubeId))),
            correctRemaining: new Set(correct.map(x => String(x.cubeId))),
            pendingIncorrectReleases: incorrect.length,
            pendingVacuumStarts: correct.length,
            vacuumActive: new Set(),
            completed: false
        };
        this.active = null;
        this.setResultState("PreparingFailureDrop");
        for (const cube of correct) {
            M.Sleeping.set(cube.body, false);
            M.Body.setStatic(cube.body, true);
        }
        if (incorrect.length === 0) {
            this.schedule(() => this.beginVacuum(), 80);
            return;
        }
        this.setResultState("DroppingIncorrectCubes");
        incorrect.sort((a, b) => b.body.position.y - a.body.position.y).forEach((cube, index) => {
            this.schedule(() => this.releaseIncorrect(cube), index * (45 + Math.random() * 55));
        });
    }

    releaseIncorrect(cube) {
        if (!this.result || !this.cubes.has(String(cube.cubeId))) return;
        const M = window.Matter;
        this.result.pendingIncorrectReleases--;
        cube.resultMode = "incorrect-drop";
        M.Sleeping.set(cube.body, false);
        cube.body.collisionFilter.mask = CATEGORY_CUBE | CATEGORY_WALL;
        M.Body.setVelocity(cube.body, { x: cube.body.velocity.x * 0.4, y: 5.5 + Math.random() * 2 });
        M.Body.setAngularVelocity(cube.body, (-0.045 + Math.random() * 0.09) * (this.reducedMotion ? 0.25 : 1));
        this.playIncorrectSound();
        this.schedule(() => {
            if (this.cubes.has(String(cube.cubeId))) cube.body.collisionFilter.mask = CATEGORY_WALL;
        }, 180);
    }

    beginVacuum() {
        if (!this.result || this.result.completed) return;
        if (this.result.incorrectRemaining.size > 0 || this.result.pendingIncorrectReleases > 0) return;
        if (this.result.correctRemaining.size === 0) {
            this.completeResultPhysics();
            return;
        }
        const M = window.Matter;
        this.setResultState("VacuumActive");
        this.engine.gravity.y = 0.22;
        this.startVacuumAudio();
        void this.dotNetRef.invokeMethodAsync("OnVacuumStarted").catch(() => {});
        const correct = [...this.result.correctRemaining]
            .map(id => this.cubes.get(id)).filter(Boolean)
            .sort((a, b) => a.body.position.y - b.body.position.y);
        correct.forEach((cube, index) => {
            this.schedule(() => {
                if (!this.result || !this.cubes.has(String(cube.cubeId))) return;
                this.result.pendingVacuumStarts--;
                this.result.vacuumActive.add(String(cube.cubeId));
                cube.resultMode = "vacuum";
                M.Body.setStatic(cube.body, false);
                M.Sleeping.set(cube.body, false);
                cube.body.collisionFilter.mask = 0;
                M.Body.setVelocity(cube.body, { x: 0, y: this.reducedMotion ? -16 : -11 });
                M.Body.setAngularVelocity(cube.body, this.reducedMotion ? 0 : (-0.035 + Math.random() * 0.07));
            }, index * (95 + Math.random() * 55));
        });
    }

    updateResultPhysics() {
        if (!this.result || this.result.completed) return;
        const M = window.Matter;
        for (const id of [...this.result.incorrectRemaining]) {
            const cube = this.cubes.get(id);
            if (!cube) { this.result.incorrectRemaining.delete(id); continue; }
            if (cube.resultMode === "incorrect-drop" && cube.body.bounds.min.y > BOARD_HEIGHT + 80) {
                this.result.incorrectRemaining.delete(id);
                this.removeCube(cube);
                void this.dotNetRef.invokeMethodAsync("OnIncorrectCubeRemoved", cube.cubeId).catch(() => {});
            }
        }
        if (this.result.state === "DroppingIncorrectCubes" &&
            this.result.pendingIncorrectReleases === 0 && this.result.incorrectRemaining.size === 0) {
            this.setResultState("WaitingForIncorrectCubesToExit");
            this.schedule(() => this.beginVacuum(), 150);
        }

        const targetX = BOARD_WIDTH / 2;
        for (const id of [...this.result.vacuumActive]) {
            const cube = this.cubes.get(id);
            if (!cube) { this.result.vacuumActive.delete(id); continue; }
            const dx = targetX - cube.body.position.x;
            const desiredX = Math.max(-4, Math.min(4, dx * 0.035));
            const desiredY = this.reducedMotion ? -18 : -14;
            M.Body.setVelocity(cube.body, {
                x: cube.body.velocity.x * 0.72 + desiredX * 0.28,
                y: Math.max(desiredY, cube.body.velocity.y - 0.42)
            });
            if (cube.body.bounds.max.y < -60) {
                this.result.vacuumActive.delete(id);
                this.result.correctRemaining.delete(id);
                this.removeCube(cube);
                this.playCollectSound(cube);
                void this.dotNetRef.invokeMethodAsync("OnCorrectCubeCollected", cube.cubeId).catch(() => {});
            }
        }
        if (this.result.state === "VacuumActive" && this.result.pendingVacuumStarts === 0 &&
            this.result.correctRemaining.size === 0 && this.result.vacuumActive.size === 0) {
            this.completeResultPhysics();
        }
    }

    completeResultPhysics() {
        if (!this.result || this.result.completed) return;
        this.result.completed = true;
        this.setResultState("ResultComplete");
        this.stopVacuumAudio();
        this.engine.gravity.y = 1.75;
        void this.dotNetRef.invokeMethodAsync("OnResultPhysicsCompleted").catch(() => {});
    }

    removeCube(cube) {
        window.Matter.Composite.remove(this.engine.world, cube.body);
        this.cubes.delete(String(cube.cubeId));
    }

    schedule(callback, delay) {
        const timer = window.setTimeout(callback, delay);
        this.result?.timers.push(timer);
        return timer;
    }

    cancelResultTimers() {
        if (!this.result) return;
        for (const timer of this.result.timers) window.clearTimeout(timer);
        this.result.timers.length = 0;
    }

    setResultState(state) {
        if (this.result) this.result.state = state;
        this.setDebug(state, 0, 0);
        if (this.element) {
            this.element.dataset.resultState = state;
            this.element.dataset.incorrectRemaining = String(this.result?.incorrectRemaining.size || 0);
            this.element.dataset.correctRemaining = String(this.result?.correctRemaining.size || 0);
        }
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
        if (this.result) this.updateResultPhysics();
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
        if (this.result?.state === "VacuumActive") this.drawAirflow(ctx);
        for (const cube of this.cubes.values()) this.drawCube(ctx, cube);
    }

    drawAirflow(ctx) {
        const time = performance.now() * 0.012;
        ctx.save();
        ctx.strokeStyle = "rgba(102, 164, 205, .18)";
        ctx.lineWidth = 2;
        for (let index = 0; index < 6; index++) {
            const x = 55 + index * 56 + Math.sin(time + index) * 8;
            const y = BOARD_HEIGHT - ((time * 12 + index * 93) % (BOARD_HEIGHT + 100));
            ctx.beginPath();
            ctx.moveTo(x, y + 42);
            ctx.quadraticCurveTo(x + 8, y + 20, x, y);
            ctx.stroke();
        }
        ctx.restore();
    }

    unlockAudio() {
        try {
            this.audioContext ??= new (window.AudioContext || window.webkitAudioContext)();
            if (this.audioContext.state === "suspended") void this.audioContext.resume();
        } catch { }
    }

    playIncorrectSound() {
        this.unlockAudio();
        const context = this.audioContext;
        if (!context) return;
        const oscillator = context.createOscillator();
        const gain = context.createGain();
        oscillator.type = "triangle";
        oscillator.frequency.setValueAtTime(125 + Math.random() * 22, context.currentTime);
        oscillator.frequency.exponentialRampToValueAtTime(72, context.currentTime + 0.09);
        gain.gain.setValueAtTime(0.0001, context.currentTime);
        gain.gain.exponentialRampToValueAtTime(0.07, context.currentTime + 0.008);
        gain.gain.exponentialRampToValueAtTime(0.0001, context.currentTime + 0.11);
        oscillator.connect(gain).connect(context.destination);
        oscillator.start();
        oscillator.stop(context.currentTime + 0.12);
    }

    startVacuumAudio() {
        this.unlockAudio();
        const context = this.audioContext;
        if (!context || this.vacuumAudio) return;
        const length = context.sampleRate;
        const buffer = context.createBuffer(1, length, context.sampleRate);
        const data = buffer.getChannelData(0);
        for (let i = 0; i < length; i++) data[i] = Math.random() * 2 - 1;
        const source = context.createBufferSource();
        const filter = context.createBiquadFilter();
        const gain = context.createGain();
        source.buffer = buffer;
        source.loop = true;
        filter.type = "bandpass";
        filter.frequency.value = 620;
        filter.Q.value = 0.7;
        gain.gain.setValueAtTime(0.0001, context.currentTime);
        gain.gain.exponentialRampToValueAtTime(0.035, context.currentTime + 0.1);
        source.connect(filter).connect(gain).connect(context.destination);
        source.start();
        this.vacuumAudio = { source, gain };
    }

    stopVacuumAudio() {
        const audio = this.vacuumAudio;
        const context = this.audioContext;
        if (!audio || !context) return;
        audio.gain.gain.cancelScheduledValues(context.currentTime);
        audio.gain.gain.setValueAtTime(Math.max(0.0001, audio.gain.gain.value), context.currentTime);
        audio.gain.gain.exponentialRampToValueAtTime(0.0001, context.currentTime + 0.15);
        window.setTimeout(() => { try { audio.source.stop(); } catch { } }, 170);
        this.vacuumAudio = null;
    }

    playCollectSound(cube) {
        this.unlockAudio();
        const context = this.audioContext;
        if (!context) return;
        const hardness = { easy: 1, medium: 1.4, hard: 2, veryhard: 3 }[cube.difficulty] || 1;
        const normalized = Math.min(1, ((cube.importanceTier || 1) * hardness) / 15);
        const layers = normalized > 0.58 ? 2 : 1;
        for (let layer = 0; layer < layers; layer++) {
            const oscillator = context.createOscillator();
            const gain = context.createGain();
            oscillator.type = layer ? "sine" : "triangle";
            oscillator.frequency.value = (620 - normalized * 190) * (layer ? 1.5 : 1);
            gain.gain.setValueAtTime(0.0001, context.currentTime);
            gain.gain.exponentialRampToValueAtTime(0.045 + normalized * 0.035, context.currentTime + 0.008);
            gain.gain.exponentialRampToValueAtTime(0.0001, context.currentTime + 0.13);
            oscillator.connect(gain).connect(context.destination);
            oscillator.start();
            oscillator.stop(context.currentTime + 0.14);
        }
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
        this.cancelResultTimers();
        this.stopVacuumAudio();
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
        if (this.audioContext) void this.audioContext.close().catch(() => {});
    }
}
