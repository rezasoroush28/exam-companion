# Discovery Output

## 1. Task

Discover the current folders, frontend/motion implementations, constraints, and change surface for creating a new folder beside `GramophoneEntrancePrototype` that contains a new, fully functional version of the main Exam Companion game whose visible motion follows the prototype's approach.

### Explicit Constraints

* The prototype must remain available and be copied or used as the basis of a new sibling folder.
* The result is intended to be a new version, not an in-place replacement of the current main game.
* The new version must retain the main game's functionality while visibly using the prototype motion approach.
* Frontend refactoring and additional frontend tooling are allowed if needed.
* This phase is discovery only.

### Explicit Exclusions

* No new sibling folder was created.
* No prototype or production source was copied or refactored.
* No runtime, database, configuration, or dependency was changed.
* No implementation plan was produced.

### Acceptance Criteria

* Locate the prototype and every plausible main-game source.
* Identify which copy contains the latest motion work.
* Compare the prototype runtime approach with the current main-game runtime approach.
* Identify the functional boundaries that a new version must preserve.
* Identify copy hazards, missing choices, tests, and probable change surface.

## 2. Discovery Result

**Status:** `COMPLETE`

**Complexity:** `HIGH`

**Discovery Confidence:** `96`

**Recommended Next Phase:** `PLANNING_REQUIRED`

Brief justification:

The prototype is a small static HTML/SVG application with one centralized `requestAnimationFrame` timeline. The functional game is a multi-project Blazor/.NET application with SQLite data, Blazor lifecycle, audio-clock synchronization, reduced motion, cancellation, developer tooling, and browser regression coverage. There are also two dirty, divergent main-game folders at the same Git commit. Creating a clean sibling version requires explicit source, copy, runtime-ownership, data, and repository decisions before implementation.

## 3. Project Context Used

### Repository Instructions

* `docs/AgentSteps/discovery.md` was read and followed.
* No applicable `AGENTS.md` was found in the active repository.

### Current State

`docs/AgentSteps/currentState.md` exists and was consulted for the verified four-layer application boundary, current articulated entrance, JavaScript/.NET ownership, one-shot lifecycle, audio boundary, and browser-test inventory.

The source was checked because the requested target also involves an external prototype folder and a second repository copy not described by `CurrentState.md`.

## 4. Relevant System Area

### Primary Ownership

* Prototype motion is owned by `C:/Users/r.soroush/Desktop/Exam_companion/GramophoneEntrancePrototype/animation.js` and writes SVG attributes directly from one absolute-time animation-frame loop.
* Current active game motion is owned by `C:/Users/r.soroush/Desktop/new/wwwroot/js/gramophoneEntranceRig.js` and `gameMotion.js`: a pure pose sampler generates dense tracks, and the existing controller executes them with the Web Animations API.
* Main-game SVG structure is owned by `Components/Game/Gramophone.razor`; Blazor lifecycle and gameplay requests are coordinated by `Components/Pages/Game.razor`.

### Related Components

* `gameAudio.js` supplies timestamps for record, needle, waveform, and scratch synchronization.
* `MotionLab.razor` exposes the production SVG/controller in a Development-only isolated page.
* The Domain/Application/Infrastructure projects and SQLite files provide the functional game that does not exist in the prototype.
* Browser tests exercise motion, complete gameplay, audio, responsiveness, and Production gates.

### Boundary Notes

The prototype's rendering loop has no Blazor, questions, audio, persistence, reduced-motion substitution, hidden-tab policy, controller rebinding, or gameplay reactions. A new full game cannot be produced by copying only the three prototype files. Conversely, copying the main game and retaining its current Web Animations executor does not literally retain the prototype's runtime rendering approach.

## 5. Relevant Files

### Primary Files

#### `C:/Users/r.soroush/Desktop/Exam_companion/GramophoneEntrancePrototype/animation.js`

**Relevant symbols:** `stages`, `solve`, `leg`, `arm`, `sample`, `render`, `update`, `play`, `reset`.

**Purpose:** Central 4.90-second mechanics-only timeline. A single `requestAnimationFrame` callback samples a complete pose and synchronously writes every SVG transform.

**Why it matters:** This is the exact motion execution approach named by the user.

#### `C:/Users/r.soroush/Desktop/Exam_companion/GramophoneEntrancePrototype/index.html`

**Relevant symbols:** `locomotion-root`, `body-root`, articulated limbs, pocket groups, `disc-prop`.

**Purpose:** Static SVG rig and isolated controls.

**Why it matters:** Defines the DOM assumptions and coordinate system consumed by `animation.js`.

#### `C:/Users/r.soroush/Desktop/Exam_companion/GramophoneEntrancePrototype/style.css`

**Relevant symbols:** stage layout, scene sizing, controls, mobile breakpoint.

**Purpose:** Minimal prototype shell.

**Why it matters:** Confirms the prototype is not an application UI or reusable frontend framework.

#### `C:/Users/r.soroush/Desktop/new/wwwroot/js/gramophoneEntranceRig.js`

**Relevant symbols:** `entranceStages`, `solveTwoLink`, `solveLeg`, `sampleEntrancePose`, `buildEntranceTracks`.

**Purpose:** Current pure, corrected version of the prototype mechanics adapted to the main SVG geometry.

**Why it matters:** It already captures prototype walking, sitting, pocket, wrist/disc, and placement poses without DOM or game ownership.

#### `C:/Users/r.soroush/Desktop/new/wwwroot/js/gameMotion.js`

**Relevant symbols:** `rigRecipe`, `perform`, `entranceSequence`, `sequence`, `setEntrance`, playback loop, controller lifecycle.

**Purpose:** Current production executor. It turns sampled rig poses into dense Web Animation tracks and owns channels, lifecycle, reduced motion, focus, semantic reactions, and audio visualization.

**Why it matters:** This differs from the prototype's direct per-frame renderer and is the main frontend boundary implicated by the request.

#### `C:/Users/r.soroush/Desktop/new/Components/Game/Gramophone.razor`

**Relevant symbols:** `gram-locomotion`, `machine-root`, articulated limb groups, `pocket-root`, `record-prop`, `record-root`, horn and tonearm groups.

**Purpose:** Full-game native SVG including the playable turntable, five topic grooves, needle, scratch layers, and accessible state.

**Why it matters:** The prototype artwork alone lacks these functional game elements.

#### `C:/Users/r.soroush/Desktop/new/Components/Pages/Game.razor`

**Relevant symbols:** motion initialization/disposal, entrance start, answer feedback, playback, repair, completion.

**Purpose:** Connects authoritative Blazor game state to presentation and JavaScript.

**Why it matters:** A motion-runtime refactor must preserve this public behavioral boundary.

#### `C:/Users/r.soroush/Desktop/new/wwwroot/js/gameAudio.js`

**Relevant symbols:** audio scheduling, cancellation, playback timeline.

**Purpose:** Provides the timing source for musical playback and damaged-topic noise.

**Why it matters:** Prototype motion has no equivalent; a centralized motion loop must coexist with this clock.

### Supporting Files

* `Components/Pages/MotionLab.razor` and `.css` — isolated inspection of the real game SVG/controller.
* `Components/Game/Gramophone.razor.css` and `wwwroot/css/game-shell.css` — SVG transform/layout and responsive presentation.
* `tests/browser/entrance-rig.cjs`, `motion-lab.cjs`, `motion.cjs`, `visible-motion.cjs`, `smoke.cjs`, `audio.cjs`, `production.cjs` — observable contracts.
* `ExamCompanion.sln`, `ExamCompanion.Web.csproj`, `src/**` — functional four-layer application absent from the prototype.
* `.gitignore` — identifies generated output, browser profiles, local data, tools, logs, and secrets that should not be blindly duplicated.

## 6. Current Behavior

### Prototype

1. Static HTML loads the SVG rig and `animation.js`; no package manager or third-party frontend runtime is used.
2. `play()` starts one `requestAnimationFrame` loop based on `performance.now()`.
3. `at()` maps absolute elapsed time into one of nine stages totaling 4900 ms.
4. `sample()` computes the entire character pose; analytical two-link functions compute legs and right arm.
5. `render()` writes all SVG transforms in one synchronous pass and derives the held disc from the computed wrist.
6. Cancellation increments a run ID and cancels the pending frame. Page hide cancels the loop.
7. The sequence stops while the seated character holds the disc. There is no playable turntable handoff or game behavior.

### Active main game at `Desktop/new`

1. Blazor renders the full game and inline SVG, then initializes one JavaScript controller for the live host.
2. `gramophoneEntranceRig.js` samples prototype-derived poses and builds dense stage keyframes at 60 samples per second.
3. `gameMotion.js` starts separate `Element.animate()` players for each affected SVG node while tracking them under channels and a sequence generation.
4. The production entrance runs 13 stages over about 6.72 seconds, adding record placement, wobble, wake, and listen after the prototype-derived hold.
5. Question focus/readiness occurs only after the playable record is mounted and the sequence completes.
6. Ordinary answer, milestone, scratch, repair, playback, and lesson-finish motions use the same controller. Playback visualization uses an audio-clock-driven animation-frame loop.
7. Pause/resume/rate changes, reduced motion, cancellation, root replacement, hidden tabs, navigation, and disposal are covered by the controller and tests.

### Adjacent main game at `Desktop/Exam_companion/new`

1. It is a separate Git working tree at the same branch and commit as `Desktop/new` (`make-it-live`, commit `ec6b892...`).
2. It has a different dirty working tree and older relevant files.
3. It does not contain `wwwroot/js/gramophoneEntranceRig.js` or the completed focused rig test.
4. Its production motion file still uses the earlier authored Web Animation entrance and MotionLab's slow legacy entrance option.

## 7. Current Data Model

Motion state is transient and has no database model. The full game depends on application data that the prototype does not contain.

Relevant copy-level data:

* `data/exam-companion.db` — active EF Core game/catalog database and player/session state; ignored by Git.
* Additional SQLite question/reference databases under `data/` — source/import/reference inputs used by the project history.
* `sessionStorage` — one-shot entrance marker per game session.
* `localStorage` — presentation preferences such as reduced motion/mute where applicable.

Whether the new sibling version should receive current player/session data is not specified.

## 8. Current Business Rules

### .NET owns learning behavior; JavaScript owns presentation only

**Status:** `DOCUMENTED_AND_IMPLEMENTED`

**Evidence:** `CurrentState.md`, `ARCHITECTURE.md`, `Game.razor`, `gameMotion.js`.

### Fresh-session entrance runs once and ends in a playable state

**Status:** `DOCUMENTED_AND_IMPLEMENTED`

**Evidence:** `gameMotion.js` `entranceSequence`, session marker handling, motion browser tests.

### Audio and playback visuals share timing

**Status:** `DOCUMENTED_AND_IMPLEMENTED`

**Evidence:** `gameAudio.js`, `gameMotion.js` playback loop, audio/motion tests.

### Prototype is mechanics-only

**Status:** `IMPLEMENTED`

**Evidence:** Prototype contains only static HTML/CSS/JS/assets and explicitly has no sound or game logic.

## 9. Existing Patterns

### Pattern: Central absolute-time pose renderer

**Location:** `GramophoneEntrancePrototype/animation.js`

**Purpose:** One frame loop samples and applies the entire rig pose.

**Similarity to current task:** This is the requested prototype execution style.

**Important differences:** It has only simple run-ID cancellation and does not cover Blazor lifecycle, multiple motion channels, audio synchronization, reduced motion, or semantic reactions.

**Assessment:** `LIKELY_REUSABLE` as a rendering concept, not as a complete game controller.

### Pattern: Pure pose sampler plus lifecycle-aware executor

**Location:** `Desktop/new/wwwroot/js/gramophoneEntranceRig.js` and `gameMotion.js`

**Purpose:** Separates pose mathematics from DOM/lifecycle ownership and executes dense tracks through Web Animations.

**Similarity to current task:** Already transfers prototype mechanics into the full game safely.

**Important differences:** Runtime application is distributed among multiple WAAPI players rather than one synchronous per-frame render pass.

**Assessment:** `LIKELY_REUSABLE`; the planning question is whether its executor changes while the sampler/controller contracts remain.

### Pattern: Development-only live Motion Lab

**Location:** `Components/Pages/MotionLab.razor`

**Purpose:** Runs the real production SVG and controller without player-data changes.

**Similarity to current task:** Existing validation surface for any new runtime approach.

**Important differences:** It is not a separate distributable app or sibling project.

**Assessment:** `LIKELY_REUSABLE`.

## 10. Dependencies

### Internal Dependencies

* `Game.razor` -> `gameMotion.js` -> live `Gramophone.razor` SVG parts.
* `gameMotion.js` -> `gramophoneEntranceRig.js` for entrance poses.
* `gameMotion.js` -> `gameAudio.js` schedule for playback visualization.
* Web project -> Application -> Domain; Infrastructure implements Application persistence using EF Core/SQLite.
* Browser tests -> a running Development or published Production host.

### External Dependencies

* .NET 10 / Blazor Web App.
* EF Core SQLite.
* Browser SVG, Web Animations, `requestAnimationFrame`, Web Audio, storage, and media-query APIs.
* Playwright/Microsoft Edge for browser verification.
* The prototype itself uses no frontend package dependency.

## 11. Integration Behavior

### Integration

**Boundary:** Blazor component lifecycle to JavaScript controller.

**Protocol/mechanism:** JS interop returns/uses a controller bound to one host SVG; semantic calls request entrance, reactions, playback, pause/resume, and disposal.

**Current flow:** .NET supplies authoritative state -> controller samples/animates presentation -> audio returns timestamps -> controller renders playback/scratch state -> .NET remains authoritative.

**Failure behavior:** Current controller rejects disconnected/missing roots, invalidates stale generations, cancels tracked players/frames, and restores coherent record state.

**Retry behavior:** Motion can be replayed in Motion Lab; gameplay entrance is intentionally one-shot.

**Idempotency behavior:** Controller reuse uses a host-bound registry; session storage prevents entrance replay.

**Known uncertainties:** Whether the new version must literally replace entrance and semantic WAAPI players with one rAF renderer, or only make the visible mechanics match the prototype.

## 12. Transaction and Consistency Behavior

The motion layer has no database transaction. Gameplay answers are persisted by the Application/Infrastructure flow before presentation feedback. A copied version with an independent database will diverge from current player state; a copied version sharing the same SQLite file may create cross-version locking/consistency risk if both versions run simultaneously.

The important frontend consistency boundary is cancellation: no stale animation callback may focus a question, reveal/hide the wrong record, or overwrite a newer gameplay reaction.

## 13. Security and Access Control

The application is a local single-user MVP without authentication. `.gitignore` excludes `.env*`, `secrets.json`, and `appsettings.Local.json`; these must not be implicitly copied or synthesized without an explicit configuration policy. Motion Lab remains Development-only and should not become accessible in published Production by accident.

## 14. Testing State

### Relevant Tests

* `tests/browser/entrance-rig.cjs`
* `tests/browser/motion-lab.cjs`
* `tests/browser/motion.cjs`
* `tests/browser/visible-motion.cjs`
* `tests/browser/smoke.cjs`
* `tests/browser/audio.cjs`
* `tests/browser/production.cjs`
* `tests/ExamCompanion.Application.Tests`

### Behaviors Currently Covered

* Rendered IK/joint positions, planted feet, swing lift, sitting, pocket reveal, and wrist/disc constraint.
* Exact production entrance order/timing and one-shot readiness.
* Pause/rate/cancel/reset/replacement/hidden/reduced/dispose behavior.
* All gameplay reactions, playback, scratch synchronization, repair, final bow, responsive layout, persistence, and Production gates.

### Important Missing Coverage

* No test currently asserts a full-game controller implemented exclusively through the prototype's one-loop rAF approach.
* No test covers two sibling full-game versions running against the same copied/shared SQLite data.
* No clean-copy/packaging test proves that generated folders and local browser profiles are excluded from a new sibling.

### Existing Testing Pattern

Use observable live SVG geometry and controller diagnostics rather than implementation-string assertions. Run focused Motion Lab/rig checks before the complete browser and Production suites.

## 15. CurrentState.md Verification

### Confirmed

* Four-layer ownership, current native SVG rig, pure pose sampler, WAAPI executor, one-shot entrance, audio boundary, Development Motion Lab, and test inventory match `Desktop/new`.

### Outdated

* No material drift for `Desktop/new`.

### Partially Confirmed

* `CurrentState.md` does not describe the separate stale `Desktop/Exam_companion/new` working copy or the external static prototype.

### Unverified

* Which main-game folder the user considers authoritative for the new sibling version.

## 16. Current vs Intended State

### Area: Folder/product layout

**CURRENT**

`Desktop/Exam_companion` contains the prototype and an older dirty main-game clone. The latest verified motion work is in the separate `Desktop/new` clone. No new sibling version exists.

**INTENDED**

A new folder beside the prototype contains a new full main-game version based on the prototype, leaving existing folders intact.

**GAP**

Target name, authoritative source clone, Git relationship, generated-file policy, and data-copy policy are unresolved.

### Area: Motion execution

**CURRENT**

The prototype synchronously applies one complete pose per rAF tick. The latest main game samples the same style of pose mathematics into dense per-node WAAPI tracks; only audio playback visualization is applied by rAF.

**INTENDED**

The new game should visibly use the approach that exists in the prototype.

**GAP**

It is unclear whether “approach” requires literal single-loop rAF rendering for entrance/game reactions or means matching the prototype's centralized pose/IK mechanics while retaining the proven WAAPI lifecycle executor.

### Area: Product capability

**CURRENT**

The prototype stops at a held record and has no questions, data, audio, accessibility, responsive game shell, or completion flow. The main game has all those capabilities and continues from hold through record placement and question readiness.

**INTENDED**

The sibling must be a version of the main game, not only a larger motion demo.

**GAP**

The prototype shell cannot serve as the application base without importing essentially the complete Blazor solution and adapting its presentation layer.

## 17. Relevant Technical Debt

### Item: Divergent dirty clones at one commit

**Location:** `Desktop/new` and `Desktop/Exam_companion/new`.

**Impact on task:** Copying the physically adjacent clone would omit the latest rig implementation; copying the active clone without recording provenance may confuse future maintenance.

**Severity:** `HIGH`

### Item: Very large working folders dominated by generated/local artifacts

**Location:** both main-game folders are roughly 700 MB, while the prototype is about 0.58 MB.

**Impact on task:** Blind recursive copying would duplicate `.git`, `bin`, `obj`, `.tools`, `.vs`, browser profiles, artifacts, package output, logs, and possibly player data instead of producing a clean new version.

**Severity:** `HIGH`

### Item: Prototype solver/runtime is not production-lifecycle complete

**Location:** `GramophoneEntrancePrototype/animation.js`.

**Impact on task:** Its direct renderer lacks the main controller's cancellation generations, root rebinding, reduced-motion path, semantic channels, and audio integration.

**Severity:** `HIGH`

## 18. Risks

### High Risk

* Choosing `Desktop/Exam_companion/new` as source loses verified, uncommitted motion changes present only in `Desktop/new`.
* Blindly copying an entire dirty repository duplicates hundreds of megabytes of generated state and may copy an independent `.git` history or local player data.
* Replacing WAAPI with direct rAF without equivalent generation/cancellation semantics can allow stale frames to overwrite gameplay reactions or question readiness.
* Sharing one writable SQLite database between old and new running versions can cause locking or behavioral divergence.

### Medium Risk

* The prototype's 4.90-second held-disc endpoint conflicts with the game-required turntable placement and 6.72-second readiness contract.
* A literal prototype DOM hierarchy lacks the five grooves, tonearm, scratch layers, accessibility data, and state classes used by the full game.
* Introducing a frontend framework/library is not evidenced as necessary and could duplicate existing Blazor/JS ownership.

### Low Risk

* Creating a clean sibling after source/copy policy is decided is mechanically straightforward.

## 19. Unknowns

### Unknown 1: Target folder name

**Why it matters:** Required before creating a sibling and setting project/repository identity.

**Evidence checked:** Current `Desktop/Exam_companion` children; no intended new name exists.

### Unknown 2: Authoritative main-game source

**Why it matters:** `Desktop/new` has the latest verified changes; `Desktop/Exam_companion/new` is physically adjacent but older.

**Evidence checked:** Git branch/commit, file sizes/hashes, status, presence of the rig module and focused tests.

### Unknown 3: Meaning of “prototype approach”

**Why it matters:** Literal direct-rAF execution and prototype-like pose mechanics are different architectural scopes.

**Evidence checked:** Prototype `play/update/render` versus current `buildEntranceTracks/perform/Element.animate`.

### Unknown 4: Data and Git isolation policy

**Why it matters:** The new version may need fresh, copied, or shared SQLite state and may be a new repository, worktree, branch, or unversioned experiment.

**Evidence checked:** `.git` folders, ignored data files, directory sizes, and current single-user SQLite architecture.

## 20. Planning Decisions Required

* Select the authoritative source: latest `Desktop/new`, older adjacent `Exam_companion/new`, or an explicit reconciliation of both.
* Choose a target sibling folder name and Git relationship.
* Define a clean-copy inclusion/exclusion manifest, especially for `.git`, `bin`, `obj`, tools, browser profiles, artifacts, logs, local config, and databases.
* Decide whether the target receives fresh, copied, or shared application data.
* Define whether the prototype runtime must be reproduced literally with one rAF loop or whether its pose/IK approach is sufficient.
* Define which motion families use the new approach: entrance only, all semantic character reactions, or playback/scratch as well.
* Preserve or intentionally revise the held-disc-to-turntable handoff and question-readiness endpoint.
* Define acceptance parity between the new sibling and the current full-game browser/test suite.

## 21. Probable Change Surface

This is not an implementation plan.

### Likely Affected

* New sibling folder under `C:/Users/r.soroush/Desktop/Exam_companion/`.
* New-version `wwwroot/js/gameMotion.js` and `gramophoneEntranceRig.js` or their replacement boundary.
* New-version `Components/Game/Gramophone.razor` and CSS if runtime/DOM transform ownership changes.
* New-version Motion Lab and motion browser tests.
* New-version documentation and launch configuration.

### Possibly Affected

* `Components/Pages/Game.razor` if controller API/readiness changes.
* `gameAudio.js` if one shared rAF clock is extended into playback; no current evidence requires audio formula changes.
* Project/repository metadata and application-data connection configuration.

### Expected To Remain Unaffected

* Domain learning rules, question selection, topic progress, correctness, repair, and EF entities.
* Database schema/migrations, unless the future plan explicitly changes data isolation; motion itself requires no persistence.
* Existing prototype and existing main-game folders.

## 22. Recommended Planner Context

### Must Read

* `C:/Users/r.soroush/Desktop/Exam_companion/GramophoneEntrancePrototype/animation.js` — exact requested runtime mechanics.
* `C:/Users/r.soroush/Desktop/Exam_companion/GramophoneEntrancePrototype/index.html` — prototype rig/DOM contract.
* `C:/Users/r.soroush/Desktop/new/wwwroot/js/gramophoneEntranceRig.js` — latest corrected pose model.
* `C:/Users/r.soroush/Desktop/new/wwwroot/js/gameMotion.js` — current lifecycle-safe executor and integration boundary.
* `C:/Users/r.soroush/Desktop/new/Components/Game/Gramophone.razor` — full-game SVG capability that must survive.
* `C:/Users/r.soroush/Desktop/new/Components/Pages/Game.razor` — Blazor/controller behavior boundary.
* `C:/Users/r.soroush/Desktop/new/.gitignore` — clean-copy exclusions.
* `docs/AgentSteps/currentState.md` — verified functional architecture.
* Motion/rig/smoke/audio/Production browser tests — existing acceptance baseline.

### Read If Needed

* `gameAudio.js` — if the planned renderer owns playback frames.
* `ExamCompanion.Web.csproj`, solution projects, and `Program.cs` — when defining the copied full-app structure.
* `data/` inventory and appsettings — only after data isolation is decided.

### Usually Unnecessary For This Task

* Detailed Domain and EF entity implementation, provided the frontend/controller boundary remains unchanged.
* Historical cube, health, coin, or retired challenge UI code.

## 23. Discovery Summary

* The prototype is `Desktop/Exam_companion/GramophoneEntrancePrototype`, a 0.58 MB static SVG/JS study driven by one absolute-time rAF loop.
* A physically adjacent main game exists at `Desktop/Exam_companion/new`, but it is an older dirty clone.
* The latest verified main game is `Desktop/new`; both clones share branch/commit but have divergent uncommitted files.
* The latest game already uses corrected prototype-derived pose/IK mechanics, but converts them into dense per-node Web Animations rather than applying one synchronous pose per rAF tick.
* The full game adds Blazor lifecycle, SQLite, questions, record placement, audio-clock playback, reduced motion, cancellation, reactions, accessibility, and Production gates absent from the prototype.
* A clean sibling should not be made through blind recursive copying: the main folders contain about 700 MB of generated/local state.
* The main uncertainty is whether the user wants literal rAF execution or prototype-equivalent mechanics inside the existing controller contract.
* Source, target name, Git relationship, copy manifest, data isolation, motion scope, and entrance endpoint require planning decisions.
* No production implementation was performed during discovery.
