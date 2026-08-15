const controllers = new Map();
let nextId = 1;

export function initializeHealthBar(fill, health) {
    const id = `health-${nextId++}`;
    const controller = new HealthBarController(fill, health);
    controllers.set(id, controller);
    return id;
}

export function beginQuestion(id, health, decayPerSecond, gracePeriodMs) {
    get(id).begin(health, decayPerSecond, gracePeriodMs);
}
export function getActiveElapsedMs(id) { return get(id).elapsed(); }
export function stopQuestion(id) { return get(id).stop(); }
export function setHealth(id, health, positive) { get(id).setHealth(health, positive); }
export function disposeHealthBar(id) {
    const controller = controllers.get(id);
    if (!controller) return;
    controller.dispose();
    controllers.delete(id);
}

function get(id) {
    const controller = controllers.get(id);
    if (!controller) throw new Error("Health bar was not initialized.");
    return controller;
}

class HealthBarController {
    constructor(fill, health) {
        this.fill = fill;
        this.health = health;
        this.active = false;
        this.startedAt = 0;
        this.accumulated = 0;
        this.animation = null;
        this.reconnectModal = document.getElementById("components-reconnect-modal");
        this.onVisibility = () => this.syncTimingState();
        this.onAnswerIntent = event => {
            if (this.active && event.target instanceof Element && event.target.closest(".answer:not(:disabled)"))
                this.freezeAtAnswerIntent();
        };
        document.addEventListener("visibilitychange", this.onVisibility);
        document.addEventListener("click", this.onAnswerIntent, true);
        window.addEventListener("online", this.onVisibility);
        window.addEventListener("offline", this.onVisibility);
        this.observer = this.reconnectModal ? new MutationObserver(() => this.syncTimingState()) : null;
        this.observer?.observe(this.reconnectModal, { attributes: true, attributeFilter: ["open", "class"] });
        this.setTransform(health);
    }

    begin(health, decayPerSecond, gracePeriodMs) {
        this.cancelAnimation();
        this.health = Number(health);
        this.active = true;
        this.accumulated = 0;
        this.startedAt = this.canCount() ? performance.now() : 0;
        const decay = Math.max(0.000001, Number(decayPerSecond));
        const decayDuration = this.health / decay * 1000;
        const total = gracePeriodMs + decayDuration;
        const graceOffset = Math.min(1, gracePeriodMs / total);
        this.animation = this.fill.animate([
            { transform: `scaleX(${this.health / 100})`, offset: 0 },
            { transform: `scaleX(${this.health / 100})`, offset: graceOffset },
            { transform: "scaleX(0)", offset: 1 }
        ], { duration: total, easing: "linear", fill: "forwards" });
        if (!this.canCount()) this.animation.pause();
    }

    elapsed() {
        return this.accumulated + (this.active && this.startedAt ? performance.now() - this.startedAt : 0);
    }

    stop() {
        const elapsed = this.elapsed();
        this.active = false;
        this.startedAt = 0;
        this.accumulated = elapsed;
        this.cancelAnimation();
        return elapsed;
    }

    freezeAtAnswerIntent() {
        if (!this.active) return;
        if (this.startedAt) this.accumulated += performance.now() - this.startedAt;
        this.startedAt = 0;
        this.active = false;
        this.animation?.pause();
    }

    setHealth(health, positive) {
        this.active = false;
        this.cancelAnimation();
        this.health = Math.max(0, Math.min(100, Number(health)));
        this.fill.style.transition = `transform ${positive ? 250 : 220}ms ease-out`;
        this.setTransform(this.health);
    }

    syncTimingState() {
        if (!this.active) return;
        if (this.canCount()) {
            if (!this.startedAt) this.startedAt = performance.now();
            this.animation?.play();
        } else {
            if (this.startedAt) this.accumulated += performance.now() - this.startedAt;
            this.startedAt = 0;
            this.animation?.pause();
        }
    }

    canCount() {
        const reconnecting = this.reconnectModal?.hasAttribute("open") || false;
        return document.visibilityState === "visible" && navigator.onLine && !reconnecting;
    }

    setTransform(health) { this.fill.style.transform = `scaleX(${Math.max(0, Math.min(100, health)) / 100})`; }
    cancelAnimation() { if (this.animation) { this.animation.cancel(); this.animation = null; } }

    dispose() {
        this.stop();
        document.removeEventListener("visibilitychange", this.onVisibility);
        document.removeEventListener("click", this.onAnswerIntent, true);
        window.removeEventListener("online", this.onVisibility);
        window.removeEventListener("offline", this.onVisibility);
        this.observer?.disconnect();
        this.fill = null;
    }
}
