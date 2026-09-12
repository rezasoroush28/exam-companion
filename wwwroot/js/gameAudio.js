let context;
let enabled = true;
try {
  enabled = localStorage.getItem("hamaahang-sound") !== "off";
} catch {}
const voices = new Set();
const roots = [261.63, 293.66, 329.63, 349.23, 392];
export function isEnabled() {
  return enabled;
}
export async function unlock() {
  if (!enabled) return;
  try {
    const Audio = window.AudioContext || window.webkitAudioContext;
    if (!Audio) return;
    context ??= new Audio();
    if (context.state === "suspended") await context.resume();
  } catch {
    /* Sound must never block an answer or progress. */
  }
}
export function setEnabled(value) {
  enabled = value;
  try {
    localStorage.setItem("hamaahang-sound", value ? "on" : "off");
  } catch {}
  if (!value) stop();
  else void unlock();
}
function note(frequency, delay = 0, duration = 0.4, volume = 0.055) {
  if (!enabled || !context) return;
  const oscillator = context.createOscillator(),
    gain = context.createGain();
  const at = context.currentTime + delay;
  oscillator.type = "sine";
  oscillator.frequency.value = frequency;
  gain.gain.setValueAtTime(0, at);
  gain.gain.linearRampToValueAtTime(volume, at + 0.02);
  gain.gain.exponentialRampToValueAtTime(0.0001, at + duration);
  oscillator.connect(gain).connect(context.destination);
  oscillator.start(at);
  oscillator.stop(at + duration + 0.03);
  voices.add(oscillator);
  oscillator.onended = () => {
    voices.delete(oscillator);
    oscillator.disconnect();
    gain.disconnect();
  };
}
function scratch(delay = 0) {
  if (!enabled || !context) return;
  const buffer = context.createBuffer(
    1,
    context.sampleRate * 0.12,
    context.sampleRate,
  );
  const data = buffer.getChannelData(0);
  for (let i = 0; i < data.length; i++)
    data[i] = (Math.random() * 2 - 1) * (1 - i / data.length);
  const source = context.createBufferSource(),
    filter = context.createBiquadFilter(),
    gain = context.createGain();
  source.buffer = buffer;
  filter.type = "bandpass";
  filter.frequency.value = 900;
  gain.gain.value = 0.025;
  source.connect(filter).connect(gain).connect(context.destination);
  source.start(context.currentTime + delay);
  voices.add(source);
  source.onended = () => {
    voices.delete(source);
    source.disconnect();
    filter.disconnect();
    gain.disconnect();
  };
}
function phrase(index, damaged, delay = 0, progress = 1) {
  const root = roots[index % 5],
    third = index === 1 || index === 2 ? 1.1892 : 1.2599;
  [1, third, 1.4983, 2, 1.4983]
    .slice(0, Math.max(1, Math.ceil(progress * 5)))
    .forEach((ratio, i) => note(root * ratio, delay + i * 0.17, 0.48, 0.045));
  if (damaged) {
    scratch(delay + 0.35);
    note(root * 1.05, delay + 0.35, 0.12, 0.015);
  }
}
export async function playTopic(index, damaged = false, progress = 1) {
  await unlock();
  phrase(index, damaged, 0, progress);
}
export async function feedback(result, correct, index, type) {
  await unlock();
  const root = roots[index % 5];
  if (result === "Repaired") {
    phrase(index, false);
    note(root * 2, 0.9, 0.7);
  } else if (result === "LessonCompleted") {
    for (let i = 0; i < 5; i++) phrase(i, false, i * 0.25);
  } else if (result === "Scratched" || result === "RepairFailed")
    phrase(index, true);
  else if (result === "TopicCompleted") phrase(index, false);
  else if (correct && type === "Challenge")
    [1, 1.2599, 1.4983].forEach((x) => note(root * x, 0, 0.7));
  else if (correct) note(root * 2, 0, 0.4);
  else {
    note(root, 0, 0.22, 0.03);
    note(root * 1.06, 0.08, 0.22, 0.015);
  }
}
export async function playDisc(topics) {
  await unlock();
  stop();
  if (!enabled) return 200;
  const audible = topics.filter((t) => t.progress > 0);
  audible.forEach((t, i) => phrase(t.order, t.scratched, i * 1.15, t.progress));
  return Math.max(400, audible.length * 1150 + 400);
}
export function stop() {
  for (const voice of voices) {
    try {
      voice.stop();
    } catch {}
  }
  voices.clear();
}
