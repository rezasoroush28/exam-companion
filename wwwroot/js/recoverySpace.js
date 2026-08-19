let controller = null;

export function initialize(canvas, dotNetRef) {
    dispose();
    controller = new RecoverySpace(canvas, dotNetRef);
}

export function beginQuestion(stability, correctElementId) { controller?.beginQuestion(stability, correctElementId); }
export function stopQuestion() { return Math.round(controller?.stopQuestion() || 0); }
export function showCorrect(stability) { controller?.showCorrect(stability); }
export function showIncorrect() { controller?.showIncorrect(); }
export function completeTopic() { controller?.completeTopic(); }
export function dispose() { controller?.dispose(); controller = null; }

class RecoverySpace {
    constructor(canvas, dotNetRef) {
        this.canvas = canvas;
        this.ctx = canvas.getContext("2d");
        this.dotNetRef = dotNetRef;
        this.reducedMotion = matchMedia("(prefers-reduced-motion: reduce)").matches;
        this.cubes = [];
        this.particles = [];
        this.stability = 0;
        this.pulse = 0;
        this.wrongPulse = 0;
        this.topicFlash = 0;
        this.ambientShock = 0;
        this.shockX = .5;
        this.shockY = .5;
        this.waves = [];
        this.nextShockAt = performance.now() + 450;
        this.nextWaveAt = performance.now() + 240;
        this.questionActive = false;
        this.activeElapsed = 0;
        this.activeStartedAt = 0;
        this.sentHints = new Set();
        this.correctElementId = null;
        this.audio = null;
        this.disposed = false;
        this.lastFrame = performance.now();
        this.onResize = () => this.resize();
        this.onVisibility = () => this.handleVisibilityChange();
        this.onOnline = () => this.handlePauseState();
        this.onPointer = () => this.unlockAudio();
        addEventListener("resize", this.onResize);
        document.addEventListener("visibilitychange", this.onVisibility);
        addEventListener("online", this.onOnline);
        addEventListener("offline", this.onOnline);
        canvas.addEventListener("pointerdown", this.onPointer, { passive: true });
        this.resize();
        this.hintTimer = setInterval(() => this.checkHints(), 250);
        this.frame = document.hidden ? null : requestAnimationFrame(time => this.draw(time));
    }

    resize() {
        const rect = this.canvas.getBoundingClientRect();
        const ratio = Math.min(devicePixelRatio || 1, 2);
        this.width = Math.max(280, rect.width);
        this.height = Math.max(330, rect.height);
        this.canvas.width = Math.round(this.width * ratio);
        this.canvas.height = Math.round(this.height * ratio);
        this.ctx.setTransform(ratio, 0, 0, ratio, 0, 0);
        this.seed();
    }

    seed() {
        const count = this.width < 720 ? 68 : this.width < 1200 ? 118 : 158;
        this.cubes = Array.from({ length: count }, (_, index) => ({
            orbit: Math.random() * Math.PI * 2,
            radius: this.width * (.12 + Math.random() * .48),
            verticalBand: (Math.random() - .5) * this.height * .7,
            z: Math.random(),
            size: 7 + Math.random() * 18,
            angle: Math.random() * Math.PI * 2,
            speed: .45 + Math.random() * 1.25,
            spin: (Math.random() - .5) * .035,
            phase: Math.random() * Math.PI * 2,
            index
        }));
    }

    beginQuestion(stability, correctElementId) {
        this.stability = clamp(stability);
        this.correctElementId = correctElementId;
        this.activeElapsed = 0;
        this.sentHints.clear();
        this.questionActive = true;
        this.activeStartedAt = this.canCount() ? performance.now() : 0;
        this.scheduleAmbientEvents(performance.now(), true);
        this.unlockAudio();
    }

    stopQuestion() {
        this.captureElapsed();
        this.questionActive = false;
        return this.activeElapsed;
    }

    canCount() { return !document.hidden && navigator.onLine; }

    captureElapsed() {
        if (this.questionActive && this.activeStartedAt) {
            this.activeElapsed += performance.now() - this.activeStartedAt;
            this.activeStartedAt = 0;
        }
    }

    handlePauseState() {
        if (!this.questionActive) return;
        if (!this.canCount()) this.captureElapsed();
        else if (!this.activeStartedAt) this.activeStartedAt = performance.now();
    }

    handleVisibilityChange() {
        this.handlePauseState();
        if (document.hidden) {
            if (this.frame !== null) cancelAnimationFrame(this.frame);
            this.frame = null;
            return;
        }
        if (this.frame === null && !this.disposed) {
            this.lastFrame = performance.now();
            this.frame = requestAnimationFrame(time => this.draw(time));
        }
    }

    elapsed() {
        return this.activeElapsed + (this.questionActive && this.activeStartedAt
            ? performance.now() - this.activeStartedAt : 0);
    }

    checkHints() {
        if (!this.questionActive || !this.canCount()) return;
        const elapsed = this.elapsed();
        for (const threshold of [10000, 18000, 28000]) {
            if (elapsed >= threshold && !this.sentHints.has(threshold)) {
                this.sentHints.add(threshold);
                void this.dotNetRef?.invokeMethodAsync("OnHintThreshold", Math.round(elapsed)).catch(() => {});
            }
        }
    }

    showCorrect(stability) {
        this.stability = clamp(stability);
        this.pulse = 1;
        this.spawnAnswerParticles("#55d6c2", 34);
        this.playChord();
    }

    showIncorrect() {
        this.wrongPulse = 1;
        this.spawnAnswerParticles("#ff726f", 18);
        this.playTone(155, .05, .14, "sawtooth");
    }

    completeTopic() {
        this.stability = 1;
        this.ambientShock = 0;
        this.waves.length = 0;
        this.topicFlash = 1;
        this.spawnAnswerParticles("#79f0dc", 55);
        this.playTone(660, .05, .18);
        setTimeout(() => this.playTone(880, .045, .2), 110);
    }

    spawnAnswerParticles(color, count) {
        const target = this.answerTarget();
        for (let i = 0; i < count; i++) this.particles.push({
            x: target.x, y: target.y, vx: -2.8 + Math.random() * 5.6,
            vy: -3.4 + Math.random() * 2.6, life: 1, color
        });
    }

    answerTarget() {
        const element = this.correctElementId && document.getElementById(this.correctElementId);
        if (!element) return { x: this.width * .5, y: this.height * .62 };
        const target = element.getBoundingClientRect();
        const canvas = this.canvas.getBoundingClientRect();
        return {
            x: clamp((target.left + target.width / 2 - canvas.left) / Math.max(1, canvas.width)) * this.width,
            y: clamp((target.top + target.height / 2 - canvas.top) / Math.max(1, canvas.height)) * this.height
        };
    }

    scheduleAmbientEvents(time, immediate = false) {
        const stability = this.stability;
        this.nextShockAt = time + (immediate ? 260 : 650 + stability * 5200 + Math.random() * (850 + stability * 2200));
        this.nextWaveAt = time + (immediate ? 120 : 420 + stability * 3400 + Math.random() * (500 + stability * 1800));
    }

    updateAmbientEvents(time, chaos) {
        if (this.reducedMotion || chaos < .08) return;
        if (time >= this.nextShockAt) {
            this.ambientShock = .68 + Math.random() * .32;
            this.shockX = .12 + Math.random() * .76;
            this.shockY = .14 + Math.random() * .72;
            this.spawnAmbientWave(chaos, true);
            if (chaos > .72) this.spawnAmbientWave(chaos, true);
            this.nextShockAt = time + 650 + this.stability * 5200
                + Math.random() * (850 + this.stability * 2200);
        }
        if (time >= this.nextWaveAt) {
            this.spawnAmbientWave(chaos, false);
            this.nextWaveAt = time + 420 + this.stability * 3400
                + Math.random() * (500 + this.stability * 1800);
        }
    }

    spawnAmbientWave(chaos, fromShock) {
        const originX = fromShock ? this.shockX : .08 + Math.random() * .84;
        const originY = fromShock ? this.shockY : .12 + Math.random() * .76;
        this.waves.push({
            x: originX * this.width,
            y: originY * this.height,
            radius: 10 + Math.random() * 18,
            maxRadius: Math.max(this.width, this.height) * (.2 + Math.random() * .22),
            speed: 2.1 + chaos * 2.8 + Math.random() * 1.3,
            life: 1,
            strength: (.1 + chaos * .2) * (fromShock ? 1.3 : 1)
        });
    }

    drawAmbientWaves(dt, chaos) {
        const ctx = this.ctx;
        ctx.save();
        ctx.globalCompositeOperation = "screen";
        for (const wave of this.waves) {
            const progress = wave.radius / wave.maxRadius;
            const alpha = Math.max(0, wave.life) * wave.strength * chaos * (1 - progress * .45);
            ctx.strokeStyle = `rgba(244, 42, 91, ${alpha})`;
            ctx.lineWidth = .8 + chaos * 1.9;
            ctx.beginPath();
            ctx.ellipse(wave.x, wave.y, wave.radius, wave.radius * (.48 + chaos * .16), 0, 0, Math.PI * 2);
            ctx.stroke();
            if (chaos > .55) {
                ctx.strokeStyle = `rgba(255, 112, 134, ${alpha * .36})`;
                ctx.lineWidth = .6;
                ctx.beginPath();
                ctx.ellipse(wave.x, wave.y, wave.radius * .86, wave.radius * (.4 + chaos * .14), 0, 0, Math.PI * 2);
                ctx.stroke();
            }
            wave.radius += wave.speed * dt;
            wave.life -= (.006 + this.stability * .012) * dt;
            if (wave.radius >= wave.maxRadius) wave.life -= .06 * dt;
        }
        ctx.restore();
        this.waves = this.waves.filter(wave => wave.life > 0 && wave.radius < wave.maxRadius * 1.15);
    }

    draw(time) {
        if (this.disposed) return;
        this.frame = null;
        const dt = Math.min(2, (time - this.lastFrame) / 16.67);
        this.lastFrame = time;
        const ctx = this.ctx;
        ctx.clearRect(0, 0, this.width, this.height);
        const chaos = 1 - this.stability;
        this.updateAmbientEvents(time, chaos);
        const atmospherePulse = this.reducedMotion ? 0 : (Math.sin(time * (.0012 + chaos * .0028)) + 1) * .5 * chaos;
        const centerX = this.width * .5;
        const centerY = this.height * .5;
        const background = ctx.createRadialGradient(centerX, centerY, 20,
            centerX, centerY, Math.max(this.width, this.height) * .78);
        background.addColorStop(0, mix("#50122f", "#07505a", this.stability));
        background.addColorStop(.46, mix("#210d28", "#0a2639", this.stability));
        background.addColorStop(1, mix("#080711", "#050e18", this.stability));
        ctx.fillStyle = background;
        ctx.fillRect(0, 0, this.width, this.height);
        const nebula = ctx.createRadialGradient(this.width * .72, this.height * .3, 0,
            this.width * .72, this.height * .3, this.width * .48);
        nebula.addColorStop(0, `rgba(${Math.round(155 - this.stability * 110)},${Math.round(18 + this.stability * 115)},${Math.round(78 + this.stability * 85)},${.1 + atmospherePulse * .08})`);
        nebula.addColorStop(1, "transparent");
        ctx.fillStyle = nebula;
        ctx.fillRect(0, 0, this.width, this.height);
        ctx.save();
        ctx.translate(centerX, centerY);
        ctx.scale(1, .43);
        for (let ring = 1; ring <= 4; ring++) {
            ctx.strokeStyle = mixAlpha([212, 49, 91], [59, 207, 195], this.stability, .035 + ring * .012);
            ctx.lineWidth = 1;
            ctx.beginPath();
            ctx.arc(0, 0, this.width * (.11 + ring * .1), 0, Math.PI * 2);
            ctx.stroke();
        }
        ctx.restore();
        this.drawAmbientWaves(dt, chaos);
        if (this.ambientShock > 0) {
            const shockGradient = ctx.createRadialGradient(this.shockX * this.width, this.shockY * this.height, 0,
                this.shockX * this.width, this.shockY * this.height, Math.max(this.width, this.height) * .48);
            shockGradient.addColorStop(0, `rgba(255, 45, 92, ${this.ambientShock * chaos * .12})`);
            shockGradient.addColorStop(.45, `rgba(190, 18, 64, ${this.ambientShock * chaos * .045})`);
            shockGradient.addColorStop(1, "transparent");
            ctx.fillStyle = shockGradient;
            ctx.fillRect(0, 0, this.width, this.height);
        }
        const shake = this.reducedMotion ? 0 : this.wrongPulse * 7 + this.ambientShock * chaos * 5.5;
        ctx.save();
        ctx.translate((Math.random() - .5) * shake, (Math.random() - .5) * shake);
        for (const cube of this.cubes) {
            const motionScale = this.reducedMotion ? .13 : .42 + chaos * 1.55;
            cube.orbit += .0032 * cube.speed * motionScale * dt;
            cube.phase += .009 * cube.speed * motionScale * dt;
            cube.z = (Math.sin(cube.orbit) + 1) * .5;
            const instability = chaos * (8 + cube.speed * 13);
            cube.x = centerX + Math.cos(cube.orbit) * cube.radius
                + Math.sin(cube.phase * 1.7) * instability;
            cube.y = centerY + Math.sin(cube.orbit) * cube.radius * .42
                + cube.verticalBand * (.2 + cube.z * .45)
                + Math.cos(cube.phase) * instability;
            cube.angle += cube.spin * (.18 + chaos * 1.7) * dt;
        }
        for (const cube of [...this.cubes].sort((a, b) => a.z - b.z)) {
            this.drawCube(cube, chaos);
        }
        ctx.restore();
        this.drawParticles(dt);
        if (this.pulse > 0) {
            ctx.strokeStyle = `rgba(92, 236, 211, ${this.pulse * .5})`;
            ctx.lineWidth = 3;
            ctx.beginPath();
            ctx.arc(this.width / 2, this.height / 2, (1 - this.pulse) * this.width * .45, 0, Math.PI * 2);
            ctx.stroke();
        }
        if (this.wrongPulse > 0) {
            ctx.fillStyle = `rgba(255, 70, 75, ${this.wrongPulse * .16})`;
            ctx.fillRect(0, 0, this.width, this.height);
        }
        if (this.topicFlash > 0) {
            ctx.fillStyle = `rgba(107, 243, 218, ${this.topicFlash * .2})`;
            ctx.fillRect(0, 0, this.width, this.height);
        }
        this.pulse = Math.max(0, this.pulse - .025 * dt);
        this.wrongPulse = Math.max(0, this.wrongPulse - .045 * dt);
        this.topicFlash = Math.max(0, this.topicFlash - .018 * dt);
        this.ambientShock = Math.max(0, this.ambientShock - (.045 + this.stability * .035) * dt);
        if (!document.hidden) this.frame = requestAnimationFrame(next => this.draw(next));
    }

    drawCube(cube, chaos) {
        const ctx = this.ctx;
        const s = cube.size * (.52 + cube.z * 1.05);
        const color = mix(cube.index % 3 === 0 ? "#bf2859" : "#e34a57",
            cube.index % 4 === 0 ? "#3987c7" : "#38b8b0", this.stability);
        ctx.save();
        ctx.translate(cube.x, cube.y);
        ctx.rotate(cube.angle);
        ctx.globalAlpha = .25 + cube.z * .7;
        ctx.fillStyle = shade(color, 22);
        ctx.beginPath();
        ctx.moveTo(0, -s * .62); ctx.lineTo(s * .62, -s * .28); ctx.lineTo(0, .06 * s); ctx.lineTo(-s * .62, -s * .28); ctx.closePath(); ctx.fill();
        ctx.fillStyle = color;
        ctx.beginPath();
        ctx.moveTo(-s * .62, -s * .28); ctx.lineTo(0, .06 * s); ctx.lineTo(0, s * .68); ctx.lineTo(-s * .62, s * .34); ctx.closePath(); ctx.fill();
        ctx.fillStyle = shade(color, -28);
        ctx.beginPath();
        ctx.moveTo(s * .62, -s * .28); ctx.lineTo(0, .06 * s); ctx.lineTo(0, s * .68); ctx.lineTo(s * .62, s * .34); ctx.closePath(); ctx.fill();
        if (chaos > .18) {
            ctx.strokeStyle = `rgba(255,125,151,${chaos * .25})`;
            ctx.strokeRect(-s * .67, -s * .67, s * 1.34, s * 1.34);
        }
        ctx.restore();
    }

    drawParticles(dt) {
        for (const particle of this.particles) {
            particle.x += particle.vx * dt;
            particle.y += particle.vy * dt;
            particle.vy += .06 * dt;
            particle.life -= .025 * dt;
            this.ctx.globalAlpha = Math.max(0, particle.life);
            this.ctx.fillStyle = particle.color;
            this.ctx.fillRect(particle.x, particle.y, 4, 4);
        }
        this.ctx.globalAlpha = 1;
        this.particles = this.particles.filter(x => x.life > 0);
    }

    unlockAudio() {
        try {
            this.audio ??= new (window.AudioContext || window.webkitAudioContext)();
            if (this.audio.state === "suspended") void this.audio.resume();
        } catch { }
    }

    playTone(frequency, volume, duration, type = "sine") {
        this.unlockAudio();
        if (!this.audio) return;
        const oscillator = this.audio.createOscillator();
        const gain = this.audio.createGain();
        oscillator.type = type;
        oscillator.frequency.value = frequency;
        gain.gain.setValueAtTime(.0001, this.audio.currentTime);
        gain.gain.exponentialRampToValueAtTime(volume, this.audio.currentTime + .01);
        gain.gain.exponentialRampToValueAtTime(.0001, this.audio.currentTime + duration);
        oscillator.connect(gain).connect(this.audio.destination);
        oscillator.start(); oscillator.stop(this.audio.currentTime + duration + .02);
    }

    playChord() {
        this.playTone(440, .04, .16);
        setTimeout(() => this.playTone(660, .035, .18), 70);
    }

    dispose() {
        if (this.disposed) return;
        this.disposed = true;
        this.stopQuestion();
        if (this.frame !== null) cancelAnimationFrame(this.frame);
        clearInterval(this.hintTimer);
        removeEventListener("resize", this.onResize);
        document.removeEventListener("visibilitychange", this.onVisibility);
        removeEventListener("online", this.onOnline);
        removeEventListener("offline", this.onOnline);
        this.canvas.removeEventListener("pointerdown", this.onPointer);
        if (this.audio) void this.audio.close().catch(() => {});
        this.dotNetRef = null;
    }
}

function clamp(value) { return Math.max(0, Math.min(1, Number(value) || 0)); }
function mix(a, b, amount) {
    const av = parseInt(a.slice(1), 16), bv = parseInt(b.slice(1), 16), t = clamp(amount);
    const channel = shift => Math.round(((av >> shift) & 255) * (1 - t) + ((bv >> shift) & 255) * t);
    return `rgb(${channel(16)},${channel(8)},${channel(0)})`;
}
function shade(color, amount) {
    const values = color.match(/\d+/g)?.map(Number) || [80, 120, 130];
    return `rgb(${values.map(x => Math.max(0, Math.min(255, x + amount))).join(",")})`;
}
function mixAlpha(from, to, amount, alpha) {
    const t = clamp(amount);
    const values = from.map((value, index) => Math.round(value * (1 - t) + to[index] * t));
    return `rgba(${values.join(",")},${alpha})`;
}
