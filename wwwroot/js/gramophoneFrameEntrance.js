// One absolute-time, controller-scoped renderer for the prototype-derived
// entrance. It owns one requestAnimationFrame at most; pose mathematics stays
// in gramophoneEntranceRig.js and DOM/lifecycle policy stays in gameMotion.js.
import { entranceStages, sampleEntrancePose } from "./gramophoneEntranceRig.js";

const clamp = (value, min, max) => Math.max(min, Math.min(max, value));

function timelineFor(stages) {
  let start = 0;
  return stages.map((stage) => {
    const item = { ...stage, start, end: start + stage.duration };
    start = item.end;
    return item;
  });
}

function locate(timeline, elapsed) {
  const total = timeline.at(-1)?.end ?? 0;
  const time = clamp(elapsed, 0, total);
  const stage = timeline.find((item) => time < item.end) ?? timeline.at(-1);
  const progress = stage?.duration ? clamp((time - stage.start) / stage.duration, 0, 1) : 1;
  return { stage, progress, time, total };
}

export function createFrameEntranceRunner({ isValid, applyPose, onStage, onComplete, onInvalid }) {
  let frameId = 0;
  let active = false;
  let paused = false;
  let rate = 1;
  let elapsed = 0;
  let startedAt = 0;
  let generation = 0;
  let frameCount = 0;
  let lastStage = "";
  let geometry = null;
  let timeline = timelineFor(entranceStages);
  let lastSample = null;
  let completion = null;

  const currentElapsed = (now = performance.now()) =>
    active && !paused ? elapsed + (now - startedAt) * rate : elapsed;

  function snapshot(now = performance.now()) {
    const located = locate(timeline, currentElapsed(now));
    return {
      runtime: "requestAnimationFrame",
      active,
      paused,
      rate,
      generation,
      frameCount,
      absoluteTime: located.time,
      totalDuration: located.total,
      stage: located.stage?.name ?? null,
      progress: located.progress,
      activeFrameLoops: active ? 1 : 0,
      sample: lastSample?.diagnostics ?? null,
    };
  }

  function stopFrame() {
    if (frameId) cancelAnimationFrame(frameId);
    frameId = 0;
  }

  function cancel() {
    const pending = completion;
    completion = null;
    generation++;
    active = false;
    paused = false;
    stopFrame();
    pending?.({ completed: false, ...snapshot() });
  }

  function pause() {
    if (!active || paused) return;
    elapsed = currentElapsed();
    paused = true;
    stopFrame();
  }

  function resume() {
    if (!active || !paused) return;
    paused = false;
    startedAt = performance.now();
    schedule(generation);
  }

  function setRate(nextRate) {
    const normalized = Number.isFinite(nextRate) && nextRate > 0 ? nextRate : 1;
    if (active && !paused) {
      elapsed = currentElapsed();
      startedAt = performance.now();
    }
    rate = normalized;
  }

  function seek(nextElapsed) {
    if (!active || !geometry || !isValid()) return false;
    elapsed = clamp(Number(nextElapsed) || 0, 0, timeline.at(-1)?.end ?? 0);
    startedAt = performance.now();
    const located = locate(timeline, elapsed);
    const sample = sampleEntrancePose(located.stage.name, located.progress, geometry);
    if (!sample || sample.diagnostics?.clamped || !Number.isFinite(sample.locomotionX)) return false;
    if (located.stage.name !== lastStage) {
      lastStage = located.stage.name;
      onStage?.(located.stage.name, located);
    }
    lastSample = sample;
    frameCount++;
    applyPose(sample, located);
    return snapshot();
  }

  function schedule(own) {
    frameId = requestAnimationFrame((now) => tick(own, now));
  }

  function tick(own, now) {
    frameId = 0;
    if (!active || paused || own !== generation || !isValid()) {
      if (own === generation) {
        onInvalid?.();
        cancel();
      }
      return;
    }
    const located = locate(timeline, currentElapsed(now));
    const sample = sampleEntrancePose(located.stage.name, located.progress, geometry);
    if (!sample || sample.diagnostics?.clamped || !Number.isFinite(sample.locomotionX)) {
      cancel();
      return;
    }
    if (located.stage.name !== lastStage) {
      lastStage = located.stage.name;
      onStage?.(located.stage.name, located);
    }
    lastSample = sample;
    frameCount++;
    applyPose(sample, located);
    if (located.time >= located.total) {
      elapsed = located.total;
      active = false;
      onComplete?.(snapshot(now));
      const pending = completion;
      completion = null;
      pending?.({ completed: true, ...snapshot(now) });
      return;
    }
    schedule(own);
  }

  function start({ stages = entranceStages, nextGeometry } = {}) {
    cancel();
    timeline = timelineFor(stages);
    geometry = nextGeometry;
    elapsed = 0;
    frameCount = 0;
    lastStage = "";
    lastSample = null;
    active = true;
    paused = false;
    startedAt = performance.now();
    const own = generation;
    schedule(own);
    return new Promise((resolve) => { completion = resolve; });
  }

  return { start, cancel, pause, resume, setRate, seek, diagnostics: snapshot };
}
