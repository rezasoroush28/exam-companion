# هم‌آهنگ — Exam Companion

A Persian, RTL music-disc learning game. Answer questions to build five musical rings, finish your biology album, and revisit scratched tracks to polish them. The former cubes, health, coins, and recovery-suggestion engines are not part of this game.

## Run

Requires the **.NET 10 SDK**. This implementation keeps the existing solution's .NET 10 / EF Core 10 stack, rather than downgrading it to the .NET 8 mentioned in the design brief.

```powershell
dotnet restore ExamCompanion.sln
dotnet build ExamCompanion.sln --no-restore
dotnet test ExamCompanion.sln --no-build
dotnet run --project ExamCompanion.Web.csproj --launch-profile http
```

Open **http://localhost:5175**. In Visual Studio, open `ExamCompanion.sln`, select `ExamCompanion.Web` as startup project, and run the `http` profile. The repository's NuGet.Config uses public NuGet only; no private feed or question-bank API credentials are required.

## Game rules

- One fixed lesson, **زیست‌شناسی 1**, with the same five existing topics, in a fixed sequence. Later tracks cannot be skipped to.
- A cycle normally contains **two educational questions, then one challenge**.
- A wrong educational answer adds a reinforcement question, capped at **four educational questions before the first challenge**.
- A failed challenge adds one educational reinforcement, followed by a second challenge attempt. The second failure closes the cycle with a scratch; it never traps the player in a loop.
- A topic also scratches at three wrong answers or three reinforcement answers across its cycles. Mistakes do not remove completed progress.
- A completed topic unlocks the next one. Completing all topics produces the album screen, whether clean or scratched.
- Repair is one educational question plus one challenge. Both must be correct to clear the scratch. Failed repairs can be retried. Repairing an earlier topic preserves the exact pending question/stage in the main sequence.
- There is no timer, health loss, coin economy, or topic-selection detour. Feedback and explanations remain visible until the player chooses Continue.
- Correctness, question selection, progression, and repairs are server-authoritative. A unique turn ID rejects duplicate/stale answers; EF optimistic concurrency protects concurrent saves. Refresh resumes the saved next question.

Cycle count uses raw importance: `< 0.34 → 1`, `< 0.67 → 2`, otherwise `3`. Configuration for the other thresholds is under `MusicGameRules` in `appsettings.json`.

**Data caveat:** all five existing raw importance values fall below 0.34. They therefore each receive **one cycle** under the supplied new rules. The values have deliberately not been rescaled or fabricated to force variety. Tests cover one-, two-, and three-cycle topics.

## Data and migrations

Development startup applies the EF Code First migrations and, on an empty catalog, imports **100 educational questions and 25 challenge questions** from the existing local SQLite files. It preserves original IDs as `SourceId`, names, answers, educational explanations, and raw topic importance. Application-owned keys are GUIDs.

| Source topic ID | Topic | Raw importance |
|---|---|---:|
| 4253 | دنیای زنده | 0.089286 |
| 4319 | تبادلات گازی | 0.232143 |
| 4346 | گردش مواد در بدن | 0.339286 |
| 4399 | تنظیم اسمزی و دفع مواد زائد | 0.3035715 |
| 4457 | جذب و انتقال مواد در گیاهان | 0 |

The new database is **`data/exam-companion.db`** (gitignored). Its tables are `Exams`, `Lessons`, `Topics`, `Questions`, `Sessions`, `TopicProgress`, and `Attempts`, plus EF migration history. The old question-bank, educational, and progress databases remain untouched. Only the seed importer reads the original question banks; gameplay reads the new database. Re-running startup does not duplicate the catalog or clear progress.

Migration files live in `src/ExamCompanion.Infrastructure/Music/Migrations`. Always specify **MusicDbContext**; the older historical context is still in the solution.

```powershell
dotnet tool install dotnet-ef --version 10.0.9 --tool-path .tools
./.tools/dotnet-ef migrations add YourChange --context MusicDbContext --project src/ExamCompanion.Infrastructure --startup-project ExamCompanion.Web.csproj --output-dir Music/Migrations
./.tools/dotnet-ef database update --context MusicDbContext --project src/ExamCompanion.Infrastructure --startup-project ExamCompanion.Web.csproj
```

Production does not automatically migrate or seed: prepare the database in Development, back it up, and deploy it with the application, applying subsequent migrations explicitly. Start a new album from Home to begin again without erasing previous sessions. This is a local, single-user MVP; there is intentionally no authentication or authorization boundary between sessions.

## Project map

```text
ExamCompanion.sln
Program.cs / ExamCompanion.Web.csproj     composition and Blazor host
Components/
  Pages/Home.razor                       start / resume, fixed lesson
  Pages/Game.razor                       game and completed-album screens
  Game/MusicDisc.razor                   interactive SVG musical artifact
  Game/QuestionPanel.razor               questions and answer feedback
wwwroot/app.css                          responsive game-art styling
wwwroot/js/gameAudio.js                  synthesized Web Audio notes / scratches
src/
  ExamCompanion.Domain/Music/            entities and game vocabulary
  ExamCompanion.Application/Music/       engine, selection, DTOs, persistence port
  ExamCompanion.Infrastructure/Music/    EF configurations, store, seed, migrations
tests/
  ExamCompanion.Application.Tests/       real SQLite-backed state-machine tests
  browser/smoke.cjs                      optional end-to-end development check
```

See [ARCHITECTURE.md](ARCHITECTURE.md) for the dependency and request flow. Historical non-Music services remain in the repository for reference, but are not registered by the new host. The old recovery page and Matter.js script loading were removed.

## Verification

Verified locally: restore and build succeeded, **16 .NET tests passed**, the complete browser smoke test passed, and EF reported no pending model changes. The native SQLite package warning below remains.

The .NET suite covers cycle boundaries, capped reinforcement, challenge retry/closure, unlock order, completion, successful/failed repairs, suspended-question restoration, reload, invalid/duplicate answers, and concurrent saves, using real SQLite migrations rather than an in-memory EF provider.

Optional browser test requires Node 24 and Microsoft Edge. Start the app first, then:

```powershell
npm --prefix tests/browser install
npm --prefix tests/browser test
```

It checks desktop (1440px), laptop (1366px), and mobile (390px) layouts; resume; sound toggle; a complete clean album; scratches; and repair. It verifies persisted data and removes **only its own test sessions**, leaving catalog and player data intact. Screenshots are saved to ignored `artifacts/`.

## Visual and audio choices / limitations

The disc is real SVG, not a screenshot: five colored tracks, a smiling star label, animated progress, per-ring interaction, scratches, and repair sparkles. Cream, lime, blue, purple, coral, and gold sit against a dark studio background. Audio phrases grow with ring progress; scratches add soft filtered noise. Sound is optional, and reduced-motion preferences are respected.

- Audio is lightweight synthesis, not prerecorded music or a full music arrangement. Browser sound requires a user gesture.
- The font uses Google Fonts with local Tahoma/sans-serif fallback; questions and game logic do not require a network service.
- Existing question HTML is converted to safe plain text for this MVP; rich mathematical/illustrated question rendering is not implemented.
- Restore currently reports an inherited `SQLitePCLRaw.lib.e_sqlite3 2.1.11` vulnerability warning ([advisory](https://github.com/advisories/GHSA-2m69-gcr7-jv3q)). It is not suppressed. Updating the native SQLite dependency is a prerequisite before public production deployment.
