// Geometry checks use the rendered SVG matrices, not the sampler's own claims.
const assert = require("node:assert/strict");
const path = require("node:path");
const fs = require("node:fs");
let chromium;
try { ({ chromium } = require("playwright")); }
catch { ({ chromium } = require("../../.tools/browser/node_modules/playwright")); }

(async () => {
  const browser = await chromium.launch({ channel: "msedge", headless: true });
  const page = await browser.newPage({ viewport: { width: 1440, height: 1100 } });
  const errors = [];
  page.on("pageerror", (error) => errors.push(error.message));
  const artifacts = path.resolve(__dirname, "../../artifacts/entrance-rig");
  fs.mkdirSync(artifacts, { recursive: true });
  try {
    await page.goto(`${process.env.EXAM_URL || "http://localhost:5175"}/motion-lab`);
    await page.getByRole("button", { name: "rig-pull-disc", exact: true }).waitFor();
    await page.waitForFunction(() => !document.querySelector(".motion-lab-actions button").disabled);
    const model = await page.evaluate(async () => {
      const r = await import("/js/gramophoneEntranceRig.js"), g = r.entranceRigDefaults;
      const neutralErrors = [g.left, g.right].map((p) => {
        const s = r.solveTwoLink({ root: p.hip, joint: p.knee, end: p.ankle, target: p.ankle });
        return Math.hypot(s.end.x - p.ankle.x, s.end.y - p.ankle.y);
      });
      const unreachable = r.solveTwoLink({ root: g.left.hip, joint: g.left.knee, end: g.left.ankle, target: { x: 9999, y: 9999 } });
      const boundaryErrors = r.entranceStages.slice(1).map((s, i) => {
        const a = r.sampleEntrancePose(r.entranceStages[i].name, 1), b = r.sampleEntrancePose(s.name, 0);
        return Math.max(Math.abs(a.bodyY - b.bodyY), Math.abs(a.left.thigh - b.left.thigh), Math.abs(a.left.shin - b.left.shin),
          Math.abs(a.rightArm - b.rightArm), Math.abs(a.rightForearm - b.rightForearm));
      });
      return { neutralErrors, unreachable, boundaryErrors, durations: r.entranceStages.map((s) => s.duration) };
    });
    assert(model.neutralErrors.every((error) => error < 0.001), "neutral IK reconstructs original links");
    assert(model.unreachable.clamped && Number.isFinite(model.unreachable.end.x), "unreachable target clamps without NaN");
    assert(model.boundaryErrors.every((error) => error < 0.001), "adjacent stages have continuous joints");
    assert.deepEqual(model.durations, [1800, 250, 500, 250, 450, 250, 350, 550, 500]);
    const sample = async (stage, progress) => page.evaluate(async ({ stage, progress }) => {
      const m = await import("/js/gameMotion.js");
      const rig = await import("/js/gramophoneEntranceRig.js");
      const host = document.querySelector(".motion-lab"), c = m.create(host);
      c.setReducedMotion(false); c.preview(stage); c.pause();
      const duration = rig.entranceStages.find((s) => s.name === stage)?.duration || 600;
      c.seekFrameEntrance(duration * progress);
      const part = (name) => host.querySelector(`[data-part="${name}"]`);
      const position = (name, x, y, reference = "machine-root") => {
        const matrix = part(reference).getScreenCTM().inverse().multiply(part(name).getScreenCTM());
        const p = new DOMPoint(x, y).matrixTransform(matrix);
        return { x: p.x, y: p.y };
      };
      const geometry = rig.readRigGeometry({ leftLeg: part("left-leg"), rightLeg: part("right-leg"), rightArm: part("right-arm"), pocketRoot: part("pocket-root") });
      const pose = rig.sampleEntrancePose(stage, progress, geometry);
      return {
        wrist: position("right-forearm", geometry.arm.wrist.x, geometry.arm.wrist.y),
        disc: position("record-prop", 0, 0),
        expectedWrist: pose.diagnostics.wrist,
        expectedDisc: pose.disc.center,
        leftFoot: position("left-foot", geometry.left.ankle.x, geometry.left.ankle.y, "gramophone-root"),
        rightFoot: position("right-foot", geometry.right.ankle.x, geometry.right.ankle.y, "gramophone-root"),
        expectedLeft: { x: pose.left.target.x + pose.locomotionX, y: pose.left.target.y },
        expectedRight: { x: pose.right.target.x + pose.locomotionX, y: pose.right.target.y },
        leftAngle: Math.atan2(part("left-foot").getScreenCTM().b, part("left-foot").getScreenCTM().a),
        bodyY: new DOMMatrix(getComputedStyle(part("machine-root")).transform).f,
        shin: getComputedStyle(part("left-shin")).transform,
        flap: getComputedStyle(part("pocket-flap")).transform,
        visibility: getComputedStyle(part("record-prop")).visibility,
        opacity: Number(getComputedStyle(part("record-prop")).opacity),
        diagnostics: c.diagnostics(),
      };
    }, { stage, progress });
    const near = (a, b, message, tolerance = 2) =>
      assert(Math.hypot(a.x - b.x, a.y - b.y) < tolerance, `${message}: ${JSON.stringify({ actual: a, expected: b })}`);

    for (const width of [1440, 390]) {
      await page.setViewportSize({ width, height: 1100 });
      for (const stage of ["rig-walk-in", "rig-sit-settle", "rig-grab-disc", "rig-pull-disc", "rig-hold-disc"]) {
        for (const progress of [0.1, 0.37, 0.73, 0.99]) {
          const s = await sample(stage, progress);
          near(s.leftFoot, s.expectedLeft, `${width} ${stage} left ankle`);
          near(s.rightFoot, s.expectedRight, `${width} ${stage} right ankle`);
          assert(Math.abs(s.leftAngle) < 0.015, "foot counter-rotation keeps the sole level");
          assert.equal(s.diagnostics.rig.clamped, false, "intended pose must be reachable");
          if (["rig-grab-disc", "rig-pull-disc", "rig-hold-disc"].includes(stage)) {
            near(s.wrist, s.expectedWrist, "live wrist matches solved target");
            near(s.disc, s.expectedDisc, "live disc matches solved center");
            assert(Math.abs(Math.hypot(s.wrist.x - s.disc.x, s.wrist.y - s.disc.y) - Math.hypot(32, 15)) < 2, "live rigid grip");
          }
        }
      }
      const start = await sample("rig-walk-in", 0.02), end = await sample("rig-walk-in", 0.98);
      assert(end.expectedLeft.x - start.expectedLeft.x > 800, "large horizontal arrival");
      near((await sample("rig-walk-in", 0.01)).leftFoot, (await sample("rig-walk-in", 0.09)).leftFoot, "left stance is planted");
      near((await sample("rig-walk-in", 0.14)).rightFoot, (await sample("rig-walk-in", 0.22)).rightFoot, "right stance is planted");
      assert((await sample("rig-walk-in", 0.0625)).rightFoot.y < 1080, "right swing lifts");
      assert((await sample("rig-walk-in", 0.1875)).leftFoot.y < 1070, "left swing lifts");
      const sitStart = await sample("rig-sit-settle", 0), sitEnd = await sample("rig-sit-settle", 0.99);
      assert(sitEnd.bodyY - sitStart.bodyY > 40, "sit lowers torso");
      assert.notEqual(sitStart.shin, sitEnd.shin, "sit articulates knee");
      const opening = await sample("rig-open-pocket", 0.5);
      assert(opening.opacity > 0 && opening.opacity < 1, "partial reveal");
      assert.notEqual(opening.flap, "matrix(1, 0, 0, 1, 0, 0)", "flap opens");
      await sample("rig-hold-disc", 0.5);
      await page.locator(".motion-lab-stage").screenshot({ path: path.join(artifacts, `hold-${width}.png`) });
    }
    await page.setViewportSize({ width: 1440, height: 1100 });
    await page.evaluate(async () => {
      const m = await import("/js/gameMotion.js"), h = document.querySelector(".motion-lab"), c = m.create(h);
      c.reset(); c.setSlowMotion(true); c.preview("prototype-entrance");
      window.testRigController = c;
    });
    await page.waitForFunction(() => {
      const d = window.testRigController.diagnostics();
      return d.rig?.stage === "rig-hold-disc" && d.rig.currentProgress === 1 && d.activeAnimations === 0;
    }, null, { timeout: 20000 });
    const slowEnd = await page.evaluate(async () => {
      const m = await import("/js/gameMotion.js"), h = document.querySelector(".motion-lab"), c = m.create(h);
      const d = c.diagnostics(); c.setSlowMotion(false); return d;
    });
    assert.equal(slowEnd.rig.currentProgress, 1, "complete slow preview reaches a stable hold");
    assert(Math.abs(slowEnd.rig.rendered.attachmentDistance - Math.hypot(32, 15)) < 2);
    const lifecycle = await page.evaluate(async () => {
      const m = await import("/js/gameMotion.js"), r = await import("/js/gramophoneEntranceRig.js");
      const h = document.querySelector(".motion-lab"), c = m.create(h);
      const sleep = (ms) => new Promise((resolve) => setTimeout(resolve, ms));
      const style = (name, property) => getComputedStyle(h.querySelector(`[data-part="${name}"]`))[property];
      const completions = [];
      h.addEventListener("gramophone-entrance-complete", () => completions.push(performance.now()));
      const clean = () => c.diagnostics().activeAnimations === 0 && Object.keys(c.diagnostics().activeChannels).length === 0
        && style("record-root", "visibility") === "visible" && style("record-prop", "visibility") === "hidden"
        && ["left-thigh", "right-shin", "pocket-flap", "horn-follow"].every((p) => style(p, "transform") === "none");
      c.reset(); c.setSlowMotion(true); c.preview("rig-pull-disc");
      await sleep(140); c.pause(); await sleep(40);
      const paused = c.diagnostics().rig.currentProgress;
      const pausedWrist = c.diagnostics().rig.rendered.wrist;
      await sleep(150);
      const frozen = paused === c.diagnostics().rig.currentProgress
        && JSON.stringify(pausedWrist) === JSON.stringify(c.diagnostics().rig.rendered.wrist);
      c.resume(); await sleep(160);
      const advance = c.diagnostics().rig.currentProgress - paused;
      c.setSlowMotion(false);
      const rateJump = Math.abs(c.diagnostics().rig.currentProgress - (paused + advance));
      c.reset();
      const cancellation = [];
      for (const stage of r.entranceStageNames) {
        c.preview(stage); await sleep(20); c.cancelMotion(); await sleep(10);
        cancellation.push(clean());
      }
      c.preview("rig-walk-in"); c.preview("rig-walk-in"); await sleep(50);
      const replacementRunning = c.diagnostics().motion === "rig-walk-in" && c.diagnostics().activeChannels["gram-locomotion"] === "rig-walk-in";
      c.reset(); void c.playEntrance(); await sleep(25);
      Object.defineProperty(document, "hidden", { configurable: true, value: true });
      document.dispatchEvent(new Event("visibilitychange"));
      delete document.hidden; await sleep(30);
      const hiddenClean = clean();
      void c.playEntrance(); await sleep(30);
      c.setReducedMotion(true); await sleep(30);
      const reductionClean = clean();
      const reducedNames = [];
      const listener = (e) => reducedNames.push(e.detail.motion);
      h.addEventListener("gramophone-motion-started", listener);
      await c.playEntrance();
      h.removeEventListener("gramophone-motion-started", listener);
      const reducedClean = clean(), beforeReplacement = completions.length;
      c.setReducedMotion(false); void c.playEntrance(); await sleep(40);
      const old = h.querySelector("[data-gramophone]"), detachedAnimations = h.getAnimations({ subtree: true }); old.outerHTML = old.outerHTML;
      // Complete the detached stage to exercise its continuation/generation guard.
      for (const a of detachedAnimations) a.finish();
      await sleep(50);
      const replacedClean = clean() && completions.length === beforeReplacement;
      c.preview("prototype-entrance"); await sleep(30); c.dispose(); await sleep(30);
      return { frozen, advance, rateJump, cancellation, replacementRunning, hiddenClean, reductionClean,
        reducedNames, reducedClean, replacedClean, disposedClean: clean(), completions: completions.length };
    });
    assert(lifecycle.frozen, "pause freezes diagnostics and rendered rig together");
    assert(lifecycle.advance > 0.06 && lifecycle.advance < 0.18, "0.35x advances the shared timeline smoothly");
    assert(lifecycle.rateJump < 0.02, "rate switch does not jump diagnostics");
    assert(lifecycle.cancellation.every(Boolean), "every rig stage cancels to a coherent idle state");
    assert(lifecycle.replacementRunning, "stale same-name completion cannot clear a replacement run");
    assert(lifecycle.hiddenClean && lifecycle.reductionClean && lifecycle.reducedClean && lifecycle.replacedClean && lifecycle.disposedClean, JSON.stringify(lifecycle));
    assert.deepEqual(lifecycle.reducedNames, ["place-disc", "wake", "listen-lean"]);
    assert.equal(lifecycle.completions, 1, "only the completed reduced entrance publishes readiness");
    assert.deepEqual(errors, []);
    console.log("PASS: rendered IK, planted stance, swing, sit, pocket, rigid grip, desktop/mobile, pause/rate, cancel/replacement/hidden/reduced/dispose.");
  } finally { await browser.close(); }
})().catch((error) => { console.error(error); process.exitCode = 1; });
