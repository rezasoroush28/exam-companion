// Run against a separately started Production host on port 5176.
// Removes only the session created by this check.
const path = require("node:path");
const assert = require("node:assert/strict");
const { DatabaseSync } = require("node:sqlite");
let chromium;
try {
  ({ chromium } = require("playwright"));
} catch {
  ({ chromium } = require("../../.tools/browser/node_modules/playwright"));
}
(async () => {
  const db = new DatabaseSync(
    path.resolve(__dirname, "../../data/exam-companion.db"),
  );
  db.exec("PRAGMA foreign_keys=ON; PRAGMA busy_timeout=5000;");
  const browser = await chromium.launch({ channel: "msedge", headless: true });
  let id;
  try {
    const page = await browser.newPage();
    await page.goto("http://localhost:5176/");
    await page.locator(".start-game:not([disabled])").click();
    await page.waitForURL("**/game/*");
    id = page.url().split("/").pop().toUpperCase();
    await page.locator(".answer-option").first().waitFor();
    assert.equal(await page.locator(".dev-answer-toggle").count(), 0);
    assert.equal(await page.locator(".developer-answer-mark").count(), 0);
    console.log(
      "PASS: Production renders questions without developer answer controls or markers.",
    );
  } finally {
    await browser.close();
    if (id) db.prepare("DELETE FROM Sessions WHERE Id=?").run(id);
    db.close();
  }
})().catch((e) => {
  console.error(e);
  process.exitCode = 1;
});
