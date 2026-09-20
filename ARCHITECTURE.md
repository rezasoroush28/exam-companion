# Music game architecture

Four Clean Architecture layers, preserving the existing Visual Studio solution:

| Layer | Active code | Responsibility |
|---|---|---|
| Domain | `src/ExamCompanion.Domain/Music` | Entities, states, default thresholds, importance-to-cycles rule |
| Application | `src/ExamCompanion.Application/Music` | Session lifecycle, question selection, educational/challenge transitions, scratch/repair, DTOs and `IGameStore` |
| Infrastructure | `src/ExamCompanion.Infrastructure/Music` | SQLite Code First mappings/migrations, tracked EF store, initial import from existing local banks |
| Web | root project, `Components`, `wwwroot` | Persian Blazor UI, inline-SVG gramophone, synchronized Web Audio/motion, dependency composition |

Application references Domain, not EF. Infrastructure implements the Application persistence port. Web references Application and Infrastructure at its composition root. No new game formulas live in JavaScript or Razor.

## An answer's path

1. `Game.razor` sends the session ID, current question ID, turn ID, and selected option to `IGameEngine`.
2. `MusicGameEngine` reloads the authoritative session through `IGameStore`, validates the current turn and available option, and checks the stored correct answer.
3. It records a `QuestionAttempt`, updates `TopicProgress` and the cycle state, selects the next question, and changes the concurrency revision.
4. `MusicGameStore` saves the tracked aggregate atomically. The database enforces one attempt per session/turn. Competing revisions fail safely.
5. The engine returns feedback and the new state. The gramophone immediately reflects it; Continue displays the next question. Audio provides optional feedback but owns no game state.

Every answer is saved before feedback is returned. Reload therefore resumes the next pending question, not an already answered turn. An interrupted repair also resumes. A repair stores the suspended main question and stage, restoring them exactly afterward with a fresh turn ID.

## Presentation boundary

`Game.razor` coordinates persisted state, explicit Continue, repair entry, playback intent and semantic motion selection. `Gramophone.razor` is entirely inline SVG with a nested articulated rig: locomotion and machine roots, upper/lower limbs, level feet, pocket/flap, removable record, horn, turntable, and tone arm. `gramophoneEntranceRig.js` is a pure pose sampler and two-link solver; `gameMotion.js` is the only DOM animation executor. Motion reads SVG geometry and matrices to keep hands, feet, record, and needle aligned. `TopicSidebar`, `QuestionPanel`, `PlaybackWaveform`, and `GameIcon` handle the surrounding interface. Home uses the same gramophone in preview mode. Tokens and responsive shell styles are separate from component-isolated SVG styles.

The only supporting presentation contract is `TopicStateDto.NotesRevealed`, derived from correct non-repair educational attempts. No schema change or learning-rule change accompanies the redesign. Developer correct-answer keys are opt-in in Development; `Program.cs` forcibly disables the engine option in every other environment, and the UI has a second environment gate.

`gameAudio.js` returns a per-phrase schedule on the Web Audio clock. `gameMotion.js` uses that clock for record rotation, groove contact, needle movement, and waveform disturbance. The visual scratch angle and audible dropout use the same contact fraction. JavaScript owns no correctness, progression, repair, or persistence decisions. Its controller cancels oscillators and animation frames on replacement, navigation/disposal, and hidden tabs. Reduced motion preserves state feedback without continuous object motion.

The controller also owns cancellable Web Animations, the 13-stage articulated entrance, semantic reactions, and a sparse idle timer. The entrance walks through four alternating planted strides, sits, retrieves a pocket record with a rigid hand-relative grip, places it on the turntable, and reaches question-ready state. Generation tokens prevent stale completions from mutating replacements; pause/resume and playback-rate changes preserve current time; root replacement, hidden tabs, navigation, and disposal cleanly cancel the rig. The two-major-system budget pauses record spin during body/horn reactions. A one-shot sessionStorage marker stages entrance only for newly created sessions. OS and persisted in-app reduced-motion preferences are combined. Final bow follows completed-album cadence, never answer submission alone. See the [motion/artwork contract](docs/gramophone-art.md) for durations, mappings, lifecycle, and verification.

## Persistence and lifecycle

`IDbContextFactory<MusicDbContext>` supplies a fresh context for each engine operation; the loaded graph is tracked only through that operation's save. `DateTime` UTC is used for sortable SQLite timestamps, avoiding the historical DateTimeOffset ordering issue. Entity GUIDs are application-generated and configured `ValueGeneratedNever` so newly appended attempts/progress are inserted reliably.

The source databases are read only by `MusicSeed` on the first Development startup. Its two existing question-source adapters are the only old services registered. The new catalog and all gameplay data live in `data/exam-companion.db`. Historical health, coin, falling-cube, and suggestion code/data is not used by the new game.

`Home.razor` starts a new session or opens the most recent one. `/game/{SessionId}` handles active learning, repair, and completed albums. There is no authentication: this is intentionally a single-user local MVP, not a multi-user hosted service.

For run commands, migration instructions, source topic IDs, game thresholds, verification, and limitations, see [README.md](README.md).
