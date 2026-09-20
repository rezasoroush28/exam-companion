const assert = require("node:assert/strict");
let chromium;
try {
  ({ chromium } = require("playwright"));
} catch {
  ({ chromium } = require("../../.tools/browser/node_modules/playwright"));
}

(async () => {
  const browser = await chromium.launch({ channel: "msedge", headless: true });
  const page = await browser.newPage({
    viewport: { width: 1440, height: 950 },
  });
  const errors = [];
  page.on("pageerror", (e) => errors.push(e.message));
  try {
    await page.goto(`${process.env.EXAM_URL || "http://localhost:5175"}/motion-lab`);
    await page.locator("[data-gramophone]").waitFor();
    assert.equal(
      await page.locator(".motion-lab-motion-grid button").count(),
      28,
      "controls include the original entrance vocabulary and the isolated articulated rig stages",
    );
    await page.getByRole("button", { name: "وضعیت" }).click();
    await page.waitForFunction(() =>
      document
        .querySelector(".motion-lab-controls pre")
        .textContent.includes("resolvedParts"),
    );
    const status = JSON.parse(
      await page.locator(".motion-lab-controls pre").textContent(),
    );
    assert(
      status.hostConnected &&
        status.targetsConnected &&
        status.resolvedParts >= 19,
      "live controller resolves all required SVG parts",
    );
    assert.deepEqual(status.missingParts, []);
    assert.equal(typeof status.activeChannels, "object");
    const hierarchy = await page.locator("[data-gramophone]").evaluate((g) => {
      const rest = g.querySelector('[data-part="artifact-rest"]'),
        motion = g.querySelector('[data-part="artifact-motion"]'),
        machine = g.querySelector('[data-part="machine-root"]');
      return {
        restContainsMotion: rest?.contains(motion),
        motionContainsMachine: motion?.contains(machine),
        machineContainsHorn: machine?.contains(g.querySelector('[data-part="horn-follow"]')),
        machineContainsTonearm: machine?.contains(g.querySelector('[data-part="tonearm-follow"]')),
        machineContainsRecord: machine?.contains(g.querySelector('[data-part="record-stage"]')),
      };
    });
    assert(Object.values(hierarchy).every(Boolean), "the complete living-object rig hierarchy is rendered");
    const articulatedRig = await page.locator("[data-gramophone]").evaluate((g) => {
      const names = ["left-thigh", "left-shin", "right-thigh", "right-shin", "pocket-root", "pocket-front", "pocket-flap"];
      return {
        names: names.filter((name) => g.querySelector(`[data-part="${name}"]`)),
        pivots: {
          leftHip: g.querySelector('[data-part="left-leg"]')?.dataset.hipX,
          leftKnee: g.querySelector('[data-part="left-leg"]')?.dataset.kneeX,
          rightWrist: g.querySelector('[data-part="right-arm"]')?.dataset.wristY,
          flap: g.querySelector('[data-part="pocket-root"]')?.dataset.flapX,
        },
      };
    });
    assert.equal(articulatedRig.names.length, 7, "all split rig links are inspectable");
    assert.deepEqual(articulatedRig.pivots, { leftHip: "452", leftKnee: "466", rightWrist: "711", flap: "765" });
    assert.equal(await page.getByRole("button", { name: "prototype-entrance", exact: true }).count(), 1);
    assert.equal(await page.getByRole("button", { name: "rig-pull-disc", exact: true }).count(), 1);
    assert.equal(
      await page.evaluate(async () => {
        const m = await import("/js/gameMotion.js"),
          h = document.querySelector(".motion-lab");
        return m.create(h) === m.createController(h);
      }),
      true,
      "one controller per host",
    );

    const machine = page.locator('[data-part="machine-root"]');
    const rest = await machine.boundingBox();
    await page.getByRole("button", { name: "enter-drop", exact: true }).click();
    await page.waitForTimeout(650);
    const falling = await machine.boundingBox();
    assert(
      Math.abs(falling.y - rest.y) > 80,
      "isolated enter-drop visibly travels while slow preview is active",
    );
    await page.getByRole("button", { name: "مکث" }).click();
    await page.waitForTimeout(100);
    const paused = await machine.evaluate((e) => getComputedStyle(e).transform);
    await page.waitForTimeout(250);
    assert.equal(
      await machine.evaluate((e) => getComputedStyle(e).transform),
      paused,
      "pause freezes motion",
    );
    await page.getByRole("button", { name: "ادامه" }).click();
    await page.waitForTimeout(150);
    assert.notEqual(
      await machine.evaluate((e) => getComputedStyle(e).transform),
      paused,
      "resume continues motion",
    );
    await page.getByRole("button", { name: "لغو" }).click();
    await page.waitForTimeout(100);
    assert.equal(
      await machine.evaluate((e) => e.getAnimations().length),
      0,
      "cancel clears active WAAPI animation",
    );
    await page.getByRole("button", { name: "بازنشانی" }).click();

    // Replace the component root as a Blazor rerender can, then require a fresh binding.
    const rebound = await page.evaluate(async () => {
      const h = document.querySelector(".motion-lab"),
        old = h.querySelector("[data-gramophone]");
      old.outerHTML = old.outerHTML;
      const live = h.querySelector("[data-gramophone]"),
        m = await import("/js/gameMotion.js");
      const binding = m.refreshParts(h);
      void m.playMotion(h, "topic-proud-hop", { cycleIndex: 2 });
      await new Promise(requestAnimationFrame);
      await new Promise(requestAnimationFrame);
      return {
        oldConnected: old.isConnected,
        liveConnected: live.isConnected,
        binding,
        animations: live
          .querySelector('[data-part="artifact-motion"]')
          .getAnimations().length,
      };
    });
    assert(
      !rebound.oldConnected && rebound.liveConnected && rebound.animations > 0,
      "controller animates replacement SVG nodes",
    );
    assert.deepEqual(rebound.binding.missing, []);

    // The articulated preview is isolated from the production entrance but uses
    // the live SVG and reports its hand/disc attachment through diagnostics.
    await page.evaluate(async () => {
      const m = await import("/js/gameMotion.js"), h = document.querySelector(".motion-lab"), controller = m.create(h);
      controller.reset();
      controller.preview("rig-sit-settle");
    });
    await page.waitForTimeout(220);
    assert(
      Math.abs(await machine.evaluate((e) => new DOMMatrix(getComputedStyle(e).transform).f)) > 15,
      "rig sit visibly lowers the body",
    );
    await page.evaluate(async () => {
      const m = await import("/js/gameMotion.js"), h = document.querySelector(".motion-lab");
      m.create(h).preview("rig-open-pocket");
    });
    await page.waitForTimeout(110);
    assert.notEqual(
      await page.locator('[data-part="pocket-flap"]').evaluate((e) => getComputedStyle(e).transform),
      "none",
      "rig preview rotates the pocket flap",
    );
    await page.evaluate(async () => {
      const m = await import("/js/gameMotion.js"), h = document.querySelector(".motion-lab");
      m.create(h).preview("rig-pull-disc");
    });
    await page.waitForTimeout(160);
    const pullStatus = await page.evaluate(async () => {
      const m = await import("/js/gameMotion.js"), h = document.querySelector(".motion-lab");
      return m.create(h).diagnostics();
    });
    assert.equal(pullStatus.rig.stage, "rig-pull-disc");
    assert(Math.abs(pullStatus.rig.attachmentDistance - Math.hypot(32, 15)) < 2,
      "disc remains attached to the solved wrist during pull");
    assert(Math.abs(pullStatus.rig.rendered.attachmentDistance - Math.hypot(32, 15)) < 2,
      "rendered wrist and disc satisfy the grip constraint");
    assert.equal(await page.locator('[data-part="record-prop"]').evaluate((e) => getComputedStyle(e).visibility), "visible");
    await page.evaluate(async () => {
      const m = await import("/js/gameMotion.js"), h = document.querySelector(".motion-lab");
      m.create(h).reset();
    });
    await page.evaluate(async () => {
      const m = await import("/js/gameMotion.js"), h = document.querySelector(".motion-lab");
      m.create(h).preview("prototype-entrance");
    });
    await page.waitForTimeout(5200);
    const completedRig = await page.evaluate(async () => {
      const m = await import("/js/gameMotion.js"), h = document.querySelector(".motion-lab");
      return m.create(h).diagnostics();
    });
    assert.equal(completedRig.rig.stage, "rig-hold-disc", "full articulated preview reaches the held-disc pose");
    assert.equal(await page.locator('[data-part="record-prop"]').evaluate((e) => getComputedStyle(e).visibility), "visible");
    await page.evaluate(async () => {
      const m = await import("/js/gameMotion.js"), h = document.querySelector(".motion-lab");
      m.create(h).reset();
    });

    const toggles = page.locator(".motion-lab-toggles input");
    await toggles.nth(1).check();
    await page.waitForFunction(() => document.querySelector(".motion-lab-controls pre").textContent.includes('"motionRate": 0.35'));
    await toggles.nth(2).check();
    await page.waitForFunction(() => document.querySelector(".motion-lab")?.dataset.showPivots === "true");
    assert.equal(await page.locator(".motion-lab").getAttribute("data-show-pivots"), "true");
    await toggles.nth(0).check();
    await page
      .getByRole("button", { name: "topic-proud-hop", exact: true })
      .click();
    await page.waitForTimeout(80);
    assert.equal(
      await page.locator('[data-part="artifact-motion"]').evaluate(
        (e) => new DOMMatrix(getComputedStyle(e).transform).f,
      ),
      0,
      "reduced motion substitutes emphasis without a hop",
    );
    const reducedRigRest = await machine.evaluate((e) => getComputedStyle(e).transform);
    await page.evaluate(async () => {
      const m = await import("/js/gameMotion.js"), h = document.querySelector(".motion-lab");
      m.create(h).preview("prototype-entrance");
    });
    await page.waitForTimeout(180);
    assert.equal(
      await machine.evaluate((e) => getComputedStyle(e).transform),
      reducedRigRest,
      "reduced articulated preview skips physical gait and sit travel",
    );
    assert.equal(
      await page.locator('[data-part="record-prop"]').evaluate((e) => getComputedStyle(e).transform),
      "none",
      "reduced articulated preview skips prop transport",
    );
    assert.deepEqual(errors, []);
    console.log(
      "PASS: isolated lab controls, slow visible drop, pause/resume/cancel/reset, singleton controller, live SVG rebinding, diagnostics, and reduced motion.",
    );
  } finally {
    await browser.close();
  }
})().catch((e) => {
  console.error(e);
  process.exitCode = 1;
});
