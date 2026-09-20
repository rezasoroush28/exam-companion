# Plan Output

## 1. Task

Create a clean new Exam Companion version in a folder beside `GramophoneEntrancePrototype`, preserve the complete Blazor game, and make the prototype-covered gramophone entrance visibly run through the prototype's centralized absolute-time `requestAnimationFrame` approach rather than the current dense per-node Web Animation execution.

---

## 2. Planning Status

**Status:** `READY`

**Complexity:** `HIGH`

**Plan Confidence:** `94`

**Execution Recommendation:** `STRONGER_EXECUTOR`

Brief justification:

The latest verified source is a dirty working tree separate from the prototype's parent directory. The target must be copied selectively without losing uncommitted source or duplicating generated/local data. The motion change crosses SVG geometry, an absolute-time pose sampler, controller cancellation, Blazor readiness, reduced motion, and browser verification. The architecture and scope are resolved below; no blocking decision remains.

---

## 3. Inputs Used

### Discovery

* `docs/AgentSteps/temp/Discovery_Output.md`

### Project Context

* `docs/AgentSteps/currentState.md`
* `docs/AgentSteps/Plan.md`
* `docs/AgentSteps/discovery.md`

### Additional Files Inspected

* `wwwroot/js/gameMotion.js` public controller surface and entrance executor symbols.
* `Components/Pages/Game.razor` and `Components/Pages/MotionLab.razor` JS interop calls.
* `Program.cs` Development migration/seed behavior.
* `.gitignore` copy exclusions.
* `ExamCompanion.Web.csproj` project references and target framework.
* `wwwroot/assets/gramophone` runtime asset manifest.

---

## 4. Current State Summary

The exact static prototype lives at `C:/Users/r.soroush/Desktop/Exam_companion/GramophoneEntrancePrototype`. Its nine-stage 4.90-second entrance uses one `requestAnimationFrame` loop: absolute elapsed time selects a stage, one sampler computes the full character pose, and one renderer writes all SVG transforms synchronously.

The latest verified full game is `C:/Users/r.soroush/Desktop/new`, not the older dirty clone beside the prototype. It already has corrected prototype-derived IK/pose mechanics in `gramophoneEntranceRig.js`, but converts every stage into dense per-node keyframes executed through multiple `Element.animate()` players. Its full one-shot entrance continues through record placement, wobble, wake, listen, and question readiness. It also owns Blazor game integration, audio synchronization, semantic reactions, cancellation, reduced motion, SQLite persistence, responsive UI, and tests.

No target sibling version exists. Both main-game working folders are about 700 MB because they contain generated/local artifacts; neither should be recursively copied as-is.

---

## 5. Intended Outcome

`C:/Users/r.soroush/Desktop/Exam_companion/ExamCompanionPrototypeMotion` is a clean, independently runnable snapshot of the latest full game. It contains an unchanged internal copy of the static prototype for provenance and comparison.

In the new version, the articulated entrance from walk through hold runs from one centralized, absolute-time animation-frame scheduler. Each frame samples the corrected rig model once and applies the complete pose coherently, matching the prototype's execution approach while retaining the current game's artwork, accessible grooves, turntable, and lifecycle. The existing place-disc/wobble/wake/listen handoff still reaches a playable record and focused question. Gameplay reactions and audio formulas remain behaviorally unchanged.

The original prototype, `Desktop/new`, and `Desktop/Exam_companion/new` remain untouched.

---

## 6. Scope

### In Scope

* Create the new sibling `ExamCompanionPrototypeMotion` from a selective snapshot of `Desktop/new`.
* Preserve all required current uncommitted source/runtime assets.
* Copy the static prototype unchanged into the new version as `_prototype-reference/GramophoneEntrancePrototype`.
* Exclude source-control metadata, build output, tooling caches, browser profiles, logs, packages, secrets, and active player state.
* Refactor only the new version's prototype-covered entrance to a single rAF timeline.
* Preserve the current pure corrected rig sampler and the `gameMotion.js` public JS interop API.
* Preserve the playable record handoff, question readiness, reduced motion, semantic reactions, and audio behavior.
* Extend Motion Lab diagnostics to prove the new runtime is active and uses one entrance frame loop.
* Update and run relevant tests in the new sibling.
* Document source provenance, copy policy, motion ownership, and verification in the new sibling.

### Out of Scope

* Modifying any existing source/prototype folder.
* Reconciling or deleting the older `Desktop/Exam_companion/new` clone.
* Creating a remote repository, branch, commit, or push.
* Introducing React, Vue, canvas, GSAP, or another frontend framework/animation library.
* Rewriting semantic answer/milestone/repair/final-bow motions that have no prototype equivalent.
* Changing musical synthesis, noise duration, question behavior, topic progression, scoring, persistence entities, or migrations.
* Sharing a writable SQLite player database between versions.

---

## 7. Constraints and Invariants

### Constraints

* All implementation occurs only inside the new sibling after it is created.
* The target directory must not be overwritten if it already exists or becomes non-empty.
* The copy must come from `C:/Users/r.soroush/Desktop/new`, including its required uncommitted source files.
* The original prototype reference copy remains unmodified and is never imported as production JavaScript.
* Browser-native APIs are sufficient; no new frontend package is allowed unless execution proves a required browser capability is unavailable.
* The entrance frame scheduler must be host/controller scoped, not global singleton state shared across pages.

### Invariants

* Blazor/.NET remains authoritative for questions, correctness, progress, repair, and persistence.
* The existing `gameMotion.js` controller methods called by `Game.razor` and `MotionLab.razor` retain their names and behavioral contracts.
* Fresh-session entrance remains one-shot; refresh/resume/rerender does not replay it.
* Normal entrance ends with pocket record hidden, playable record visible, pocket closed, no active entrance channel, and question focus ready.
* Reduced motion skips physical gait/sit/pocket travel and still reaches the same ready state.
* Cancel, reset, hidden tab, root replacement, navigation, and disposal prevent stale frames or completion callbacks.
* Pause/resume and playback-rate changes preserve the current visible pose.
* Audio timing/formulas and semantic gameplay reaction results remain unchanged.
* The target uses an independent fresh `data/exam-companion.db`; source question/catalog databases may be copied read-only as seed inputs.

---

## 8. Architectural Decisions

### AD1 — Use the latest verified main game as the functional base

**Decision**

Create the sibling from `C:/Users/r.soroush/Desktop/new` and not from `Desktop/Exam_companion/new`.

**Reason**

Only `Desktop/new` contains the completed corrected rig, current tests, and verified lifecycle changes. The adjacent clone is older despite sharing the same Git commit.

**Alternatives Considered**

* Copy the older physically adjacent main game.
* Reconcile both dirty clones first.
* Build upward from the static prototype.

**Why Rejected**

The first loses verified work; the second changes existing folders outside scope; the third would require reconstructing nearly the whole Blazor application and risks functional divergence.

**Affected Tasks**

* `T1`, `T2`

### AD2 — Create a clean unversioned snapshot with explicit provenance

**Decision**

Use target `C:/Users/r.soroush/Desktop/Exam_companion/ExamCompanionPrototypeMotion`. Do not copy `.git` and do not initialize or commit a new repository during this task. Record source path, branch, commit, dirty status, timestamp, inclusions, and exclusions in the target documentation.

**Reason**

The source's required implementation is uncommitted. A Git worktree would omit it, while copying `.git` would produce a misleading independent clone with large dirty state. Repository setup can be requested separately after the variant is verified.

**Alternatives Considered**

* Git worktree from current commit.
* Full recursive copy including `.git`.
* Fresh Git repository with an automatic initial commit.

**Why Rejected**

The worktree loses uncommitted work; full copying duplicates metadata and generated files; committing was not requested.

**Affected Tasks**

* `T1`, `T2`, `T8`

### AD3 — Use a selective source snapshot and independent data

**Decision**

Copy required application source/configuration/assets/tests and seed/reference data. Exclude `.git`, `.vs`, `.tools`, `bin`, `obj`, `artifacts`, `.edge-*`, `.chrome-*`, `package`, `node_modules`, logs, archives, local secrets/config, and `data/exam-companion.db*`. Allow Development startup to create/migrate/seed a fresh target database.

**Reason**

This keeps the new version small, reproducible, safe to run beside the original, and free of current player state and browser cache.

**Alternatives Considered**

* Full recursive copy.
* Share or copy the writable active database.
* Copy only Git-tracked files.

**Why Rejected**

Full copying duplicates about 700 MB of local/generated state; database sharing creates locking and state ambiguity; tracked-only copying loses required uncommitted rig/assets/tests.

**Affected Tasks**

* `T1`, `T2`

### AD4 — Reproduce the prototype runtime for prototype-covered entrance stages

**Decision**

Walk-in through hold-disc will use one controller-scoped `requestAnimationFrame` runner. It maps absolute scaled elapsed time to a named stage, calls `sampleEntrancePose()` directly once per frame, and applies all pose outputs synchronously. It must not prebuild dense 60 fps keyframes or create `Animation` players for those stages.

**Reason**

This is the distinctive execution approach in the prototype and directly satisfies the user's request to see motion produced that way.

**Alternatives Considered**

* Keep current dense WAAPI tracks because they look similar.
* Import prototype `animation.js` unchanged.
* Convert every game motion to rAF immediately.

**Why Rejected**

Keeping WAAPI does not fulfill the explicit approach request; raw prototype code uses different DOM/geometry and lacks lifecycle safety; converting unrelated reactions has no prototype basis and adds unnecessary risk.

**Affected Tasks**

* `T3`, `T4`, `T5`, `T6`

### AD5 — Preserve the corrected pure sampler, not the prototype's raw solver

**Decision**

Use `gramophoneEntranceRig.js` as the pose source and preserve its independently verified local-angle correction, actual main-game geometry, rigid wrist/disc relationship, and place-disc endpoint. The prototype is a behavioral/runtime reference, not production code.

**Reason**

The raw prototype solver was not built against the full game DOM and does not include the corrected rendered-joint guarantees established by the current tests.

**Alternatives Considered**

* Copy `solve`, `leg`, `arm`, and `render` directly from the prototype.

**Why Rejected**

That would reintroduce geometry differences and discard verified corrections.

**Affected Tasks**

* `T3`, `T4`, `T6`

### AD6 — Preserve public controller/API and gameplay boundaries

**Decision**

`gameMotion.js` remains the sole public controller module. A new private module, `gramophoneFrameEntrance.js`, owns frame timing and pose application. `Game.razor`, Domain, Application, Infrastructure, and database schema remain unchanged unless a proven binding defect requires a narrow presentation-only adjustment.

**Reason**

This localizes risk and retains all current Blazor interop and game business behavior.

**Alternatives Considered**

* Move animation state into Blazor rendering.
* Replace `gameMotion.js` with the prototype script.

**Why Rejected**

Server/component rerendering is inappropriate for frame-level SVG motion; replacing the controller loses audio, reactions, lifecycle, and diagnostics.

**Affected Tasks**

* `T3`, `T4`, `T5`

### AD7 — Retain the full game's post-hold readiness tail

**Decision**

After the rAF-driven hold, keep the existing continuous place-disc handoff, record wobble, wake, listen, entrance-complete event, and question focus. The post-hold stages may remain under the existing tracked executor because they are not part of the supplied prototype, provided the handoff has no snap and cancellation remains unified.

**Reason**

The prototype intentionally stops while holding the record, but the main game requires a mounted playable record before questions/audio.

**Alternatives Considered**

* End the full game at the held record.
* Redesign question/playback to work without mounted record.

**Why Rejected**

Both would break current gameplay and expand beyond the requested visual runtime change.

**Affected Tasks**

* `T4`, `T5`, `T6`

### AD8 — Add no frontend framework or animation package

**Decision**

Use browser-native rAF, SVG transforms, `performance.now()`, and the existing pure sampler/controller.

**Reason**

The prototype proves the required approach without a dependency. A new tool would add bundle and ownership complexity without solving a missing capability.

**Alternatives Considered**

* GSAP or another animation library.
* Canvas/WebGL renderer.

**Why Rejected**

Neither is necessary for the existing SVG rig or acceptance criteria.

**Affected Tasks**

* `T3`, `T4`

---

## 9. Target Flow

1. The executor creates a clean, non-existing sibling from the latest active main-game source and copies the static prototype into a reference-only subfolder.
2. Development startup creates and seeds an independent target `exam-companion.db` from copied source banks.
3. Blazor renders the existing full-game SVG and creates the unchanged `gameMotion.js` public controller.
4. A fresh-session marker invokes `entranceSequence()` once.
5. The controller starts one `gramophoneFrameEntrance` runner for walk through hold.
6. On each frame, the runner computes scaled absolute time, selects the stage, calls the corrected sampler once, and applies the full pose synchronously to the live SVG parts.
7. Pause/rate/cancel/reduced/hidden/dispose operations update or invalidate that runner through the existing controller lifecycle.
8. Hold completes into the existing continuous place-disc/wobble/wake/listen tail.
9. The temporary record is hidden, playable record is visible, entrance completion fires, and question focus/readiness proceeds unchanged.
10. Gameplay reactions, audio playback, scratch, repair, and completion continue through their existing public contracts.

---

## 10. Data Changes

### Models / Entities

No entity, DTO, EF mapping, or relationship change.

### Database

**Migration Required:** `NO`

**Compatibility Concerns:**

* Do not share the active source `data/exam-companion.db` between old and new processes.
* Copy only source/reference SQLite banks required for Development seeding.
* Production still requires a prepared database under the target's own `data` path.

**Existing Data Impact:**

* Existing source player/session data remains untouched.
* The target begins with a fresh independent game database and first-use state.

---

## 11. Integration Changes

### Integration: Blazor to motion controller

**Direction**

`Game.razor` / `MotionLab.razor` -> `gameMotion.js` -> private frame entrance runner -> live SVG.

**Contract Impact**

No public JS interop method rename or signature change. Diagnostics gain runtime-specific fields such as `entranceRuntime`, `frameLoopActive`, `frameCount`, `absoluteTime`, `stage`, and `activeFrameLoops`.

**Failure Behavior**

Missing/disconnected SVG parts prevent start and return a coherent inactive result; exceptions cancel the runner, restore coherent record visibility, and suppress entrance completion/focus.

**Retry Behavior**

Motion Lab may replay/reset. Gameplay entrance stays one-shot according to existing session-marker behavior.

**Idempotency**

One active entrance runner per controller. Starting/replacing/resetting increments the existing generation and cancels the prior frame request before new work begins.

**Compatibility**

All existing Blazor calls remain valid. Existing semantic motion and audio APIs remain unchanged.

### Integration: Frame entrance to pose sampler

**Direction**

`gramophoneFrameEntrance.js` -> `gramophoneEntranceRig.js`.

**Contract Impact**

Add only pure exports needed to resolve total duration/stage-at-time or reuse existing `entranceStages` and `sampleEntrancePose`. Do not add DOM access to the sampler.

**Failure Behavior**

Unknown stages, non-finite samples, unintended IK clamp, or missing required nodes fail before completion and surface through diagnostics/tests.

**Compatibility**

Existing sampler tests and Motion Lab stage previews continue to work.

---

## 12. Transaction and Consistency Strategy

### Transaction Boundary

Gameplay persistence remains entirely in the existing .NET Application/Infrastructure operation. Animation has no database transaction.

### Commit Sequence

Answer/session state is saved before visual feedback as today. Entrance readiness is presentation-only and does not commit data.

### External Operations

No new network/external operation.

### Failure Semantics

An entrance runtime failure must not mutate learning data. It must cancel all entrance frames, retain/resume a coherent mounted-record state only through explicit reset/recovery logic, and not fire a stale focus callback.

### Retry / Recovery

Motion Lab supports reset/replay. In gameplay, reload follows existing resume behavior without replaying the fresh entrance; the game remains usable because authoritative state is persisted independently.

### Duplicate Protection

Existing fresh-session marker plus one-runner-per-controller generation prevents duplicate entrance execution.

---

## 13. Security Impact

### Authentication

No change; application remains a local single-user MVP.

### Authorization

No change. Motion Lab remains Development-only and must return unavailable/not found in Production.

### Identity / Ownership

New variant uses its own local relative database. Do not copy `.env`, `secrets.json`, `appsettings.Local.json`, browser profiles, or source `.git` metadata.

### Required Security Tests

* Published Production does not expose `/motion-lab`.
* Published Production does not expose developer-answer controls.
* Copy audit proves excluded secret/local files are absent.

---

## 14. Files and Change Types

### Create

* `C:/Users/r.soroush/Desktop/Exam_companion/ExamCompanionPrototypeMotion/**` — selective full-game snapshot.
* `.../ExamCompanionPrototypeMotion/_prototype-reference/GramophoneEntrancePrototype/**` — unchanged reference snapshot.
* `.../ExamCompanionPrototypeMotion/wwwroot/js/gramophoneFrameEntrance.js` — centralized entrance runner.
* `.../ExamCompanionPrototypeMotion/docs/prototype-motion-variant.md` — provenance, copy/data policy, runtime boundary, run/test commands.
* `.../ExamCompanionPrototypeMotion/tests/browser/frame-entrance.cjs` — focused runtime/geometry/lifecycle coverage.

### Modify

* Target `wwwroot/js/gameMotion.js`.
* Target `wwwroot/js/gramophoneEntranceRig.js` only for narrow pure timeline helpers/diagnostics if required.
* Target `Components/Pages/MotionLab.razor` and `.css`.
* Target motion/browser test files where entrance runtime assertions change.
* Target `docs/gramophone-art.md`, `docs/AgentSteps/currentState.md`, `README.md`, and `ARCHITECTURE.md`.

### Possibly Modify

* Target `Components/Game/Gramophone.razor` / `.css` only if a stable data-part or transform-layer boundary is missing for direct frame writes.
* Target `Components/Pages/Game.razor` only if diagnostics or a proven readiness-binding defect requires it; public behavior must not change.
* Target `.gitignore` only to add the internal reference/copy-specific generated path if needed.

### Read-Only References

* Original `GramophoneEntrancePrototype/**`.
* Original `C:/Users/r.soroush/Desktop/new/**` after target creation.
* Original `C:/Users/r.soroush/Desktop/Exam_companion/new/**`.
* Target Domain/Application/Infrastructure source and migrations.

### Delete

No source file is planned for deletion. The copied target must simply omit excluded generated/local files.

---

# 15. Implementation Tasks

## T1 — Preflight and create the clean sibling snapshot

### Objective

Create the target folder safely from the latest source while preserving originals and excluding generated/local state.

### Change Type

`CREATE`

### Files

* Source: `C:/Users/r.soroush/Desktop/new/**`
* Target: `C:/Users/r.soroush/Desktop/Exam_companion/ExamCompanionPrototypeMotion/**`
* Prototype: `C:/Users/r.soroush/Desktop/Exam_companion/GramophoneEntrancePrototype/**`

### Relevant Symbols

* Not code-symbol based; filesystem manifest and source provenance.

### Prerequisites

* Target path does not exist or is empty.
* Source critical files exist: solution, Web project, `Program.cs`, all three `src` projects, Components, `wwwroot/js/gameMotion.js`, `gramophoneEntranceRig.js`, runtime assets, tests, source databases.

### Instructions

* Resolve and compare absolute source/target paths before writing; stop if target resolves inside the source or prototype.
* Snapshot source branch, commit, `git status --short`, timestamp, and critical file hashes for later provenance documentation.
* Selectively copy application source, configs, assets, tests, docs, solution/project files, scripts, and seed/reference data.
* Explicitly include required untracked source: Motion Lab, rig module, gramophone assets, focused browser tests, and AgentSteps current state.
* Exclude `.git`, `.vs`, `.tools`, `bin`, `obj`, `artifacts`, `.edge-*`, `.chrome-*`, `node_modules`, `package`, logs, archives, transient screenshots, local secrets/config, workflow `temp` outputs, and `data/exam-companion.db*`.
* Copy the prototype unchanged to `_prototype-reference/GramophoneEntrancePrototype` and store its file hashes.
* Do not remove, move, or edit anything in either source folder.

### Constraints

* Never overwrite a non-empty target.
* Do not use a broad recursive copy followed by deletion.
* Do not initialize Git or commit.
* Preserve relative paths and file contents.

### Completion Conditions

* Target contains a complete source/build/test tree and exact internal prototype reference.
* Excluded directories/files are absent.
* Critical source and prototype hashes match.
* Existing source/prototype Git statuses and hashes are unchanged.

### Verification

* List target top-level tree and excluded-path checks.
* Compare SHA-256 for solution, controller, sampler, SVG component/assets, tests, and every prototype reference file.
* Record target size; it should be materially smaller than the roughly 700 MB source working folder before restore/build.

---

## T2 — Establish a runnable independent baseline

### Objective

Prove the clean target is a complete full game before changing its motion runtime.

### Change Type

`CONFIGURE`

### Files

* Target solution/projects/configuration.
* Target `data/` seed/reference inputs.
* Target `docs/prototype-motion-variant.md`.

### Relevant Symbols

* `Program.cs` Development migration/seed block.
* `MusicDbContext` and `MusicSeed` only as existing runtime behavior.

### Prerequisites

* `T1`.

### Instructions

* Restore/build using target-local working directory and target-specific temporary output if another executable is locked.
* Start target in Development on a unique port.
* Confirm a new target-local `data/exam-companion.db` is created/migrated/seeded and the home/game routes render.
* Confirm source `Desktop/new/data/exam-companion.db*` timestamps/hashes do not change.
* Create the provenance document with the T1 manifest, fresh-data policy, and exact baseline commands/results.

### Constraints

* Do not change connection-string semantics unless the database resolves outside the target; if so, stop as a blocker.
* Do not copy current player/session rows.

### Completion Conditions

* Target solution builds and launches independently.
* A fresh game can be created using target-local data.
* The source game and prototype remain unchanged.

### Verification

* `dotnet build ExamCompanion.sln --no-restore` after restore as required.
* Target-local database path and initial application render.
* Minimal existing smoke startup check before motion refactor.

---

## T3 — Implement the controller-scoped absolute-time entrance runner

### Objective

Create the reusable private runtime that reproduces the prototype's single-loop, absolute-time pose application with production lifecycle controls.

### Change Type

`CREATE`

### Files

* Target `wwwroot/js/gramophoneFrameEntrance.js`.
* Target `wwwroot/js/gramophoneEntranceRig.js` (`POSSIBLY_MODIFY`).

### Relevant Symbols

* New `createFrameEntranceRunner` or equivalent private factory.
* `entranceStages`, `sampleEntrancePose`, rig geometry readers/transform outputs.

### Prerequisites

* `T2`.

### Instructions

* Implement one runner per motion controller, with one pending rAF at most.
* Use `performance.now()` and accumulated paused duration/rate segments to compute absolute scaled timeline time without pose jumps.
* Map absolute time into stage name/progress and call the pure pose sampler exactly once per rendered frame.
* Apply all returned transforms/opacity/visibility in one frame commit to the already resolved live part nodes.
* Expose start, pause, resume, setRate, cancel, current diagnostics, and completion promise/callback.
* Require an external generation/validity predicate so stale runners cannot commit frames or completion.
* On cancel, cancel the pending rAF, mark inactive, and never call completion.
* Treat hidden documents as cancellation in production entrance, consistent with current controller behavior.
* Keep sampler pure; no DOM, timers, storage, or audio in `gramophoneEntranceRig.js`.
* Do not import or execute the reference prototype script.

### Constraints

* No `Element.animate()` or prebuilt 60 fps keyframes inside the runner.
* No global singleton runner.
* No uncontrolled autoplay.
* Non-finite poses, missing required nodes, or unintended clamp fail fast with diagnostics.

### Completion Conditions

* A unit/in-browser harness can run one stage or the nine-stage walk-to-hold timeline with one active rAF loop.
* Pause/resume/rate changes preserve pose continuity.
* Cancel/replacement prevents further DOM writes/completion.
* Directly rendered foot/wrist/disc geometry remains within existing tolerances.

### Verification

* JavaScript syntax/import checks.
* Focused deterministic clock tests where practical.
* Motion Lab/in-browser sampling at normal, paused, resumed, 0.35x, and cancelled states after T5 wiring.

---

## T4 — Integrate the frame runner into the live entrance

### Objective

Route the prototype-covered production entrance through the new runner while preserving the controller API and post-hold ready-state tail.

### Change Type

`MODIFY`

### Files

* Target `wwwroot/js/gameMotion.js`.
* Target `Components/Game/Gramophone.razor` / `.css` only if proven necessary.
* Target `Components/Pages/Game.razor` only if proven necessary.

### Relevant Symbols

* `create`, `perform`, `sequence`, `entranceSequence`, `setEntrance`, `cancelAllMotion`, `pause`, `resume`, `setSlowMotion`, `setReducedMotion`, `stop`, `dispose`, `diagnostics`.

### Prerequisites

* `T3`.

### Instructions

* Import and instantiate the runner inside each `gameMotion` controller.
* Normal production entrance uses the frame runner continuously from `walk-in` through `hold-disc`.
* Emit the same stage-start/stage-complete observable events at the planned absolute boundaries so existing readiness/test semantics remain inspectable.
* After hold, transfer without snap to existing `place-disc`, wobble, wake, and listen tail.
* Keep temporary pocket record visibility and playable record visibility coherent at every boundary.
* Wire existing pause/resume/slow-rate/cancel/reset/hidden/root replacement/dispose operations to both the frame runner and remaining tracked motions.
* Maintain one sequence generation; only the live generation may commit the tail or focus the question.
* Keep reduced-motion entrance on the existing concise ready-state path without starting the physical frame runner.
* Do not change public module/controller method names or Blazor call sites unless a binding defect is proven.
* Keep semantic non-entrance reactions under the current executor.

### Constraints

* No business, persistence, question, or audio formula change.
* No simultaneous old rig WAAPI players for prototype-covered stages.
* A cancelled frame entrance cannot leave `record-root` missing.

### Completion Conditions

* Fresh sessions visibly execute the frame-driven articulated entrance once.
* Diagnostics report rAF runtime and exactly one active entrance loop.
* No rig `Animation` players exist during walk-to-hold.
* Placement/readiness and existing/resumed session behavior remain correct.

### Verification

* Fresh and resumed game observation.
* Browser instrumentation of `Element.prototype.animate` confirms no calls for rig parts during prototype-covered stages.
* Event trace/timing and final record/focus assertions.

---

## T5 — Adapt Motion Lab for direct runtime inspection

### Objective

Make the new runtime directly visible and debuggable without gameplay/data changes.

### Change Type

`MODIFY`

### Files

* Target `Components/Pages/MotionLab.razor`.
* Target `Components/Pages/MotionLab.razor.css`.
* Target `wwwroot/js/gameMotion.js` diagnostics/preview surface.

### Relevant Symbols

* `Motions`, `preview`, `pause`, `resume`, `cancelMotion`, `reset`, `setSlowMotion`, `diagnostics`.

### Prerequisites

* `T4`.

### Instructions

* Full entrance and each prototype stage must use the same frame runner as production, not a separate demo renderer.
* Display runtime type, active loop count, current absolute time, stage/progress, frame count, rate, paused state, generation, clamp state, sampled/rendered feet, wrist, disc, and attachment distance.
* Keep existing semantic reaction controls available for compatibility checks.
* Preserve reset, root rebinding, reduced motion, and pivot inspection.
* Clearly label prototype-reference timing versus the full-game post-hold tail.

### Constraints

* Development-only route remains unavailable in Production.
* Motion Lab must not create or modify player data.

### Completion Conditions

* User can visibly play the frame-driven walk-to-hold and full entrance at normal and slow speed.
* Diagnostics prove one frame loop and actual rendered constraints.
* Reset returns a coherent neutral/ready composition.

### Verification

* Update/run `motion-lab.cjs` against target Development host.
* Manual slow-motion inspection at desktop and 390px viewport.

---

## T6 — Add focused frame-runtime acceptance coverage

### Objective

Prove that the target uses the prototype execution approach and retains geometric/lifecycle correctness.

### Change Type

`TEST`

### Files

* Target `tests/browser/frame-entrance.cjs`.
* Target `tests/browser/entrance-rig.cjs`.
* Target `tests/browser/motion-lab.cjs`.
* Target `tests/browser/motion.cjs`.
* Target `tests/browser/visible-motion.cjs`.

### Relevant Symbols

* Entrance event traces, diagnostics, live SVG transforms, animation instrumentation, cancellation scenarios.

### Prerequisites

* `T5`.

### Instructions

* Instrument rAF and `Element.prototype.animate` before controller initialization.
* Assert one active entrance rAF loop, increasing absolute time/frame count, and zero rig-part WAAPI starts from walk through hold.
* Preserve independent rendered checks for planted stance foot, alternating swing lift, material sit, flap/reveal order, rigid hand/disc constraint, readable 500 ms hold, and continuous placement handoff.
* Test pause, resume, 0.35x rate, mid-run rate change without jump, stage cancellation, same-name replacement, hidden tab, root replacement, reduced-motion change, navigation, and disposal.
* Preserve exact one-shot entrance, final record composition, focus readiness, responsive fit, semantic reactions, scratch, repair, playback, and final bow assertions.
* Tests must remove only sessions they create and must not touch source-folder data.

### Constraints

* Assert observable runtime/geometry, not only implementation strings.
* Do not delete existing unrelated regression coverage.

### Completion Conditions

* Focused runtime, rig, Motion Lab, production motion, and visible-motion tests pass without console errors/unhandled rejections.
* Tests fail if the entrance silently falls back to dense WAAPI rig tracks.

### Verification

* `node tests/browser/frame-entrance.cjs`.
* `node tests/browser/entrance-rig.cjs`.
* `node tests/browser/motion-lab.cjs`.
* `node tests/browser/motion.cjs`.
* `node tests/browser/visible-motion.cjs` in normal and blocked-audio modes.

---

## T7 — Run full functional and Production regression

### Objective

Verify the new sibling remains a complete main game, not only a motion demo.

### Change Type

`TEST`

### Files

* Target solution/projects.
* Target `tests/ExamCompanion.Application.Tests`.
* Target `tests/browser/smoke.cjs`, `audio.cjs`, and `production.cjs`.

### Relevant Symbols

* Full game journey, persistence, audio schedule, publish configuration, Production environment gates.

### Prerequisites

* `T6`.

### Instructions

* Build and run .NET tests using target-local paths.
* Run responsive smoke journey through all five topics, scratch, repair, persistence, and completion.
* Run actual offline audio rendering to prove formulas/tails are unchanged.
* Publish to a target-specific temporary directory; launch with target-local prepared database and run Production checks.
* Verify Motion Lab and developer-answer controls remain unavailable in Production.
* Verify source game/prototype files and databases remain unchanged after the entire suite.

### Constraints

* Do not reuse the source app's writable database.
* Stop only processes launched by this execution and record their ports/PIDs.

### Completion Conditions

* Solution build, application tests, focused browser tests, smoke, audio, and published Production tests pass.
* No source/prototype mutation occurred.

### Verification

* `dotnet build ExamCompanion.sln --no-restore`.
* `dotnet test tests/ExamCompanion.Application.Tests --no-build` or equivalent valid project command.
* Target-host browser suite with `EXAM_URL`.
* Published-host Production test with `EXAM_PRODUCTION_URL`.

---

## T8 — Finalize target documentation and handoff

### Objective

Document the new sibling's actual verified ownership, provenance, data isolation, runtime behavior, and test results.

### Change Type

`MODIFY`

### Files

* Target `docs/prototype-motion-variant.md`.
* Target `docs/gramophone-art.md`.
* Target `docs/AgentSteps/currentState.md`.
* Target `README.md`.
* Target `ARCHITECTURE.md`.
* Source `docs/AgentSteps/temp/step1Exc_output.md` / `step2Exc_output.md` only when later execution instructions explicitly assign workflow reporting there.

### Relevant Symbols

* Motion runtime ownership, entrance stage/timing, public controller boundary, data policy, reference snapshot, tests.

### Prerequisites

* `T7`.

### Instructions

* Record target path and source/prototype provenance hashes.
* Document the selective-copy exclusions and independent database policy.
* State precisely that prototype-covered entrance mechanics use one rAF runner while semantic reactions retain their current executor and playback retains the audio clock.
* Document stage timing, hold-to-placement boundary, reduced motion, cancellation, and diagnostics.
* Document only commands/results actually verified.
* Update target CurrentState without rewriting source CurrentState.

### Constraints

* Do not claim all game motion is rAF-driven when semantic reactions remain WAAPI.
* Do not modify Discovery or this Plan output during execution.

### Completion Conditions

* Target documentation matches source and observed tests.
* A future maintainer can identify the target's source, reference prototype, motion boundary, data isolation, run commands, and known limitations without inspecting the original folders.

### Verification

* Compare documented file names, stage order, timing, runtime ownership, and commands against final target source/tests.
* Run link/path existence checks and `git diff --check` where applicable.

---

## 16. Task Dependency Order

`T1 -> T2 -> T3 -> T4 -> T5 -> T6 -> T7 -> T8`

---

## 17. Error and Failure Scenarios

### Target already exists or is non-empty

**Condition**

`ExamCompanionPrototypeMotion` exists before T1 or appears during copy.

**Expected Behavior**

Stop without overwriting, merging, deleting, or renaming it. Report its resolved path and contents summary.

**Relevant Task**

`T1`

### Source changes during snapshot

**Condition**

Critical source hashes/status differ between pre-copy and post-copy checks.

**Expected Behavior**

Stop before implementation. Preserve target for inspection and report changed files; do not silently combine versions.

**Relevant Task**

`T1`

### Clean target cannot seed independently

**Condition**

Startup resolves its connection outside the target or required source bank is absent.

**Expected Behavior**

Do not point it at the source writable database. Identify the missing input/path and mark T2 blocked.

**Relevant Task**

`T2`

### Frame runner loses live SVG binding

**Condition**

Blazor rerender replaces/disconnects the bound root during entrance.

**Expected Behavior**

Cancel the old runner without completion/focus; controller refresh/retry follows existing binding rules and restores coherent visual state.

**Relevant Task**

`T3`, `T4`

### Hidden/cancelled runner commits a stale frame

**Condition**

A queued callback executes after cancellation/generation replacement.

**Expected Behavior**

Validity check rejects the frame before DOM writes; no tail or focus callback runs.

**Relevant Task**

`T3`, `T4`, `T6`

### Hold-to-place snap

**Condition**

The rAF final held transform differs from the first place-disc transform.

**Expected Behavior**

Use the current sampled exact held matrix as the placement start; do not hide the discontinuity with opacity or shorten hold.

**Relevant Task**

`T4`, `T6`

### Reduced motion toggled mid-entrance

**Condition**

User/OS preference changes while the frame runner is active.

**Expected Behavior**

Cancel runner/generation, mount coherent playable state through existing reduced path, and never emit duplicate completion.

**Relevant Task**

`T4`, `T6`

---

## 18. Verification Plan

### Build

* Restore as required, then build target `ExamCompanion.sln` using target-specific output if necessary.
* Publish target Web project to a target-specific temporary directory.

### Unit Tests

* Existing `ExamCompanion.Application.Tests` because the copied full game and independent database must retain business behavior.
* JavaScript syntax/module checks and deterministic timing helper tests if added.

### Integration Tests

* `frame-entrance.cjs`, `entrance-rig.cjs`, and `motion-lab.cjs` against target Development host.
* `motion.cjs` and `visible-motion.cjs` for entrance/reaction/lifecycle integration.
* `smoke.cjs` for full five-topic/persistence/scratch/repair flow.
* `audio.cjs` for unchanged synthesized audio/noise/tail behavior.
* `production.cjs` against published Production output.

### Regression Checks

* One active rAF entrance loop and no rig-part WAAPI starts during walk-to-hold.
* Exact stage order/timing within browser tolerance.
* Rendered foot, joint, pocket, wrist/disc, hold, and placement constraints.
* One-shot/reduced/cancel/reset/hidden/root-replacement/navigation/disposal behavior.
* Existing semantic reactions, playback, scratch, repair, final bow, responsive fit, focus, and accessibility.
* Production hides developer UI/Motion Lab.
* Source/prototype critical hashes and source database hashes/timestamps unchanged.

### Manual Verification

* Compare target Motion Lab full entrance beside the static prototype at normal and slow rate.
* Inspect desktop and 390px mobile composition.
* Listen to clean, damaged, repaired, and cycle-complete audio on an actual output device; automated tests cannot judge subjective sound quality.

---

## 19. Acceptance Criteria

* [ ] A new sibling exists at `Desktop/Exam_companion/ExamCompanionPrototypeMotion` and existing folders are unchanged.
* [ ] The target is a complete runnable Blazor game, not only a static prototype.
* [ ] The exact static prototype is retained unchanged in the target reference folder.
* [ ] Generated/local/source-control/secret artifacts and active player DB are absent from the initial clean snapshot.
* [ ] Target creates and uses its own fresh `data/exam-companion.db`.
* [ ] Walk through hold uses one centralized absolute-time rAF loop.
* [ ] Prototype-covered rig parts do not start WAAPI animations during that interval.
* [ ] Each frame samples one coherent complete pose from the corrected main-game rig model.
* [ ] Alternating gait, planted stance foot, swing lift, body bob/weight shift, arm counter-swing, and delayed horn sway are visible.
* [ ] Sit, notice, reach, open, grab, pull, and 500 ms hold remain separately readable.
* [ ] Pocket reveal and wrist/disc attachment stay within existing rendered tolerances.
* [ ] Hold transitions continuously to place/wobble/wake/listen and final playable/focused state.
* [ ] Fresh entrance runs once; resume/rerender does not replay it.
* [ ] Pause/resume/rate/cancel/reset/hidden/root replacement/navigation/disposal remain correct.
* [ ] Reduced motion skips physical entrance travel and reaches coherent readiness.
* [ ] Semantic answer/milestone/scratch/repair/final motions and audio behavior remain correct.
* [ ] Motion Lab proves runtime type, one loop, timing, and rendered constraints.
* [ ] Responsive Development and published Production checks pass without console errors.
* [ ] No database schema, learning, scoring, topic, repair, or question behavior changes.
* [ ] Target documentation accurately records provenance, architecture, data isolation, and verification.

---

## 20. Risks and Mitigations

### R1 — Dirty-source snapshot loses or mixes work

**Severity:** `HIGH`

**Risk**

Required motion source is uncommitted and two clones diverge.

**Mitigation**

Use only `Desktop/new`, capture pre/post status and hashes, copy via explicit allow/exclude policy, and verify critical files before implementation.

**Relevant Tasks**

* `T1`, `T2`

### R2 — Destructive or bloated folder copying

**Severity:** `HIGH`

**Risk**

Blind copying may overwrite a target or duplicate `.git`, generated outputs, browser profiles, secrets, and hundreds of MB.

**Mitigation**

Require a non-existing/empty validated target, selective copy, exclusion audit, and no post-copy bulk deletion.

**Relevant Tasks**

* `T1`

### R3 — rAF cancellation race mutates newer state

**Severity:** `HIGH`

**Risk**

A queued frame/completion can run after replacement, navigation, hidden tab, or disposal.

**Mitigation**

One runner per controller, one pending frame, generation predicate before every DOM write/completion, and adversarial lifecycle tests.

**Relevant Tasks**

* `T3`, `T4`, `T6`

### R4 — Timing drift or pause/rate jump

**Severity:** `HIGH`

**Risk**

Naively multiplying `performance.now()` can jump when rate changes or include paused time.

**Mitigation**

Track accumulated logical time plus active segment start/rate; rebase on every pause/resume/rate change; test current pose before/after transitions.

**Relevant Tasks**

* `T3`, `T6`

### R5 — Direct writes conflict with semantic or tail transforms

**Severity:** `HIGH`

**Risk**

Prototype frame transforms may overwrite stable outer wrappers or compete with existing WAAPI players.

**Mitigation**

Keep frame writes on inner entrance rig nodes, retain semantic outer wrappers, cancel conflicting entrance channels, and assert no concurrent rig players.

**Relevant Tasks**

* `T3`, `T4`, `T6`

### R6 — Data isolation failure

**Severity:** `HIGH`

**Risk**

New and original versions could share one writable SQLite database.

**Mitigation**

Exclude active DB, preserve relative target-local connection, verify resolved path/process behavior, and compare source DB hashes/timestamps after tests.

**Relevant Tasks**

* `T1`, `T2`, `T7`

### R7 — Prototype endpoint breaks game readiness

**Severity:** `MEDIUM`

**Risk**

Stopping at hold leaves no playable record/question-ready composition.

**Mitigation**

Preserve the verified place/wobble/wake/listen tail and assert final record/focus state.

**Relevant Tasks**

* `T4`, `T6`, `T7`

### R8 — Scope expands into unnecessary frontend rewrite

**Severity:** `MEDIUM`

**Risk**

The request could trigger a framework migration or rewriting every semantic reaction without a prototype reference.

**Mitigation**

Limit direct rAF migration to prototype-covered entrance mechanics, preserve public controller/reaction/audio contracts, and add no package unless a proven blocker requires replanning.

**Relevant Tasks**

* `T3`, `T4`, `T8`

---

## 21. Assumptions

### A1

The requested unnamed sibling may use `ExamCompanionPrototypeMotion`.

**Evidence**

No target name exists; this name describes both the full game and motion variant without colliding with current folders.

**Impact If False**

Only T1 path and target documentation change, provided the user supplies a different unused sibling name before execution.

### A2

“Use the approach that exists in the prototype” requires literal single-rAF absolute-time execution for the motion sequence the prototype actually defines, not a broad rewrite of unrelated game reactions.

**Evidence**

Prototype contains only walk-to-hold entrance mechanics; current game already matches poses visually through WAAPI but not the runtime approach.

**Impact If False**

If every semantic/gameplay motion must also migrate to rAF, the scope and tests materially expand and the plan must be revised before T3.

### A3

The latest working tree at `Desktop/new` is the intended functional baseline.

**Evidence**

It contains the completed verified rig/controller/tests missing from the adjacent clone and is the current workspace.

**Impact If False**

T1 must stop; using the older clone would knowingly omit current work.

### A4

The new version should start with fresh player state while retaining catalog/question seed inputs.

**Evidence**

It is a sibling experimental version; current architecture uses a relative local SQLite DB and Development seed flow. Sharing a writable DB creates avoidable coupling.

**Impact If False**

Data-copy/migration requirements must be specified before T1/T2; never default to sharing the live DB.

### A5

Existing post-hold record placement and question readiness remain required.

**Evidence**

The user requested a version of the main game, and those states are prerequisites for current audio/question behavior.

**Impact If False**

Changing to a permanent held-disc endpoint affects gameplay and requires replanning Game/audio/readiness contracts.

---

## 22. Expected Blockers

### Blocker: Target path already contains data

The executor must not merge or overwrite it.

**Executor Action**

`STOP_AND_MARK_BLOCKED`

**Evidence to Return**

* `T1`
* resolved target path
* contents summary
* non-destructive existence check

### Blocker: Authoritative source changes during copy

Critical hashes or Git status change between preflight and post-copy.

**Executor Action**

`STOP_AND_MARK_BLOCKED`

**Evidence to Return**

* `T1`
* changed paths/hashes/status
* copy command/check
* target state

### Blocker: Target database resolves to source/shared path

The clean target cannot start independently with existing relative configuration.

**Executor Action**

`STOP_AND_MARK_BLOCKED`

**Evidence to Return**

* `T2`
* resolved connection/path
* relevant config/Program symbols
* startup error or proof of sharing

### Blocker: Required single-loop execution cannot preserve controller lifecycle

Actual controller/DOM behavior requires an unplanned public API or architecture change.

**Executor Action**

`STOP_AND_MARK_BLOCKED`

**Evidence to Return**

* `T3` or `T4`
* exact failing lifecycle scenario
* relevant symbols/files
* minimal reproduction/test output

### Blocker: User intent requires every semantic motion to migrate

Evidence or new instruction expands “prototype approach” beyond the prototype-defined entrance.

**Executor Action**

`STOP_AND_MARK_BLOCKED`

**Evidence to Return**

* affected task
* requested expanded motion list
* current executor dependencies
* revised change/test surface

---

## 23. Escalation Rules

The executor should mark a task `BLOCKED` when:

* target safety or authoritative-source assumptions are false;
* contracts differ materially from the plan;
* an unplanned Blazor/public-controller boundary change is required;
* independent data compatibility cannot be preserved;
* a required browser capability is unavailable and would require a new dependency;
* cancellation/readiness cannot be made race-safe through existing generations;
* a required seed/reference file is missing;
* the requested motion scope expands beyond the resolved prototype-covered entrance.

Do not escalate ordinary implementation defects, minor CSS alignment, port changes, or locked default build output when a safe target-specific output is available.

---

## 24. Executor Context

### Must Read Before Execution

* `docs/AgentSteps/temp/Plan_Output.md`
* `docs/AgentSteps/temp/Discovery_Output.md`
* `docs/AgentSteps/currentState.md`
* `C:/Users/r.soroush/Desktop/Exam_companion/GramophoneEntrancePrototype/animation.js`
* `C:/Users/r.soroush/Desktop/Exam_companion/GramophoneEntrancePrototype/index.html`
* `C:/Users/r.soroush/Desktop/new/.gitignore`

### Read When Working on Specific Task

#### T1-T2

* Source solution/project/config/data layout.
* Current Git status and critical runtime/test assets.
* `Program.cs` migration/seed behavior.

#### T3

* `wwwroot/js/gramophoneEntranceRig.js`.
* Prototype `animation.js` timing/render loop.

#### T4

* `wwwroot/js/gameMotion.js`.
* `Components/Game/Gramophone.razor` and CSS.
* `Components/Pages/Game.razor` only for contract confirmation.

#### T5-T6

* Motion Lab component/CSS.
* Existing entrance-rig, motion-lab, motion, and visible-motion tests.

#### T7

* Smoke, audio, Production, and Application tests.

#### T8

* Target README, architecture, motion contract, and CurrentState.

### Do Not Rediscover

* Authoritative source is `Desktop/new`.
* Target default is `Desktop/Exam_companion/ExamCompanionPrototypeMotion`.
* Do not copy `.git` or initialize Git.
* Target uses fresh independent player data.
* Prototype is copied only as a reference and not imported into production.
* Corrected `gramophoneEntranceRig.js` remains the pose source.
* Only prototype-covered walk-to-hold motion migrates to literal centralized rAF.
* Existing post-hold tail, semantic reactions, audio contracts, Blazor API, and business layers remain.
* No new frontend framework/package is planned.

---

## 25. Expected Execution Output

The execution agent must preserve task IDs and report:

* `T1: DONE | BLOCKED`
* `T2: DONE | BLOCKED`
* `T3: DONE | BLOCKED`
* `T4: DONE | BLOCKED`
* `T5: DONE | BLOCKED`
* `T6: DONE | BLOCKED`
* `T7: DONE | BLOCKED`
* `T8: DONE | BLOCKED`

For completed tasks include target files, concise behavior, and verification. For blocked tasks include task ID, blocker, files, exact evidence/command, completed predecessors, and minimum context for continuation. Final output must state target path, task count, build/test status, acceptance count, source/prototype integrity status, target data isolation status, and documentation status.

---

## 26. Implementation Summary

* Create `Desktop/Exam_companion/ExamCompanionPrototypeMotion` as a selective snapshot of the latest `Desktop/new`, never the older adjacent clone.
* Preserve both originals; copy no `.git`, generated/cache/browser/secret state, or active player DB.
* Keep an exact static prototype snapshot inside the target as reference only.
* Add one controller-scoped absolute-time rAF runner for walk through hold.
* Sample the corrected current rig directly once per frame and synchronously apply the full pose.
* Keep `gameMotion.js` public API, Blazor/business/data boundaries, reduced motion, and lifecycle semantics.
* Preserve place/wobble/wake/listen readiness, semantic reactions, and audio behavior.
* Prove one rAF loop and zero rig WAAPI starts during prototype-covered stages through focused browser instrumentation.
* Run the full functional, audio, responsive, and published Production regression set against the independent target database.
* Document provenance, copy exclusions, runtime boundary, data isolation, and verified results only in the new sibling.
