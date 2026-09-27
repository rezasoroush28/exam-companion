import * as audio from "./gameAudio.js";
export function create(host) {
  let frame = 0,
    generation = 0,
    disposed = false,
    currentOrder = -1;
  const reduced = matchMedia("(prefers-reduced-motion: reduce)");
  const gram = () => host.querySelector("[data-gramophone]");
  function emit(name, detail = {}) {
    host.dispatchEvent(new CustomEvent("gramophone-" + name, { detail }));
  }
  function reset() {
    const g = gram();
    if (!g) return;
    g.dataset.playing = "false";
    g.dataset.hit = "false";
    delete g.dataset.pulse;
    g.querySelector("[data-record]")?.setAttribute("transform", "rotate(0)");
    g.querySelectorAll(".groove.playing").forEach((e) =>
      e.classList.remove("playing"),
    );
    host.dataset.playing = "false";
    host.dataset.hit = "false";
    currentOrder = -1;
  }
  function stop() {
    generation++;
    cancelAnimationFrame(frame);
    audio.stop();
    reset();
  }
  function placeNeedle(g, order, jump = 0) {
    const plane = g
      .querySelector("[data-turntable]")
      ?.transform.baseVal.consolidate()?.matrix;
    const arm = g.querySelector("[data-tone-arm]");
    if (!plane || !arm) return;
    const r = 175 - order * 25;
    const contact = new DOMPoint(
      r * Math.cos(0.55),
      r * Math.sin(0.55),
    ).matrixTransform(plane);
    const x = contact.x,
      y = contact.y - Number(arm.dataset.needleLift) - jump;
    const px = Number(arm.dataset.pivotX),
      py = Number(arm.dataset.pivotY);
    const path = `M${px} ${py}C${px - 38} ${py + 40} ${x + 105} ${y - 18} ${x} ${y - 12}`;
    g.querySelector("[data-arm-path]")?.setAttribute("d", path);
    g.querySelector("[data-arm-highlight]")?.setAttribute("d", path);
    g.querySelector("[data-needle]")?.setAttribute(
      "transform",
      `translate(${x} ${y})`,
    );
  }
  async function play(topics, event = "preview") {
    stop();
    const own = generation;
    if (disposed || !topics.length) return;
    const timeline = await audio.schedule(topics, event);
    if (disposed || own !== generation) return;
    let hitOnce = false;
    emit("started", { event, topics: topics.length });
    function tick() {
      if (disposed || own !== generation) return;
      const now = audio.clock();
      const t = timeline.entries.find(
        (t) => now >= t.start && now < t.start + t.duration,
      );
      const g = gram();
      if (now >= timeline.end || !g || !host.isConnected) {
        stop();
        emit("completed");
        return;
      }
      if (t) {
        const p = (now - t.start) / t.duration,
          atHit =
            t.damaged && p >= t.hit && p < t.hit + t.noiseDuration / t.duration;
        if (currentOrder !== t.order) {
          g.querySelectorAll(".groove.playing").forEach((e) =>
            e.classList.remove("playing"),
          );
          currentOrder = t.order;
          hitOnce = false;
          emit("track", { order: t.order });
        }
        g.querySelector(`[data-groove="${t.order}"]`)?.classList.add("playing");
        g.dataset.playing = "true";
        g.dataset.hit = String(atHit);
        host.dataset.playing = "true";
        host.dataset.hit = String(atHit);
        g.dataset.pulse = event;
        g.dataset.order = t.order;
        if (!reduced.matches)
          g.querySelector("[data-record]")?.setAttribute(
            "transform",
            `rotate(${(p + (t.scratched && t.damaged ? t.contact - t.hit : 0)) * 360})`,
          );
        const jump =
          atHit && !reduced.matches
            ? 2 + 2 * Math.sin((now - t.start) * 65)
            : 0;
        placeNeedle(g, t.order, jump);
        const r = 175 - t.order * 25,
          angle = reduced.matches ? 0.55 : 0.55 + Math.PI * (p - t.hit);
        const point = g.querySelector("[data-playhead]");
        point?.setAttribute("cx", r * Math.cos(angle));
        point?.setAttribute("cy", r * Math.sin(angle));
        if (t.damaged && p > t.hit - 0.05 && p < t.hit) g.dataset.hit = "true";
        if (atHit && !hitOnce) {
          hitOnce = true;
          emit("scratch", {
            order: t.order,
            scheduledTime: t.start + t.duration * t.hit,
            visualTime: now,
            disturbanceMs: t.noiseDuration * 1000,
            reduced: reduced.matches,
          });
        }
      }
      frame = requestAnimationFrame(tick);
    }
    frame = requestAnimationFrame(tick);
  }
  const visibility = () => {
    if (document.hidden) stop();
  };
  document.addEventListener("visibilitychange", visibility);
  return {
    play,
    stop,
    isEnabled: audio.isEnabled,
    focusQuestion() {
      host
        .querySelector(
          ".answer-option:not(:disabled), .completion-panel .primary",
        )
        ?.focus({ preventScroll: true });
    },
    toggle(topics) {
      if (host.dataset.playing === "true") stop();
      else return play(topics, "album");
    },
    setEnabled(value) {
      stop();
      audio.setEnabled(value);
    },
    dispose() {
      disposed = true;
      stop();
      document.removeEventListener("visibilitychange", visibility);
    },
  };
}
