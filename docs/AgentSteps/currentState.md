# Current State

## Project

Exam Companion is a local, single-user Persian Blazor learning game. It retains four Clean Architecture layers: Domain owns music-game entities and rules, Application owns the engine and persistence port, Infrastructure owns EF Core/SQLite persistence and seed import, and the Web project owns composition and presentation.

The current MVP uses one fixed biology lesson and five fixed topics. Gameplay data is stored in `data/exam-companion.db`; source question banks are import inputs only. Animation state is transient and creates no database fields, migrations, progression rules, score rules, or educational behavior.

## Gramophone presentation

`Components/Game/Gramophone.razor` renders the real game object as native inline SVG. Its locomotion/machine hierarchy contains separately articulated thighs, shins, feet, arms, pocket flap, pocket record, turntable record, horn, and tone arm. Outer wrappers remain stable so answer, milestone, scratch, repair, and final-lesson reactions continue to affect the whole object.

`wwwroot/js/gramophoneEntranceRig.js` is a pure deterministic sampler. It solves two-link limbs, produces stage keyframes and diagnostics, and never mutates DOM, storage, audio, or game state. `wwwroot/js/gameMotion.js` owns DOM binding, Web Animations, sequencing, channels, cancellation, focus readiness, and scratch/playback visualization. Blazor/.NET remains authoritative for questions, correctness, progress, repair, and persistence.

## Entrance and readiness

A fresh-session marker triggers one production entrance: `walk-in`, `stop-and-settle`, `sit-settle`, `notice-pocket`, `reach-pocket`, `open-pocket`, `grab-disc`, `pull-disc`, `hold-disc`, `place-disc`, `record-wobble-settle`, `wake`, and `listen-lean`. The sequence lasts about 6.72 seconds. It includes four alternating planted strides, visible sit/knee articulation, progressive pocket reveal, a wrist-attached record, a 500 ms hold, and a continuous hold-to-turntable handoff.

Only the final ready state emits entrance completion and moves focus to the question. Refresh, resume, rerender, and existing sessions do not replay the entrance. The finished composition has the pocket record hidden, playable record visible, pocket closed, and no entrance channel left active.

Reduced motion omits gait, sit, and pocket-record travel. It mounts the playable record immediately and retains short wake/listen state feedback before readiness.

## Lifecycle and audio

Motion uses generation/channel tokens so stale completions cannot modify a replacement. Pause/resume and playback-rate changes preserve the current sampled pose. Cancel/reset, hidden tabs, navigation, SVG root replacement, reduced-motion changes, and disposal stop every articulated stage and restore coherent record state.

`wwwroot/js/gameAudio.js` owns synthesis and returns Web Audio timing; `gameMotion.js` consumes that timing for the record, needle, waveform, and scratch contact. JavaScript never decides correctness or progression. Existing noise duration, challenge-note duration, muted timing, and repair behavior are unchanged.

## Development and verification

Development-only `/motion-lab` uses the production SVG and controller. It previews the full entrance and individual stages, supports pause/resume/cancel/reset/rate/reduced-motion controls, and exposes sampled and rendered geometry diagnostics. The route is unavailable in Production.

Primary regression coverage:

- `tests/browser/entrance-rig.cjs`: rendered joints, planted feet, gait, sit, pocket, rigid grip, handoff, responsive fit, lifecycle, and reduced motion.
- `tests/browser/motion-lab.cjs`: development controls, diagnostics, rebinding, and reduced motion.
- `tests/browser/motion.cjs`: production entrance contract plus all gameplay reactions.
- `tests/browser/visible-motion.cjs`: visible entrance travel and real answer/reaction behavior.
- `tests/browser/smoke.cjs`: full responsive game, persistence, scratch, and repair journey.
- `tests/browser/audio.cjs`: offline rendered audio/noise/tail behavior.
- `tests/browser/production.cjs`: published Production gates, responsive gramophone, ready record, and hidden Motion Lab.

All above checks and the solution build passed during the articulated-entrance implementation. The build emits the pre-existing `NU1903` warning for `SQLitePCLRaw.lib.e_sqlite3` 2.1.11.

## Known limitation

Audio is lightweight browser synthesis and still needs subjective listening checks on target speakers/devices. The application remains a local single-user MVP without authentication.
