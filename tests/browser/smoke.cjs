// Run against the local Development server. Deletes only sessions created by this test.
const path = require("node:path");
const fs = require("node:fs");
const assert = require("node:assert/strict");
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
const sessions = [];
const artifacts = path.join(root, "artifacts");
fs.mkdirSync(artifacts, { recursive: true });
(async () => {
  const browser = await chromium.launch({ channel: "msedge", headless: true });
  const page = await browser.newPage({
    viewport: { width: 1440, height: 900 },
  });
  const errors = [];
  page.on("pageerror", (e) => errors.push(e.message));
  page.on("console", (m) => {
    if (m.type() === "error" && !m.text().includes("fonts.googleapis"))
      errors.push(m.text());
  });
  const snap = async (name) => {
    await page.evaluate(() => document.fonts.ready);
    await page.screenshot({
      path: path.join(artifacts, name + ".png"),
      fullPage: true,
      animations: "disabled",
    });
    if (
      await page.evaluate(
        () => document.documentElement.scrollWidth > innerWidth,
      )
    )
      console.log(
        await page.evaluate(() =>
          [...document.querySelectorAll("body *")]
            .filter((e) => {
              const r = e.getBoundingClientRect();
              return r.left < -1 || r.right > innerWidth + 1;
            })
            .map((e) => ({
              tag: e.tagName,
              cls: e.getAttribute("class"),
              left: e.getBoundingClientRect().left,
              right: e.getBoundingClientRect().right,
            })),
        ),
      );
    assert(
      await page.evaluate(
        () => document.documentElement.scrollWidth <= innerWidth,
      ),
      name + ": horizontal overflow",
    );
  };
  const start = async () => {
    await page.goto("http://localhost:5175/");
    await page.locator(".start-game:not([disabled])").waitFor();
    await page.locator(".start-game").click();
    await page.waitForURL("**/game/*");
    const id = page.url().split("/").pop().toUpperCase();
    sessions.push(id);
    await page.locator(".answer-option").first().waitFor();
    return id;
  };
  const state = (id) =>
    db
      .prepare(
        "SELECT s.*, q.CorrectOption FROM Sessions s LEFT JOIN Questions q ON q.Id=s.CurrentQuestionId WHERE s.Id=?",
      )
      .get(id);
  const answer = async (id, correct, capture) => {
    const before = state(id);
    const key = correct
      ? before.CorrectOption
      : before.CorrectOption === "A"
        ? "B"
        : "A";
    const choice = page.locator(".answer-option").filter({
      has: page.locator(".answer-letter", {
        hasText: new RegExp("^" + key + "$"),
      }),
    });
    if (capture === "educational-correct") {
      await choice.focus();
      await choice.press(key.toLowerCase());
    } else await choice.click();
    await page.locator(".question-feedback").waitFor();
    if (capture) await snap(capture);
    assert.notEqual(state(id).TurnId, before.TurnId, "answer was persisted");
    await page.locator(".question-feedback button").click();
    await page.locator(".question-feedback").waitFor({ state: "hidden" });
  };
  try {
    await page.goto("http://localhost:5175/");
    await page.locator(".start-game").waitFor();
    await snap("home-desktop");
    await page.setViewportSize({ width: 390, height: 844 });
    await snap("home-mobile");
    await page.setViewportSize({ width: 1440, height: 900 });
    const happy = await start();
    await snap("game-desktop");
    await page.setViewportSize({ width: 1366, height: 768 });
    await snap("game-laptop");
    await page.setViewportSize({ width: 1024, height: 768 });
    await snap("game-compact");
    await page.setViewportSize({ width: 390, height: 844 });
    await snap("game-mobile");
    await page.setViewportSize({ width: 1440, height: 900 });
    const first = state(happy);
    await page.reload();
    await page.locator(".answer-option").first().waitFor();
    assert.equal(state(happy).TurnId, first.TurnId, "reload preserves turn");
    await page.locator(".sound-button").click();
    await page.waitForFunction(
      () =>
        document.querySelector(".sound-button").getAttribute("aria-pressed") ===
        "false",
    );
    assert.equal(
      await page.locator(".sound-button").getAttribute("aria-pressed"),
      "false",
    );
    await page.locator(".sound-button").click();
    assert.equal(await page.locator(".developer-answer-mark").count(), 1);
    await page.locator(".dev-answer-toggle input").uncheck();
    await page.locator(".developer-answer-mark").waitFor({ state: "hidden" });
    await page.locator(".dev-answer-toggle input").check();
    for (let i = 0; i < 15; i++) {
      if (i === 2) await snap("challenge-question");
      await answer(
        happy,
        true,
        i === 0
          ? "educational-correct"
          : i === 2
            ? "challenge-success"
            : undefined,
      );
    }
    await page.locator(".completion-panel").waitFor();
    assert.equal(state(happy).Status, 1);
    assert.equal(
      db
        .prepare(
          "SELECT count(*) n FROM TopicProgress WHERE GameSessionId=? AND HasScratch=0 AND CompletedCycles=RequiredCycles",
        )
        .get(happy).n,
      5,
    );
    await snap("completed-clean");
    await page.locator(".play-disc").click();
    const scratched = await start();
    for (let i = 0; i < 7; i++)
      await answer(scratched, false, i === 0 ? "educational-wrong" : undefined);
    assert.equal(await page.locator(".scratch-mark").count(), 1);
    await snap("game-scratch");
    await page.evaluate(() => {
      window.scratchEvents = [];
      document
        .querySelector(".game-studio")
        .addEventListener("gramophone-scratch", (e) =>
          window.scratchEvents.push(e.detail),
        );
    });
    await page.locator(".track").first().click();
    await page.waitForFunction(() => window.scratchEvents.length > 0);
    const sync = await page.evaluate(() => window.scratchEvents[0]);
    console.log("Scratch timing:", sync);
    assert(
      sync.disturbanceMs >= 1000,
      "visual disturbance follows the longer noise interval",
    );
    assert(
      sync.visualTime >= sync.scheduledTime &&
        sync.visualTime - sync.scheduledTime < 0.1,
      "scratch visual/audio schedule drift under 100ms",
    );
    console.log(
      "Scratch schedule drift (ms):",
      Math.round((sync.visualTime - sync.scheduledTime) * 1000),
    );
    await snap("scratch-playback");
    await page.emulateMedia({ reducedMotion: "reduce" });
    await page.locator(".track").first().click();
    await page.waitForFunction(() => window.scratchEvents.length > 1);
    assert.equal(
      await page.locator("[data-record]").getAttribute("transform"),
      "rotate(0)",
    );
    assert.equal(
      await page.evaluate(() => window.scratchEvents[1].reduced),
      true,
    );
    const needleError = await page.evaluate(() => {
      const gram = document.querySelector("[data-gramophone]");
      const plane = gram
        .querySelector("[data-turntable]")
        .transform.baseVal.consolidate().matrix;
      const needle = gram
        .querySelector("[data-needle]")
        .transform.baseVal.consolidate().matrix;
      const lift = Number(
        gram.querySelector("[data-tone-arm]").dataset.needleLift,
      );
      const radius = 175 - Number(gram.dataset.order) * 25;
      const contact = new DOMPoint(
        radius * Math.cos(0.55),
        radius * Math.sin(0.55),
      ).matrixTransform(plane);
      return Math.hypot(needle.e - contact.x, needle.f + lift - contact.y);
    });
    assert(
      needleError < 1,
      "needle touches the selected groove in the new artwork projection",
    );
    await page.emulateMedia({ reducedMotion: "no-preference" });
    for (let i = 0; i < 12; i++) await answer(scratched, true);
    await page.locator(".completion-panel").waitFor();
    await snap("completed-scratch");
    await page.locator(".repair-track").click();
    await page.locator(".repair-question").waitFor();
    await snap("repair-question");
    await answer(scratched, true);
    await answer(scratched, true, "repair-success");
    await page.locator(".completion-panel").waitFor();
    assert.equal(await page.locator(".scratch-mark").count(), 0);
    assert.equal(
      db
        .prepare(
          "SELECT count(*) n FROM TopicProgress WHERE GameSessionId=? AND IsRepaired=1",
        )
        .get(scratched).n,
      1,
    );
    await snap("completed-repaired");
    assert.deepEqual(errors, [], "browser errors");
    console.log(
      "PASS: desktop/laptop/mobile, reload, mute, all 5 topics, persisted completion, scratch and repair.",
    );
  } finally {
    await browser.close();
    for (const id of sessions)
      db.prepare("DELETE FROM Sessions WHERE Id=?").run(id);
    db.close();
  }
})().catch((e) => {
  console.error(e);
  process.exitCode = 1;
});
