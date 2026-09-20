# هم‌آهنگ — Exam Companion

A Persian, RTL papercraft gramophone learning game. Answer questions to build five musical grooves, finish your biology album, and revisit scratched tracks to polish them. The former cubes, health, coins, and recovery-suggestion engines are not part of this game.

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
  Game/Gramophone.razor                  coherent interactive inline-SVG artifact
  Game/Gramophone.razor.css              isolated paper materials and motion
  Game/TopicSidebar.razor                accessible topic sequence and states
  Game/PlaybackWaveform.razor             playback / scratch indicator
  Game/GameIcon.razor                    small authored SVG icon set
  Game/QuestionPanel.razor               questions and answer feedback
wwwroot/app.css                          global resets and shared controls
wwwroot/css/design-tokens.css            palette, typography, spacing, materials
wwwroot/css/game-shell.css               responsive home / game / album layouts
wwwroot/art/brand/hamahang-mark.svg       original folded-note identity
wwwroot/js/gameAudio.js                  synthesized Web Audio notes / scratches
wwwroot/js/gameMotion.js                 shared-clock record / needle / playhead
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

Verified locally: restore and build succeeded, **17 .NET tests passed**, the complete browser smoke test passed, and EF reported no pending model changes. The native SQLite package warning below remains.

The .NET suite covers cycle boundaries, capped reinforcement, challenge retry/closure, unlock order, completion, successful/failed repairs, suspended-question restoration, reload, invalid/duplicate answers, and concurrent saves, using real SQLite migrations rather than an in-memory EF provider.

Optional browser test requires Node 24 and Microsoft Edge. Start the app first, then:

```powershell
npm --prefix tests/browser install
npm --prefix tests/browser test
```

It checks desktop (1440px), laptop (1366px), compact (1024px), and mobile (390px) layouts; resume; keyboard answers; sound and developer-answer toggles; a complete clean album; scratch schedule synchronization; reduced motion; and repair. It verifies persisted data and removes **only its own test sessions**, leaving catalog and player data intact. Screenshots are saved to ignored `artifacts/`.

`node tests/browser/production.cjs` checks the hidden developer-answer controls, final gramophone composition, responsive fit, and absence of the Development-only Motion Lab against separately published Production output. The host forcibly disables `ShowDeveloperAnswers` outside Development, so the engine omits the answer key from question DTOs even if the configuration switch is true. Feedback still reveals correctness after a submitted answer. Set `EXAM_PRODUCTION_URL` when the published host is not using the default test port.

The Release publish and published-output Production browser check pass. Production still does not migrate or seed automatically; point the host at a prepared database as described above.

## Visual and audio choices / limitations

The gramophone is entirely inline SVG: an articulated papercraft character with nested arms, legs, feet, pocket, removable record, rounded burgundy plinth, recessed turntable, and flower-shaped cream/ochre horn. A one-shot 13-stage entrance walks the character into view, plants alternating feet, sits, retrieves the record from its pocket, places it on the turntable, and settles into the listening pose before the first question becomes active. Gameplay reactions still provide answer feedback, weighted milestone hops, scratch contact, repair relief, and a final bow after album cadence. Five interactive grooves retain accessible topic labels, locking, and keyboard selection. The historical artwork remains reference-only. Home reuses the object in static preview mode. See the [gramophone motion/artwork contract](docs/gramophone-art.md) for exact geometry, timing, lifecycle, and verification.

The design tokens define navy canvas, warm ivory question cards, lime actions, restrained paper edges/shadows, and five topic accents. Desktop has three regions (topic sequence / gramophone / question), approximately 17/37/46 percent. Mobile stacks the current topic and object above the question, with compact topic navigation below. Answer targets remain at least 56px tall. Keyboard focus, non-color status labels, mute, and reduced motion are supported; A–D or 1–4 selects an answer while focus is in the question card.

### Musical mapping and synchronization

| Topic order | Harmonic identity | Educational notes |
|---|---|---|
| 1 | Cmaj7 | C, E, G, B |
| 2 | Dm7 | D, F, A, C |
| 3 | Em7 | E, G, B, D |
| 4 | Fmaj7 | F, A, C, E |
| 5 | G7 | G, B, D, F |

Correct educational answers reveal successive chord notes; `NotesRevealed` is derived from saved correct, non-repair educational attempts. Submitting a correct final challenge answer plays the cycle chord as four equally short 650ms notes, with no extra sustained chord at the end (about 1.3 seconds total). Topics play a 2.7-second motif with a short 250ms final chord fade; the final album combines the five motifs. Wrong answers use quiet unresolved feedback without removing progress. No new persistent fields or migrations were needed for this presentation.

`gameAudio.js` schedules audio and returns a timeline. `gameMotion.js` samples the same clock for playback/scratch contact, while tracked Web Animations handle semantic body language without per-frame server calls. Unfinished or scratched phrases break up under noise from 1.25s to 2.35s, then recover. The full chord sustains through this 1.1-second interruption; the synchronized physical needle impulse lasts only 260ms. Clean completed/repaired phrases and short correct-answer notes stay clean. Challenge resolutions keep four equally short notes without a sustained final tail. Playback/motion stop on replacement, pause, hidden tabs or disposal. The persisted in-app reduced-motion checkbox combines with the OS preference to remove rotation/falling/jumps while retaining state feedback.

`node tests/browser/motion.cjs` exercises the exact 13-stage entrance order/timing, no replay/no layout shift, gameplay motion vocabulary, alternating cycle poses, keyboard groove preview, single synchronized scratch callbacks, real answer milestones, final cadence bow, both reduced-motion controls, and navigation cancellation. `node tests/browser/entrance-rig.cjs` independently measures rendered joints, planted feet, swing lift, sitting depth, pocket/record attachment, desktop/mobile fit, pause/rate changes, replacement, root rebinding, hidden tabs, reduced motion, and disposal. Like the smoke test, gameplay journeys remove only their own test session.

In Development, `/motion-lab` isolates the live production SVG from gameplay. It provides every articulated entrance stage and full-sequence preview, semantic gameplay reactions, playback-rate control, pause, resume, cancel, reset, reduced motion, and sampled/rendered joint diagnostics. `node tests/browser/motion-lab.cjs` verifies controls, actual hand-to-record attachment, visible travel, controller reuse, SVG rebinding, and reduced-motion behavior without creating player data.

`node tests/browser/audio.cjs` verifies actual offline Web Audio renders: the chord starts clean, is suppressed under noise in the middle for over one second, and returns cleanly afterward. It also checks that correct-answer notes and clean completed topics receive no disturbance; it does not modify player data.

- Audio is lightweight synthesis, not prerecorded music or a full music arrangement. Browser sound requires a user gesture.
- Browser tests measure visual events against the scheduled audio clock, not physical speaker latency. Device output latency and subjective musical quality still need listening checks on target hardware.
- The font uses Google Fonts with local Tahoma/sans-serif fallback; questions and game logic do not require a network service.
- Existing question HTML is converted to safe plain text for this MVP; rich mathematical/illustrated question rendering is not implemented.
- Restore currently reports an inherited `SQLitePCLRaw.lib.e_sqlite3 2.1.11` vulnerability warning ([advisory](https://github.com/advisories/GHSA-2m69-gcr7-jv3q)). It is not suppressed. Updating the native SQLite dependency is a prerequisite before public production deployment.
