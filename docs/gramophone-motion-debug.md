# Visible gramophone motion diagnosis

## Confirmed failures and fixes

1. Answer reactions were inside the playback tick, after `await audio.schedule()`. That scheduler awaited `AudioContext.resume()`, which browsers can leave pending while audio permission is unavailable. With a permanently suspended audio fixture, submitting a real educational answer created **zero animations and zero pixels of body displacement**. The direct hop API still worked in the same browser. Answer reactions now start immediately when `play()` receives the server-selected motion list. Audio resume has a 200ms bound and can fall back to a silent visual timeline. Each timeline retains its initial clock so a later resume cannot switch clock epochs halfway through playback.
2. Web Animations applied the easing curve to the entire sequence of poses. The early poses passed quickly, followed by a long nearly stationary tail. Easing now applies separately between each pair of keyframes, with a linear overall timeline. The living-object rig now produces a measured 10px correct lift, 27px cycle hop and 42px topic hop while retaining readable multi-frame phases.

These are reproduced failures, not proof of the settings in the user's existing browser tab. The new Development panel makes those settings inspectable in that tab.

## Findings that were not root causes in the reproduction

- OS reduced motion was false, local preference was absent, and the in-game checkbox was unchecked.
- The live SVG groups stayed connected before and after actual answers. Group lookup was already lazy in the existing controller.
- The body remained visible with `data-ready=true`; no answer-triggered reset to false was observed. Earlier chat claims blaming this flag were not demonstrated by a trace. The existing defensive ready assignment remains.
- Computed SVG transforms changed; there was no CSS `!important` transform override. Static record projection and animated wrapper transforms are separate.
- New sessions ran entrance; reload intentionally skipped it. Home remains a static preview.

## Development controls

The isolated Development route `/motion-lab` has one button for the complete entrance and every semantic motion, plus pause, resume, cancel and reset. Its status output reports controller/target connectivity, resolved and missing part counts, generation, active animations, browser audio state, OS reduced motion and the saved app preference. The complete entrance uses a 2.8-second drop in this lab. Preview controls are rendered and executed only in Development. They do not submit answers or alter progression. Scratch contact remains exercised by real groove playback on the shared audio schedule.

The controller exposes `refreshParts()` and refreshes its part map before every semantic motion, entrance and scratch reaction. It detects a replacement `[data-gramophone]` root and rejects detached targets. Repeated `create()`/`createController()` calls for the same host return the existing controller. This prevents duplicate controllers and stale SVG targets across Blazor rerenders.

## Verification

`node tests/browser/visible-motion.cjs` samples computed body transforms on animation frames through real correct/wrong answers. It checks visible connected targets, a >=5px correct lift, >=120ms readable lifted interval, wrong recoil, >=200ms airborne hop, preview controls and reduced-motion suppression. It captures actual browser screenshots at rest and at the hop peak in ignored `artifacts/visible-motion/`.

`node tests/browser/visible-motion.cjs --blocked-audio` runs the same flow with `resume()` deliberately never resolving. It failed with zero answer motion before the fix and now checks the same physical movement without sound permission. Only test-created sessions are removed.

Existing motion, smoke and offline audio checks cover the full semantic vocabulary, progression-triggered milestones, repair, final cadence, scratch synchronization, cancellation, reduced motion and audio source cleanup. Application progression rules and database schema are unchanged.

`node tests/browser/motion-lab.cjs` checks the isolated route, all controls, the slow visible fall, pause/resume/cancel/reset, singleton controller behavior, forced SVG-root replacement and rebinding, diagnostics, and reduced-motion substitution.
