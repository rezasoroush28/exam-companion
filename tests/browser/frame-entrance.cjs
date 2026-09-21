// Proves that the prototype-covered entrance is rendered by one controller
// requestAnimationFrame loop rather than per-part Web Animations.
const assert = require("node:assert/strict");
let chromium;
try { ({ chromium } = require("playwright")); }
catch { ({ chromium } = require("../../.tools/browser/node_modules/playwright")); }

(async () => {
  const browser = await chromium.launch({ channel: "msedge", headless: true });
  try {
    const page = await browser.newPage({ viewport: { width: 1280, height: 900 } });
    await page.addInitScript(() => {
      window.__rigWaapiStarts = [];
      const animate = Element.prototype.animate;
      Element.prototype.animate = function (...args) {
        window.__rigWaapiStarts.push(this?.dataset?.part || this?.id || this?.tagName);
        return animate.apply(this, args);
      };
    });
    await page.goto(`${process.env.EXAM_URL || "http://localhost:5175"}/motion-lab`);
    await page.waitForFunction(() => !document.querySelector(".motion-lab-actions button")?.disabled);
    await page.waitForTimeout(700);
    const running = await page.evaluate(async () => {
      const motion = await import("/js/gameMotion.js");
      const controller = motion.create(document.querySelector(".motion-lab"));
      controller.setReducedMotion(false);
      controller.preview("prototype-entrance");
      await new Promise((resolve) => setTimeout(resolve, 180));
      return { diagnostics: controller.diagnostics(), waapi: window.__rigWaapiStarts };
    });
    assert(running.diagnostics.rig, JSON.stringify(running));
    assert.equal(running.diagnostics.rig.runtime, "requestAnimationFrame");
    assert.equal(running.diagnostics.rig.activeFrameLoops, 1);
    assert(running.diagnostics.rig.frameCount > 1, "frame runner advanced");
    const rigParts = new Set(["gram-locomotion", "machine-root", "left-arm", "right-arm", "right-forearm",
      "left-thigh", "left-shin", "right-thigh", "right-shin", "left-foot", "right-foot", "pocket-flap", "record-prop"]);
    assert.equal(running.waapi.filter((name) => rigParts.has(name)).length, 0, "rig entrance started no WAAPI players");
    const cancelled = await page.evaluate(async () => {
      const motion = await import("/js/gameMotion.js");
      const controller = motion.create(document.querySelector(".motion-lab"));
      controller.cancelMotion();
      await new Promise((resolve) => setTimeout(resolve, 60));
      return controller.diagnostics();
    });
    assert.equal(cancelled.rig, null, "cancellation clears frame runtime diagnostics");
    assert.equal(cancelled.activeAnimations, 0);
    console.log("PASS: one rAF entrance runner advances without rig WAAPI players and cancels cleanly.");
  } finally {
    await browser.close();
  }
})().catch((error) => { console.error(error); process.exitCode = 1; });
