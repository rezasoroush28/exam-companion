// One audio clock drives the sounds and the presentation timeline in gameMotion.js.
let context;
let generation = 0;
let enabled = true;
try {
  enabled = localStorage.getItem("hamaahang-sound") !== "off";
} catch {}
const voices = new Set();
export const phraseSeconds = 2.7;
export const noiseSeconds = 1.1;
const damageStartsAt = 1.25;
const cycleNoteSeconds = 0.65;
const cycleNoteSpacing = 0.2;
const chords = {
  C: [60, 64, 67, 71],
  Dm: [62, 65, 69, 72],
  Em: [64, 67, 71, 74],
  F: [65, 69, 72, 76],
  G: [67, 71, 74, 77],
};
export function isEnabled() {
  return enabled;
}
export async function unlock() {
  try {
    const Audio = window.AudioContext || window.webkitAudioContext;
    if (enabled && Audio) {
      context ??= new Audio();
      if (context.state === "suspended") await context.resume();
    }
  } catch {}
}
export function clock() {
  return context?.state === "running"
    ? context.currentTime
    : performance.now() / 1000;
}
export function setEnabled(value) {
  enabled = value;
  try {
    localStorage.setItem("hamaahang-sound", value ? "on" : "off");
  } catch {}
  if (!value) stop();
}
function voice(midi, at, length, volume, bus, sustain = 0) {
  if (!enabled || context?.state !== "running") return;
  const source = context.createOscillator(),
    gain = context.createGain();
  source.type = "sine";
  source.frequency.value = 440 * 2 ** ((midi - 69) / 12);
  gain.gain.setValueAtTime(0, at);
  gain.gain.linearRampToValueAtTime(volume, at + 0.025);
  if (sustain > 0) gain.gain.setValueAtTime(volume, at + sustain);
  gain.gain.exponentialRampToValueAtTime(0.0001, at + length);
  source.connect(gain).connect(bus || context.destination);
  source.start(at);
  source.stop(at + length + 0.03);
  voices.add(source);
  source.onended = () => {
    voices.delete(source);
    source.disconnect();
    gain.disconnect();
  };
}
function crackle(at, duration) {
  if (!enabled || context?.state !== "running") return;
  const b = context.createBuffer(
      1,
      Math.ceil(context.sampleRate * duration),
      context.sampleRate,
    ),
    values = b.getChannelData(0);
  for (let i = 0; i < values.length; i++) {
    const time = i / context.sampleRate;
    const flutter =
      0.65 + 0.2 * Math.sin(time * 47) + 0.15 * Math.sin(time * 113);
    values[i] = (Math.random() * 2 - 1) * flutter;
  }
  const source = context.createBufferSource(),
    filter = context.createBiquadFilter(),
    gain = context.createGain();
  source.buffer = b;
  filter.type = "bandpass";
  filter.frequency.value = 1300;
  gain.gain.setValueAtTime(0, at);
  gain.gain.linearRampToValueAtTime(0.045, at + 0.04);
  gain.gain.setValueAtTime(0.045, at + duration - 0.08);
  gain.gain.linearRampToValueAtTime(0, at + duration);
  source.connect(filter).connect(gain).connect(context.destination);
  source.start(at);
  voices.add(source);
  source.onended = () => {
    voices.delete(source);
    source.disconnect();
    filter.disconnect();
    gain.disconnect();
  };
}
export function scratchFraction(order) {
  return (
    ((0.55 - (1.5 + order * 0.4) + Math.PI * 2) % (Math.PI * 2)) / (Math.PI * 2)
  );
}
export async function schedule(topics, event = "preview") {
  stop();
  const own = generation;
  await unlock();
  const start = clock() + 0.06;
  if (own !== generation) return { start, entries: [], end: start };
  // Cycle completion plays four equally short notes, without the sustained replay layer.
  const cycleResolution = event === "resolve";
  const duration = cycleResolution
    ? 3 * cycleNoteSpacing + cycleNoteSeconds + 0.05
    : phraseSeconds;
  const entries = topics.map((t, i) => ({
    ...t,
    start: start + i * duration,
    duration,
    contact: scratchFraction(t.order),
    hit: damageStartsAt / duration,
    damaged:
      event !== "correct" &&
      event !== "wrong" &&
      !cycleResolution &&
      (t.scratched || t.progress < 1),
    noiseDuration: noiseSeconds,
  }));
  for (const t of entries) {
    if (!enabled || context?.state !== "running") continue;
    const notes = chords[t.key] || chords.C;
    const bus = context.createGain();
    bus.gain.value = 1;
    bus.connect(context.destination);
    // Interrupt a still-sounding chord in the middle, then let it recover.
    // Single-answer feedback remains a separate short musical acknowledgement.
    if (t.damaged) {
      const hit = t.start + t.duration * t.hit;
      bus.gain.setValueAtTime(1, hit - 0.025);
      bus.gain.linearRampToValueAtTime(0.035, hit);
      for (let j = 1; j < 13; j++)
        bus.gain.linearRampToValueAtTime(
          j % 3 === 0 ? 0.16 : 0.035,
          hit + (j * noiseSeconds) / 13,
        );
      bus.gain.setValueAtTime(0.035, hit + noiseSeconds - 0.04);
      bus.gain.linearRampToValueAtTime(1, hit + noiseSeconds);
      crackle(hit, noiseSeconds);
    }
    if (event === "correct")
      voice(
        notes[Math.max(0, t.notes - 1) % notes.length] + 12,
        t.start,
        1.05,
        0.07,
        bus,
      );
    else if (event === "wrong") {
      voice(notes[0], t.start, 0.5, 0.035, bus);
      voice(notes[1] - 1, t.start + 0.12, 0.6, 0.025, bus);
    } else {
      const count = notes.length;
      notes
        .slice(0, count)
        .forEach((n, j) =>
          voice(
            n + 12,
            t.start + j * cycleNoteSpacing,
            cycleNoteSeconds,
            0.03,
            bus,
          ),
        );
      if (!cycleResolution)
        notes
          .slice(0, count)
          // Brief resolution after the noise: hold until 2.4s, fade out by 2.65s.
          .forEach((n) => voice(n, t.start + 0.65, 2, 0.027, bus, 1.75));
    }
    // Disconnect the phrase bus after the last scheduled voice finishes, without timers.
    const sentinel = context.createOscillator(),
      silent = context.createGain();
    silent.gain.value = 0;
    sentinel.connect(silent).connect(context.destination);
    sentinel.start(t.start);
    sentinel.stop(t.start + t.duration + 0.1);
    voices.add(sentinel);
    sentinel.onended = () => {
      voices.delete(sentinel);
      sentinel.disconnect();
      silent.disconnect();
      bus.disconnect();
    };
  }
  return { start, entries, end: start + entries.length * duration };
}
export function stop() {
  generation++;
  for (const v of voices) {
    try {
      v.stop();
    } catch {}
  }
  voices.clear();
}
