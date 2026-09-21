// Render actual Web Audio offline; no sessions or player data are changed.
const assert = require("node:assert/strict");
let chromium;
try {
  ({ chromium } = require("playwright"));
} catch {
  ({ chromium } = require("../../.tools/browser/node_modules/playwright"));
}
(async () => {
  const browser = await chromium.launch({ channel: "msedge", headless: true });
  try {
    const page = await browser.newPage();
    await page.goto(process.env.EXAM_URL ?? "http://localhost:5175/");
    const result = await page.evaluate(async () => {
      const realAudio = window.AudioContext;
      let offline;
      // The scheduler uses standard audio nodes; supply an offline destination and clock.
      window.AudioContext = class {
        constructor() {
          offline = new OfflineAudioContext(1, 48000 * 4, 48000);
          Object.defineProperty(offline, "state", { get: () => "running" });
          return offline;
        }
      };
      async function render(name, progress, scratched, event = "preview") {
        const audio = await import("/js/gameAudio.js?offline=" + name);
        audio.setEnabled(true);
        const timeline = await audio.schedule(
          [{ order: 0, key: "C", notes: 1, progress, scratched }],
          event,
        );
        const buffer = await offline.startRendering();
        return { samples: buffer.getChannelData(0), timeline };
      }
      try {
        const clean = await render("clean", 1, false);
        const scratched = await render("scratched", 1, true);
        const partial = await render("partial", 0.3, false);
        const correct = await render("correct", 0.3, true, "correct");
        const cycle = await render("cycle", 1, true, "resolve");
        const replacementAudio =
          await import("/js/gameAudio.js?offline=replacement");
        const topic = {
          order: 0,
          key: "C",
          notes: 1,
          progress: 1,
          scratched: false,
        };
        replacementAudio.setEnabled(true);
        await replacementAudio.schedule([{ ...topic, scratched: true }]);
        await replacementAudio.wake();
        await replacementAudio.schedule([topic]);
        const replaced = (await offline.startRendering()).getChannelData(0);
        const stoppedAudio = await import("/js/gameAudio.js?offline=stopped");
        stoppedAudio.setEnabled(true);
        await stoppedAudio.schedule([{ ...topic, scratched: true }]);
        stoppedAudio.stop();
        const stopped = (await offline.startRendering()).getChannelData(0);
        const concurrentAudio =
          await import("/js/gameAudio.js?offline=concurrent");
        concurrentAudio.setEnabled(true);
        const stale = concurrentAudio.schedule([{ ...topic, scratched: true }]);
        const latest = concurrentAudio.schedule([topic]);
        const staleTimeline = await stale;
        await latest;
        const concurrent = (await offline.startRendering()).getChannelData(0);
        const amplitude = (data, time) => {
          const frequency = 440 * 2 ** ((60 - 69) / 12);
          let re = 0,
            im = 0;
          const begin = Math.round((time + 0.06) * 48000),
            count = 9600;
          for (let i = begin; i < begin + count; i++) {
            const angle = (2 * Math.PI * frequency * i) / 48000;
            re += data[i] * Math.cos(angle);
            im += data[i] * Math.sin(angle);
          }
          return (Math.hypot(re, im) * 2) / count;
        };
        const difference = (a, b, time) => {
          const begin = Math.round((time + 0.06) * 48000),
            count = 4800;
          let sum = 0;
          for (let i = begin; i < begin + count; i++) sum += (a[i] - b[i]) ** 2;
          return Math.sqrt(sum / count);
        };
        return {
          replacementDifference: replaced.reduce(
            (peak, value, i) =>
              Math.max(peak, Math.abs(value - clean.samples[i])),
            0,
          ),
          stoppedPeak: stopped.reduce(
            (peak, value) => Math.max(peak, Math.abs(value)),
            0,
          ),
          concurrentDifference: concurrent.reduce(
            (peak, value, i) =>
              Math.max(peak, Math.abs(value - clean.samples[i])),
            0,
          ),
          staleEntries: staleTimeline.entries.length,
          cases: [scratched, partial].map((test) => ({
            before: difference(clean.samples, test.samples, 0.85),
            middleRatio:
              amplitude(test.samples, 1.6) / amplitude(clean.samples, 1.6),
            lateNoise: difference(clean.samples, test.samples, 2.15),
            after: difference(clean.samples, test.samples, 2.5),
            duration: test.timeline.entries[0].noiseDuration,
          })),
          correctDamaged: correct.timeline.entries[0].damaged,
          cleanDamaged: clean.timeline.entries[0].damaged,
          cycleHasNotes: cycle.samples.some((value) => Math.abs(value) > 0.001),
          cycleTailPeak: cycle.samples
            .slice(Math.round(1.4 * 48000))
            .reduce((peak, value) => Math.max(peak, Math.abs(value)), 0),
        };
      } finally {
        window.AudioContext = realAudio;
      }
    });
    for (const sample of result.cases) {
      assert(sample.before < 0.00001, "chord begins normally");
      assert(
        sample.middleRatio < 0.3,
        "sounding chord is interrupted in the middle",
      );
      assert(
        sample.lateNoise > 0.001,
        "damage remains audible roughly one second into the disturbance",
      );
      assert(
        sample.after < 0.00001,
        "same chord returns cleanly after the noise",
      );
      assert(sample.duration >= 1, "noise lasts at least one second");
    }
    assert(
      result.replacementDifference < 0.00001,
      "replacement cancels prior notes, noise and wake without oscillator stacking",
    );
    assert(result.stoppedPeak < 0.00001, "stop silences all scheduled sources");
    assert(
      result.concurrentDifference < 0.00001 && result.staleEntries === 0,
      "concurrent stale scheduler cannot add voices",
    );
    assert(result.cycleHasNotes, "cycle completion plays its notes");
    assert(
      result.cycleTailPeak < 0.00001,
      "cycle completion has no prolonged final chord or noise tail",
    );
    assert.equal(
      result.correctDamaged,
      false,
      "correct-answer note is not damaged",
    );
    assert.equal(
      result.cleanDamaged,
      false,
      "clean completed or repaired topic stays clean",
    );
    console.log(
      "PASS: real audio renders start clean, break up mid-chord for 1.1s, and recover.",
      JSON.stringify(result),
    );
  } finally {
    await browser.close();
  }
})().catch((error) => {
  console.error(error);
  process.exitCode = 1;
});
