# Guitar learning game

Run from this directory: `dotnet run --project PrototypeMain.csproj --urls http://127.0.0.1:5197`.

The served game is in `wwwroot/`. The pre-existing root HTML/JS files are preserved as reference; open the HTTP address to use the actual game and API.

This is a separate ASP.NET Core host with vanilla HTML/CSS/JS and the articulated guitar SVG. It references the existing Domain, Application and Infrastructure projects. The same MusicGameEngine owns questions, correctness, capped reinforcement, cycles, scratches, repair, unlock order and concurrency. JSON endpoints expose lesson, start/resume, answers and repairs; no learning decisions live in JavaScript.

First startup migrates a separate EF Core database, `data/guitar-game.db`, and imports only catalog entities from `../data/exam-companion.db` opened read-only. It never copies or clears main-game sessions. Subsequent startup preserves progress. The parent source projects and catalog are required; this folder is not a standalone distribution.

The five topic orders use Cmaj7, Dm7, Em7, Fmaj7 and G7. Correct educational answers reveal successive notes and trigger short picking. Correct challenge answers play all four chord notes together with tiny strum offsets and a longer performance. Topic tiles use server progress. Feedback stays until Continue and survives refresh in the same tab; server state remains authoritative. New album preserves previous sessions. Completed scratched topics support the existing repair flow.

Every page load and successful restart begins with the silent entrance inside the exact same stage used for gameplay. The stage size, SVG scale, background, and position remain unchanged as the game appears. Questions and controls stay hidden and inert until the 4.8-second arrival completes; catalog/session loading happens in parallel. The requested entrance also plays with reduced motion enabled. Hiding the tab pauses the entrance and returning resumes it. Audio begins from a user gesture; mute retains note/chord gestures. Mobile uses a compact character above the question, large answer targets, safe-area-aware submit button and 360px minimum layout. Desktop uses two columns. Hidden tabs stop musical playback, and page disposal stops all audio and motion.

Run browser coverage against the server with `node tests/game.cjs`, setting `PLAYWRIGHT_PATH` to an installed Playwright package. The test removes only the session it creates. Requires Node with node:sqlite and Edge. No frontend dependency installation is needed to play.

Development answer keys are returned only when ASPNETCORE_ENVIRONMENT=Development. Default Production responses omit them. This local single-user prototype has no authentication and should not be exposed as a multi-user service. Existing SQLitePCLRaw.lib.e_sqlite3 2.1.11 NU1903 warning remains inherited from Infrastructure.
