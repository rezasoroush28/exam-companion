# Music game architecture

Four Clean Architecture layers, preserving the existing Visual Studio solution:

| Layer | Active code | Responsibility |
|---|---|---|
| Domain | `src/ExamCompanion.Domain/Music` | Entities, states, default thresholds, importance-to-cycles rule |
| Application | `src/ExamCompanion.Application/Music` | Session lifecycle, question selection, educational/challenge transitions, scratch/repair, DTOs and `IGameStore` |
| Infrastructure | `src/ExamCompanion.Infrastructure/Music` | SQLite Code First mappings/migrations, tracked EF store, initial import from existing local banks |
| Web | root project, `Components`, `wwwroot` | Persian Blazor UI, SVG disc, Web Audio, dependency composition |

Application references Domain, not EF. Infrastructure implements the Application persistence port. Web references Application and Infrastructure at its composition root. No new game formulas live in JavaScript or Razor.

## An answer's path

1. `Game.razor` sends the session ID, current question ID, turn ID, and selected option to `IGameEngine`.
2. `MusicGameEngine` reloads the authoritative session through `IGameStore`, validates the current turn and available option, and checks the stored correct answer.
3. It records a `QuestionAttempt`, updates `TopicProgress` and the cycle state, selects the next question, and changes the concurrency revision.
4. `MusicGameStore` saves the tracked aggregate atomically. The database enforces one attempt per session/turn. Competing revisions fail safely.
5. The engine returns feedback and the new state. The disc immediately reflects it; Continue displays the next question. Audio provides optional feedback but owns no game state.

Every answer is saved before feedback is returned. Reload therefore resumes the next pending question, not an already answered turn. An interrupted repair also resumes. A repair stores the suspended main question and stage, restoring them exactly afterward with a fresh turn ID.

## Persistence and lifecycle

`IDbContextFactory<MusicDbContext>` supplies a fresh context for each engine operation; the loaded graph is tracked only through that operation's save. `DateTime` UTC is used for sortable SQLite timestamps, avoiding the historical DateTimeOffset ordering issue. Entity GUIDs are application-generated and configured `ValueGeneratedNever` so newly appended attempts/progress are inserted reliably.

The source databases are read only by `MusicSeed` on the first Development startup. Its two existing question-source adapters are the only old services registered. The new catalog and all gameplay data live in `data/exam-companion.db`. Historical health, coin, falling-cube, and suggestion code/data is not used by the new game.

`Home.razor` starts a new session or opens the most recent one. `/game/{SessionId}` handles active learning, repair, and completed albums. There is no authentication: this is intentionally a single-user local MVP, not a multi-user hosted service.

For run commands, migration instructions, source topic IDs, game thresholds, verification, and limitations, see [README.md](README.md).
