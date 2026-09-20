// Checks rendered movement through real answers. Removes only the session it creates.
const assert = require("node:assert/strict");
const fs = require("node:fs");
const path = require("node:path");
const { DatabaseSync } = require("node:sqlite");
let chromium;
try {
  ({ chromium } = require("playwright"));
} catch {
  ({ chromium } = require("../../.tools/browser/node_modules/playwright"));
}
const root = path.resolve(__dirname, "../..");
const db = new DatabaseSync(path.join(root, "data/exam-companion.db"));
db.exec("PRAGMA foreign_keys=ON; PRAGMA busy_timeout=5000;");
fs.mkdirSync(path.join(root, "artifacts/visible-motion"), { recursive: true });
(async () => {
  const browser = await chromium.launch({ channel: "msedge", headless: true });
  const page = await browser.newPage({
    viewport: { width: 1440, height: 950 },
  });
  let id;
  const errors = [];
  page.on("pageerror", (e) => errors.push(e.message));
  if (process.argv.includes("--blocked-audio"))
    await page.addInitScript(() => {
      window.AudioContext = class {
        get state() {
          return "suspended";
        }
        resume() {
          return new Promise(() => {});
        }
      };
    });
  await page.addInitScript(() => {
    window.trace = { events: [], animations: [], samples: [] };
    const animate = Element.prototype.animate;
    Element.prototype.animate = function (frames, options) {
      const a = animate.call(this, frames, options);
      if (this.closest("[data-gramophone]")) {
        const entry = {
          part: this.dataset.part,
          at: performance.now(),
          connected: this.isConnected,
          options,
          frames,
        };
        window.trace.animations.push(entry);
        a.finished.then(
          () => (entry.finished = performance.now()),
          () => (entry.cancelled = performance.now()),
        );
      }
      return a;
    };
    for (const name of [
      "gramophone-motion-started",
      "gramophone-motion-completed",
      "gramophone-entrance-complete",
    ])
      document.addEventListener(
        name,
        (e) =>
          window.trace.events.push({
            name,
            ...e.detail,
            at: performance.now(),
          }),
        true,
      );
    function sample() {
      const artifact = document.querySelector('[data-part="artifact-motion"]'),
        machine = document.querySelector('[data-part="machine-root"]'),
        horn = document.querySelector('[data-part="horn-follow"]'),
        tonearm = document.querySelector('[data-part="tonearm-follow"]'),
        record = document.querySelector('[data-part="record-root"]');
      if (artifact && window.sampling) {
        const g = artifact.closest("[data-gramophone]"),
          m = new DOMMatrix(getComputedStyle(artifact).transform),
          machineMatrix = new DOMMatrix(getComputedStyle(machine).transform),
          h = new DOMMatrix(getComputedStyle(horn).transform),
          tone = new DOMMatrix(getComputedStyle(tonearm).transform),
          s = artifact.ownerSVGElement.getScreenCTM(),
          bodyRect = g.querySelector('[data-part="gram-body"]').getBoundingClientRect(),
          hornRect = g.querySelector('[data-part="horn-root"]').getBoundingClientRect(),
          tonearmRect = g.querySelector('[data-part="tonearm-root"]').getBoundingClientRect();
        window.trace.samples.push({
          at: performance.now(),
          motion: g.dataset.motion,
          ready: g.dataset.ready,
          opacity: getComputedStyle(machine).opacity,
          connected: artifact.isConnected,
          y: m.f * s.d,
          angle: (Math.atan2(m.b, m.a) * 180) / Math.PI,
          machineY: machineMatrix.f * s.d,
          locomotionX: new DOMMatrix(getComputedStyle(g.querySelector('[data-part="gram-locomotion"]')).transform).e * s.a,
          horn: (Math.atan2(h.b, h.a) * 180) / Math.PI,
          tonearm: (Math.atan2(tone.b, tone.a) * 180) / Math.PI,
          recordVisibility: getComputedStyle(record).visibility,
          relativeHornX: hornRect.x - bodyRect.x,
          relativeHornY: hornRect.y - bodyRect.y,
          relativeTonearmX: tonearmRect.x - bodyRect.x,
          relativeTonearmY: tonearmRect.y - bodyRect.y,
          animations: artifact.getAnimations().length,
        });
      }
      requestAnimationFrame(sample);
    }
    requestAnimationFrame(sample);
  });
  const capture = async (name, action, ms = 1900) => {
    await page.evaluate(() => {
      window.trace = { events: [], animations: [], samples: [] };
      window.sampling = true;
    });
    await action();
    await page.waitForTimeout(ms);
    const trace = await page.evaluate(() => {
      window.sampling = false;
      return window.trace;
    });
    fs.writeFileSync(
      path.join(root, "artifacts/visible-motion", name + ".json"),
      JSON.stringify(trace, null, 2),
    );
    const ys = trace.samples.map((s) => s.y),
      angles = trace.samples.map((s) => s.angle);
    console.log(
      name,
      JSON.stringify({
        motions: trace.events
          .filter((e) => e.name === "gramophone-motion-started")
          .map((e) => e.motion),
        rangeY: Math.max(...ys) - Math.min(...ys),
        rangeAngle: Math.max(...angles) - Math.min(...angles),
        movingFrames: trace.samples.filter((s) => Math.abs(s.y) > 3).length,
        hidden: trace.samples.filter((s) => s.opacity === "0").length,
        animations: trace.animations.map((a) => ({
          part: a.part,
          duration: a.options.duration,
          elapsed: (a.finished || a.cancelled) - a.at,
          connected: a.connected,
        })),
      }),
    );
    return trace;
  };
  try {
    await page.goto(`${process.env.EXAM_URL || "http://localhost:5175"}/`);
    await page.locator(".start-game:not([disabled])").waitFor();
    await page.evaluate(() => document.fonts.ready);
    console.log(
      "Preferences",
      await page.evaluate(() => ({
        os: matchMedia("(prefers-reduced-motion: reduce)").matches,
        stored: localStorage.getItem("hamaahang-reduced-motion"),
      })),
    );
    const entranceTrace = await capture(
      "entrance",
      async () => {
        await page.locator(".start-game").click();
        await page.waitForURL("**/game/*");
        id = page.url().split("/").pop().toUpperCase();
      },
      7100,
    );
    const entranceXs = entranceTrace.samples.map((s) => s.locomotionX);
    assert(
      Math.max(...entranceXs) - Math.min(...entranceXs) > 250,
      "gram-locomotion visibly walks across the scene",
    );
    assert(
      entranceTrace.samples.some((s) => s.recordVisibility === "hidden"),
      "record remains hidden while the machine enters",
    );
    const fallingSamples = entranceTrace.samples.filter(
      (s) => s.motion === "hold-disc",
    );
    const spread = (values) => Math.max(...values) - Math.min(...values);
    assert(
      fallingSamples.length > 2 &&
        spread(fallingSamples.map((s) => s.relativeHornX)) < 5 &&
        spread(fallingSamples.map((s) => s.relativeHornY)) < 5 &&
        spread(fallingSamples.map((s) => s.relativeTonearmX)) < 5 &&
        spread(fallingSamples.map((s) => s.relativeTonearmY)) < 5,
      "base, horn and tonearm preserve their composition during the readable hold",
    );
    console.log(
      "Rendered",
      await page.locator("[data-gramophone]").evaluate((e) => ({
        dataset: { ...e.dataset },
        checkbox: document.querySelector(".motion-preference input").checked,
        svg: e.querySelector("svg").outerHTML.length,
      })),
    );
    const answer = async (name, correct) =>
      capture(name, async () => {
        const row = db
          .prepare(
            "SELECT q.CorrectOption FROM Sessions s JOIN Questions q ON q.Id=s.CurrentQuestionId WHERE s.Id=?",
          )
          .get(id);
        const key = correct
          ? row.CorrectOption
          : row.CorrectOption === "A"
            ? "B"
            : "A";
        await page
          .locator(".answer-option")
          .filter({
            has: page.locator(".answer-letter", {
              hasText: new RegExp("^" + key + "$"),
            }),
          })
          .click();
        await page.locator(".question-feedback").waitFor();
      });
    const correctTrace = await answer("correct", true);
    const moving = correctTrace.samples.filter(
      (s) => s.motion === "correct-pulse" && Math.abs(s.y) > 3,
    );
    assert(
      moving.length > 0,
      "correct answer must physically move, even if audio resume never resolves",
    );
    assert(
      moving.at(-1).at - moving[0].at >= 120,
      "correct lift must remain readable across its phases",
    );
    assert(
      Math.max(...moving.map((s) => Math.abs(s.y))) >= 5,
      "correct lift reaches at least five screen pixels",
    );
    const artifactStart = correctTrace.samples.find((s) => Math.abs(s.y) > 0.5)?.at;
    const hornStart = correctTrace.samples.find((s) => Math.abs(s.horn) > 0.2)?.at;
    const tonearmStart = correctTrace.samples.find((s) => Math.abs(s.tonearm) > 0.1)?.at;
    assert(artifactStart && hornStart > artifactStart, "horn follow-through begins after artifact movement");
    assert(tonearmStart > artifactStart, "tonearm follow-through begins after artifact movement");
    assert(
      moving.every(
        (s) => s.connected && s.ready === "true" && Number(s.opacity) > 0,
      ),
      "the moving SVG stays visible and attached",
    );
    await page.locator(".question-feedback button").click();
    await page.locator(".question-feedback").waitFor({ state: "hidden" });
    const wrongTrace = await answer("wrong", false);
    assert(
      wrongTrace.samples.some(
        (s) => s.motion === "wrong-recoil" && Math.abs(s.angle) > 0.5,
      ),
      "wrong answer visibly rotates the live body",
    );
    await page.locator(".question-feedback button").click();
    await page.locator(".question-feedback").waitFor({ state: "hidden" });
    const cycleTrace = await capture("direct-cycle-hop", () =>
      page.evaluate(async () => {
        const m = await import("/js/gameMotion.js");
        void m.playMotion(
          document.querySelector(".game-studio"),
          "cycle-complete-hop",
          { cycleIndex: 2 },
        );
      }),
    );
    const hopTrace = await capture("direct-topic-hop", () =>
      page.evaluate(async () => {
        const m = await import("/js/gameMotion.js");
        void m.playMotion(
          document.querySelector(".game-studio"),
          "topic-proud-hop",
        );
      }),
    );
    const hopFrames = hopTrace.samples.filter(
      (s) => s.motion === "topic-proud-hop" && s.y < -10,
    );
    assert(
      hopFrames.length > 0 && hopFrames.at(-1).at - hopFrames[0].at >= 200,
      "hop has a readable airborne phase",
    );
    const displacement = (trace) => Math.max(...trace.samples.map((s) => Math.abs(s.y)));
    assert(displacement(cycleTrace) >= 20, "cycle hop reaches at least 20 screen pixels");
    assert(displacement(hopTrace) >= 30, "topic hop reaches at least 30 screen pixels");
    assert(displacement(hopTrace) > displacement(cycleTrace), "topic celebration is stronger than cycle celebration");
    // Exercise the user-facing development controls and save actual browser pixels.
    await page.locator(".motion-tester summary").click();
    await page.locator(".motion-tester button").last().click();
    await page.waitForFunction(() =>
      document
        .querySelector(".motion-tester pre")
        .textContent.includes("controllerFound"),
    );
    await page.locator(".motion-tester select").selectOption("topic-proud-hop");
    await page.locator(".motion-tester button").first().click();
    await page.waitForFunction(
      () =>
        document.querySelector('[data-part="artifact-motion"]').getAnimations()
          .length > 0,
    );
    await page.evaluate(() => {
      window.captureAnimations = document
        .querySelector(".game-studio")
        .getAnimations({ subtree: true });
      window.captureAnimations.forEach((a) => {
        a.pause();
        a.currentTime = 0;
      });
    });
    await page.screenshot({
      path: path.join(root, "artifacts/visible-motion/hop-rest.png"),
    });
    await page.evaluate(() =>
      window.captureAnimations.forEach((a) => (a.currentTime = 450)),
    );
    await page.screenshot({
      path: path.join(root, "artifacts/visible-motion/hop-peak.png"),
    });
    await page.evaluate(() =>
      window.captureAnimations.forEach((a) => a.play()),
    );
    const evidence = async (motion, at, file, options = {}) => {
      const transforms = await page.evaluate(
        async ({ motion, at, options }) => {
          const module = await import("/js/gameMotion.js"),
            host = document.querySelector(".game-studio"),
            body = host.querySelector(
              motion === "enter-drop"
                ? '[data-part="machine-root"]'
                : '[data-part="artifact-motion"]',
            );
          module.cancelMotion(host);
          const rest = getComputedStyle(body).transform;
          void module.playMotion(host, motion, options);
          await new Promise(requestAnimationFrame);
          const active = host.getAnimations({ subtree: true });
          active.forEach((animation) => {
            animation.pause();
            animation.currentTime = at;
          });
          return { rest, active: getComputedStyle(body).transform };
        },
        { motion, at, options },
      );
      assert.notEqual(
        transforms.active,
        transforms.rest,
        motion + " changes the rendered body transform",
      );
      await page.screenshot({
        path: path.join(root, "artifacts/visible-motion", file),
      });
      await page.evaluate(async () => {
        const module = await import("/js/gameMotion.js");
        module.cancelMotion(document.querySelector(".game-studio"));
      });
    };
    await evidence("enter-drop", 1200, "entrance-fall.png", {
      duration: 2800,
    });
    await evidence("correct-pulse", 190, "correct-pulse.png");
    await evidence("cycle-complete-hop", 400, "cycle-hop.png", {
      cycleIndex: 2,
    });
    await evidence("topic-proud-hop", 450, "topic-hop.png", {
      cycleIndex: 2,
    });
    await page.locator(".motion-preference input").check();
    await page.waitForFunction(
      () =>
        document.querySelector("[data-gramophone]").dataset.reducedMotion ===
        "true",
    );
    const reduced = await capture(
      "reduced-hop",
      () => page.locator(".motion-tester button").first().click(),
      500,
    );
    assert(
      reduced.samples.every((s) => Math.abs(s.y) < 0.01),
      "reduced motion suppresses physical lift",
    );
    assert.deepEqual(errors, []);
    console.log(
      "PASS: measured live answer movement, readable phase timing, visible DOM, developer preview, and reduced motion" +
        (process.argv.includes("--blocked-audio")
          ? " with audio permanently blocked."
          : "."),
    );
  } finally {
    await page.close();
    await browser.close();
    if (id) db.prepare("DELETE FROM Sessions WHERE Id=?").run(id);
    db.close();
  }
})().catch((e) => {
  console.error(e);
  process.exitCode = 1;
});
