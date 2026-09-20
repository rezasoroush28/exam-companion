# Living papercraft gramophone

The game renders one native inline-SVG gramophone from `Components/Game/Gramophone.razor`. Its paper base, articulated legs and arms, pocket, removable record, flower horn, turntable, and tone arm are real SVG groups rather than a flattened image. The historical artwork and supplied SVG files remain design references; gameplay uses the assembled component.

## Rig structure and ownership

The main hierarchy is `artifact-rest > artifact-motion > gram-locomotion > machine-root`. `artifact-rest` owns the persistent resting angle, `artifact-motion` owns gameplay reactions, `gram-locomotion` owns entrance travel, and `machine-root` owns the character body. Arms and legs use nested upper/lower groups with explicit shoulder, elbow, hip, knee, and ankle pivots. Feet remain nested at their ankle joints and counter-rotate so the soles stay level.

`wwwroot/js/gramophoneEntranceRig.js` is a pure sampled pose model. It solves reachable two-link limbs, clamps unreachable targets deterministically, and returns transforms without touching the DOM. `wwwroot/js/gameMotion.js` applies those samples through the Web Animations API, owns sequencing and cancellation, and keeps all learning and persistence rules in Blazor/.NET.

## Production entrance

The entrance is a one-shot sequence for a newly created challenge:

| Stage | Duration | Cumulative start |
|---|---:|---:|
| walk-in | 1800 ms | 0 ms |
| stop-and-settle | 250 ms | 1800 ms |
| sit-settle | 500 ms | 2050 ms |
| notice-pocket | 250 ms | 2550 ms |
| reach-pocket | 450 ms | 2800 ms |
| open-pocket | 250 ms | 3250 ms |
| grab-disc | 350 ms | 3500 ms |
| pull-disc | 550 ms | 3850 ms |
| hold-disc | 500 ms | 4400 ms |
| place-disc | 600 ms | 4900 ms |
| record-wobble-settle | 300 ms | 5500 ms |
| wake | 500 ms | 5800 ms |
| listen-lean | 420 ms | 6300 ms |

The walk covers roughly 1040 SVG units in four alternating strides. Each stance foot cancels the body's forward travel while the opposite foot lifts, producing planted contact rather than sliding. The body then lowers about 42 SVG units into the seated pose. The pocket flap opens, the hand reaches the record, and the record remains at a rigid hand-relative offset through pull and hold. The `place-disc` stage interpolates from that exact held matrix to the projected turntable matrix, avoiding a handoff snap. The question becomes active only after the final listen pose.

The fresh-session marker is consumed once. Refresh, resume, rerender, or a previously opened challenge does not replay the entrance.

## Motion lifecycle

Every animation belongs to a controller and channel. Replacement, navigation, disposal, hidden tabs, reduced-motion changes, root replacement, and explicit cancellation invalidate the current generation. Stale completion callbacks cannot commit styles or clear a newer animation. Pause and resume use Web Animations timing, and playback-rate changes preserve the current pose instead of restarting the stage.

The controller commits only the endpoint properties needed by an entrance stage, then cancels the finished Web Animation so no orphaned player remains. Ending or cancelling entrance clears temporary transforms, restores the playable record, hides the pocket record, and leaves the gramophone in one coherent ready state.

Reduced motion skips walking, limb choreography, and large rotations. It assembles the playable record immediately, then retains short state-oriented wake/listen feedback. Question readiness, focus, and progression behavior remain unchanged.

## Gameplay motion and audio

The entrance rig does not replace the existing semantic gameplay reactions: correct pulse, wrong recoil, challenge resolve, cycle hop, topic-proud hop, scratch approach/hit, repair heal/relief, and lesson bow still run on the shared artifact wrappers.

`gameAudio.js` returns Web Audio timestamps. `gameMotion.js` samples the same clock for record rotation, needle contact, scratch impact, and completion. Damaged phrases interrupt a sounding chord from 1.25 s to 2.35 s; the visible needle impulse lasts 260 ms. Clean completed/repaired phrases remain clean. Challenge resolutions use four equal 650 ms notes with no extended final note. Audio and animation cancellation share the same lifecycle boundaries but audio never determines correctness or progression.

## Motion Lab and verification

In Development, `/motion-lab` renders the real production SVG and rig. It can preview individual articulated stages or the complete entrance, change playback rate, pause, resume, cancel, reset, toggle reduced motion, and show sampled plus rendered joint diagnostics. The route is unavailable in Production.

Verification commands:

- `dotnet build ExamCompanion.sln --no-restore`
- `node tests/browser/entrance-rig.cjs`
- `node tests/browser/motion-lab.cjs`
- `node tests/browser/motion.cjs`
- `node tests/browser/visible-motion.cjs`
- `node tests/browser/production.cjs`
- `node tests/browser/audio.cjs`

Browser tests can target a non-default host through `EXAM_URL`; the Production test uses `EXAM_PRODUCTION_URL`. They independently measure rendered joints and foot planting at desktop and mobile widths, cover the full entrance order/timing, pause/rate/cancellation/replacement/reduced-motion paths, and verify the final playable composition.
