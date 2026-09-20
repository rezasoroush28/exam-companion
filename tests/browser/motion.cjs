// Development host required on :5175. Only this script's new session is deleted.
const assert = require("node:assert/strict");
const path = require("node:path");
const { DatabaseSync } = require("node:sqlite");
let chromium;
try {
  ({ chromium } = require("playwright"));
} catch {
  ({ chromium } = require("../../.tools/browser/node_modules/playwright"));
}
const db = new DatabaseSync(
  path.resolve(__dirname, "../../data/exam-companion.db"),
);
db.exec("PRAGMA foreign_keys=ON; PRAGMA busy_timeout=5000;");
let session;
(async () => {
  const browser = await chromium.launch({ channel: "msedge", headless: true });
  const page = await browser.newPage({
    viewport: { width: 1440, height: 950 },
  });
  const errors = [];
  page.on("pageerror", (e) => errors.push(e.message));
  await page.addInitScript(() => {
    window.motionEvents = [];
    for (const name of [
      "gramophone-motion-started",
      "gramophone-motion-completed",
      "gramophone-entrance-complete",
      "playback-started",
      "playback-completed",
      "scratch-approaching",
      "scratch-hit",
      "repair-completed",
    ])
      document.addEventListener(
        name,
        (e) =>
          window.motionEvents.push({
            name,
            ...e.detail,
            at: performance.now(),
          }),
        true,
      );
  });
  const waitMotion = (name) =>
    page.waitForFunction(
      (name) =>
        window.motionEvents.some(
          (e) => e.name === "gramophone-motion-started" && e.motion === name,
        ),
      name,
    );
  const resetEvents = () =>
    page.evaluate(() => {
      window.motionEvents = [];
    });
  const invoke = (name, options = {}) =>
    page.evaluate(
      async ({ name, options }) => {
        const m = await import("/js/gameMotion.js");
        await m.playMotion(
          document.querySelector(".game-studio"),
          name,
          options,
        );
      },
      { name, options },
    );
  const answer = async (correct = true, waitFor) => {
    const s = db
      .prepare(
        "SELECT q.CorrectOption FROM Sessions s JOIN Questions q ON q.Id=s.CurrentQuestionId WHERE s.Id=?",
      )
      .get(session);
    const key = correct ? s.CorrectOption : s.CorrectOption === "A" ? "B" : "A";
    await resetEvents();
    await page
      .locator(".answer-option")
      .filter({
        has: page.locator(".answer-letter", {
          hasText: new RegExp("^" + key + "$"),
        }),
      })
      .click();
    await page.locator(".question-feedback").waitFor();
    if (waitFor) await waitMotion(waitFor);
    await page.locator(".question-feedback button").click();
    await page.locator(".question-feedback").waitFor({ state: "hidden" });
  };
  try {
    await page.goto(`${process.env.EXAM_URL || "http://localhost:5175"}/`);
    await page.locator(".start-game:not([disabled])").waitFor();
    await page.evaluate(() => document.fonts.ready);
    await page.locator(".start-game:not([disabled])").click();
    await page.waitForURL("**/game/*");
    session = page.url().split("/").pop().toUpperCase();
    await waitMotion("walk-in");
    assert.equal(await page.locator(".question-stage").evaluate(e => e.inert), true);
    assert.equal(await page.locator('[data-part="record-root"]').evaluate(e => getComputedStyle(e).visibility), "hidden");
    const assetBoxes = await page.locator('[data-gramophone] use').evaluateAll(nodes => nodes.map(n => n.getBBox().width));
    assert(assetBoxes.length >= 10 && assetBoxes.every(w => w > 0), "editable SVG assets resolve and render");
    const initialBox = await page.locator(".question-stage").boundingBox();
    await page.waitForFunction(
      () => document.querySelector(".game-studio").dataset.entering === "false",
    );
    const entry = await page.evaluate(() =>
      window.motionEvents.filter((e) => e.name === "gramophone-motion-started"),
    );
    assert.deepEqual(
      entry.map((e) => e.motion),
      [
        "walk-in",
        "stop-and-settle",
        "sit-settle",
        "notice-pocket",
        "reach-pocket",
        "open-pocket",
        "grab-disc",
        "pull-disc",
        "hold-disc",
        "place-disc",
        "record-wobble-settle",
        "wake",
        "listen-lean",
      ],
    );
    [0, 1800, 2050, 2550, 2800, 3250, 3500, 3850, 4400, 4900, 5500, 5800, 6300].forEach((ms, i) =>
      assert(
        Math.abs(entry[i].at - entry[0].at - ms) < 400,
        `entrance staging drift: ${entry[i].motion} started at ${entry[i].at - entry[0].at}, expected ${ms}; ${JSON.stringify(entry.map((e) => [e.motion, e.at - entry[0].at]))}`,
      ),
    );
    const entryComplete = await page.evaluate(() => window.motionEvents.find((e) => e.name === "gramophone-entrance-complete"));
    assert(Math.abs(entryComplete.at - entry[0].at - 6720) < 650, "full entrance timing includes hold and mounted-record tail");
    assert.equal(await page.locator(".answer-option").first().evaluate((e) => document.activeElement === e), true, "entrance focuses question after record is ready");
    assert.equal(await page.locator('[data-part="record-prop"]').evaluate(e => getComputedStyle(e).visibility), "hidden");
    assert.equal(await page.locator('[data-part="record-root"]').evaluate(e => getComputedStyle(e).visibility), "visible");
    const finalBox = await page.locator(".question-stage").boundingBox();
    assert(
      Math.abs(initialBox.y - finalBox.y) < 1 &&
        Math.abs(initialBox.x - finalBox.x) < 1,
      "no question layout shift",
    );
    assert.equal(
      await page.locator("[data-gramophone] image").count(),
      0,
      "all native SVG",
    );
    for (const part of [
      "gram-shadow",
      "gram-body",
      "gram-top-plane",
      "turntable-housing",
      "record-root",
      "record-disc",
      "topic-grooves",
      "record-label",
      "tonearm-root",
      "tonearm-arm",
      "needle",
      "horn-support",
      "horn-root",
      "horn-shell",
      "playhead",
      "scratch-layer",
      "impact-fx",
      "sparkle-fx",
    ])
      assert((await page.locator(`[data-part="${part}"]`).count()) > 0, part);
    assert.equal(
      await page.locator('.groove[aria-disabled="true"]').count(),
      4,
    );
    await page.reload();
    await waitMotion("listen-lean");
    assert.equal(
      await page
        .locator("[data-gramophone]")
        .getAttribute("data-entrance-count"),
      null,
      "refresh does not replay arrival",
    );
    await answer(true, "correct-pulse");
    await resetEvents();
    await page.locator('.groove[aria-disabled="false"]').focus();
    await page.locator('.groove[aria-disabled="false"]').press("Enter");
    await page.waitForFunction(() =>
      window.motionEvents.some((e) => e.name === "scratch-hit"),
    );
    await page.waitForFunction(() =>
      window.motionEvents.some((e) => e.name === "playback-completed"),
    );
    const scratch = await page.evaluate(() =>
      window.motionEvents.filter((e) => e.name === "scratch-hit"),
    );
    assert.equal(scratch.length, 1, "one scratch callback per phrase");
    assert(
      scratch[0].visualTime - scratch[0].scheduledTime < 0.1,
      "shared audio timing",
    );
    // Exercise every public motion, including multi-cycle rewards not reached in this one-cycle catalog.
    const vocab = await page.evaluate(
      async () => (await import("/js/gameMotion.js")).motionVocabulary,
    );
    for (const name of vocab) {
      await invoke(name, { cycleIndex: 2 });
      assert.equal(
        await page.locator("[data-gramophone]").getAttribute("data-motion"),
        "idle",
        name + " settles",
      );
    }
    await invoke("cycle-complete-hop", { cycleIndex: 2 });
    const even = await page
      .locator('[data-part="artifact-rest"]')
      .evaluate((e) => e.style.transform);
    await invoke("cycle-complete-hop", { cycleIndex: 3 });
    const odd = await page
      .locator('[data-part="artifact-rest"]')
      .evaluate((e) => e.style.transform);
    assert(
      even.includes("-0.7deg") && odd.includes("0.7deg"),
      "deterministic alternating cycle pose",
    );
    await page.locator(".motion-preference input").check();
    const reducedFrames = await page.evaluate(async () => {
      const m = await import("/js/gameMotion.js"),
        host = document.querySelector(".game-studio");
      void m.playMotion(host, "topic-proud-hop");
      return host
        .getAnimations({ subtree: true })
        .flatMap((a) => a.effect.getKeyframes());
    });
    assert(
      reducedFrames.every((frame) => !frame.transform),
      "reduced major reward has no physical transform",
    );
    for (const name of vocab) {
      await invoke(name);
      assert.equal(
        await page
          .locator("[data-gramophone]")
          .getAttribute("data-reduced-motion"),
        "true",
      );
    }
    await page.reload();
    await waitMotion("listen-lean");
    assert(
      await page.locator(".motion-preference input").isChecked(),
      "preference persists",
    );
    await page.locator(".motion-preference input").uncheck();
    await page.emulateMedia({ reducedMotion: "reduce" });
    await invoke("topic-proud-hop");
    assert.equal(
      await page
        .locator("[data-gramophone]")
        .getAttribute("data-reduced-motion"),
      "true",
      "OS wins over unchecked preference",
    );
    await page.emulateMedia({ reducedMotion: "no-preference" });
    await answer(false, "wrong-recoil");
    // Finish remaining questions using authoritative keys; wait for actual milestone delivery.
    let turns = 0,
      topicHop = false;
    while (!(await page.locator(".completion-panel").count())) {
      assert(++turns < 30, "bounded progression");
      const s = db
        .prepare(
          "SELECT q.Type FROM Sessions s JOIN Questions q ON q.Id=s.CurrentQuestionId WHERE s.Id=?",
        )
        .get(session);
      const motion = s.Type === 1 ? "topic-proud-hop" : "correct-pulse";
      await answer(true, motion);
      if (motion === "topic-proud-hop") topicHop = true;
    }
    assert(topicHop, "real server topic completion triggers celebration");
    await waitMotion("lesson-complete-bow");
    const cadence = await page.evaluate(() => window.motionEvents);
    const completed = cadence.findLast((e) => e.name === "playback-completed");
    const bow = cadence.findLast((e) => e.motion === "lesson-complete-bow");
    assert(
      completed && bow.at >= completed.at,
      "final bow follows final cadence",
    );
    // Cancel a live hop, then leave during another; retain Animation references for disposal assertions.
    await page.evaluate(async () => {
      const m = await import("/js/gameMotion.js"),
        host = document.querySelector(".game-studio");
      void m.playMotion(host, "topic-proud-hop");
      window.oldAnimations = host.getAnimations({ subtree: true });
      m.cancelMotion(host);
    });
    assert(
      await page.evaluate(() =>
        window.oldAnimations.every((a) => a.playState === "idle"),
      ),
      "explicit cancellation clears WAAPI",
    );
    await resetEvents();
    await page.evaluate(async () => {
      const m = await import("/js/gameMotion.js"),
        host = document.querySelector(".game-studio");
      void m.playEntrance(host);
      m.cancelMotion(host);
    });
    await page.waitForTimeout(2000);
    assert.equal(
      await page.evaluate(() =>
        window.motionEvents.some(
          (e) => e.name === "gramophone-entrance-complete",
        ),
      ),
      false,
      "cancel removes scheduled entrance callbacks",
    );
    assert.equal(
      await page.locator(".question-stage").evaluate((e) => e.inert),
      false,
      "cancel restores question interaction",
    );
    await page.evaluate(async () => {
      const m = await import("/js/gameMotion.js");
      void m.playMotion(document.querySelector(".game-studio"), "idle-settle");
      window.oldAnimations = document
        .querySelector(".game-studio")
        .getAnimations({ subtree: true });
    });
    await page.locator(".exit-link").click();
    await page.locator(".start-game").waitFor();
    assert(
      await page.evaluate(
        () =>
          window.oldAnimations?.every((a) => a.playState === "idle") ?? true,
      ),
      "navigation cancels animations",
    );
    assert.deepEqual(errors, [], "no browser exceptions");
    console.log(
      "PASS: native SVG, articulated entrance timing/no replay/no layout shift, public motions, cycle poses, keyboard preview, one synchronized scratch, answer milestones, final cadence bow, reduced motion, cancellation/navigation.",
    );
  } finally {
    await browser.close();
    if (session) db.prepare("DELETE FROM Sessions WHERE Id=?").run(session);
    db.close();
  }
})().catch((e) => {
  console.error(e);
  process.exitCode = 1;
});
