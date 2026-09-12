# MASTER BUILD PROMPT FOR ASTRA
## Exam Companion — Fully Functional Blazor/.NET 8 Game MVP

You are acting as a **senior .NET 8 architect, Blazor engineer, EF Core engineer, frontend game-UI engineer, interaction designer, and game mechanic designer**.

Your task is to build a **complete, runnable MVP game** called **Exam Companion**.

This is not a design mockup.
This is not a Figma task.
This is not a static prototype.
This is not a backend-only exercise.

You must build a **working end-to-end application** with:

- .NET 8
- Blazor Web App
- Interactive Server rendering for the game experience
- EF Core 8
- SQLite for the MVP database
- migrations
- seeded sample data
- application/domain services
- persistent game sessions
- working question flow
- working topic/cycle progression
- working music-disc visualization
- working sound effects/music fragments
- working wrong-answer reinforcement
- working scratch/imperfect state
- working repair state
- polished cartoon/game-like UI
- responsive desktop-first layout
- clean project structure
- README with exact run instructions

The application must compile and run.

Do not stop at architecture, pseudocode, empty interfaces, TODO comments, or mock screenshots.

---

# 1. PRODUCT IDEA

Exam Companion is a small educational game for students preparing for an exam.

The player selects an exam/lesson and starts a learning game.

The game runs through Topics sequentially.

Each Topic is represented visually as a **ring on a musical disc**.

The Topic contains one or more learning cycles.

The player answers:

`Educational Questions -> Challenge Question`

Successful progress fills the Topic ring and builds its musical phrase.

Struggle creates a subtle visible/audio **scratch**.

The player can later repair scratched Topics.

The main motivation is:

> Complete the musical disc and make it sound clean.

The music disc is the central game object.

Do not turn this into a generic LMS dashboard.

---

# 2. MVP GOAL

For now, keep the game easy and immediately playable.

The desired user experience:

1. Open app.
2. See a playful home screen.
3. Click **Start Game**.
4. A large game panel / overlay appears.
5. Choose a sample Lesson.
6. See the Lesson music disc.
7. Click **Begin**.
8. Questions appear one after another.
9. Correct answers add musical/visual progress.
10. Wrong answers trigger reinforcement.
11. Challenge questions resolve cycle segments.
12. Topic ring completes.
13. Next Topic begins.
14. At the end, the Lesson disc is complete.
15. Scratched Topics can be replayed/repaired.

The MVP should be understandable within 10 seconds.

---

# 3. ART DIRECTION

The visual style is **cartoony, playful, expressive, polished, and game-like**.

Do not make it corporate.

Do not make it look like:
- admin dashboard
- school portal
- SaaS app
- Bootstrap demo
- MudBlazor default theme
- generic cards everywhere
- old-fashioned vinyl-player simulation

The game can use:

- rounded chunky shapes
- expressive outlines
- soft shadows
- vibrant but controlled colors
- subtle gradients
- friendly cartoon typography
- animated stars/sparkles
- playful progress effects
- oversized buttons
- floating UI details
- stylized concentric music disc
- reactive glow and bounce
- smooth transitions

The target is a modern indie educational game.

The music disc should feel like a magical/cartoon artifact, not a literal real-world vinyl record.

---

# 4. RECOMMENDED FRONTEND TECH

Use tools that work well with Blazor and do not overcomplicate the project.

Preferred stack:

- Blazor Web App (.NET 8)
- Interactive Server components
- Custom CSS
- SVG for the music disc
- CSS transitions / keyframes for most animation
- small JavaScript interop layer where necessary
- Web Audio API for generated tones/chords
- optional Howler.js only if it materially simplifies playback
- no heavy JavaScript SPA framework
- no React
- no Vue
- no Angular

Prefer native browser capabilities.

Use SVG for:
- concentric Topic rings
- ring progress arcs
- scratches
- glow
- active indicators

Use CSS for:
- button feedback
- answer transitions
- modal/game overlay
- shake/bounce
- success pulse
- topic-completion animation

Use Web Audio API to generate simple original tones programmatically.

Do not depend on copyrighted music assets.

---

# 5. SOLUTION STRUCTURE

Create a solution named:

`ExamCompanion`

Use this structure:

```text
ExamCompanion.sln

src/
  ExamCompanion.Domain/
  ExamCompanion.Application/
  ExamCompanion.Infrastructure/
  ExamCompanion.Web/

tests/
  ExamCompanion.Application.Tests/
```

Responsibilities:

## Domain
Entities, enums, pure domain rules.

## Application
Game engine, commands/services, DTOs, use cases.

## Infrastructure
EF Core DbContext, migrations, repositories, seed data.

## Web
Blazor pages/components, dependency injection, JS interop, CSS, static assets.

Do not create unnecessary abstraction layers.

Use interfaces only where they provide clear value.

---

# 6. DATABASE

Use:

`Microsoft.EntityFrameworkCore.Sqlite`

Database file:

`exam-companion.db`

On startup in Development:

- apply pending migrations
- seed sample data if empty

Do not use EnsureCreated if migrations exist.

---

# 7. CORE ENTITIES

Implement at least these entities.

## Exam

```csharp
Guid Id
string Name
DateTime ExamDate
```

## Lesson

```csharp
Guid Id
string Name
string Slug
```

## Topic

```csharp
Guid Id
Guid LessonId
Lesson Lesson

string Name
string Description

double Importance // 0..1

int DisplayOrder
string MusicalKey
string MusicalColor
```

## Question

```csharp
Guid Id
Guid TopicId
Topic Topic

QuestionType Type
string Text

string OptionA
string OptionB
string? OptionC
string? OptionD

string CorrectOption
int Difficulty
string? Explanation
```

QuestionType:

```csharp
Educational
Challenge
```

## GameSession

```csharp
Guid Id
Guid LessonId
Lesson Lesson

DateTime StartedAt
DateTime? CompletedAt

Guid CurrentTopicId
int CurrentCycleIndex
int CurrentQuestionIndex

GameSessionStatus Status
```

## TopicProgress

```csharp
Guid Id
Guid GameSessionId
Guid TopicId

int RequiredCycles
int CompletedCycles

TopicProgressStatus Status

int TotalWrongAnswers
int ReinforcementCount

bool HasScratch
bool IsRepaired

DateTime? CompletedAt
```

## QuestionAttempt

```csharp
Guid Id
Guid GameSessionId
Guid TopicId
Guid QuestionId

bool IsCorrect
DateTime AnsweredAt
int CycleIndex
bool WasReinforcement
```

---

# 8. ENUMS

Create clean enums for:

```csharp
QuestionType
GameSessionStatus
TopicProgressStatus
GameStepType
AnswerResultType
```

Suggested TopicProgressStatus:

```text
Locked
Available
Active
InProgress
CompletedClean
CompletedWithScratch
Repaired
```

---

# 9. IMPORTANCE -> NUMBER OF CYCLES

Use a simple deterministic MVP rule.

```text
Importance < 0.34      => 1 cycle
0.34 <= Importance < .67 => 2 cycles
Importance >= 0.67     => 3 cycles
```

Keep the rule in one domain/application service.

Do not duplicate it in UI code.

---

# 10. BASE GAME CYCLE

Each cycle begins as:

```text
Educational
Educational
Challenge
```

That is:

`2 Educational + 1 Challenge`

The number of cycles depends on Topic Importance.

Example:

Importance .2:

```text
E E C
```

Importance .5:

```text
E E C
E E C
```

Importance .9:

```text
E E C
E E C
E E C
```

---

# 11. EDUCATIONAL WRONG-ANSWER RULE

Wrong educational answers are not hard failures.

Normal cycle:

```text
2 Educational Questions
```

If one is answered incorrectly, add reinforcement.

Maximum educational count before the Challenge:

```text
4
```

Example:

```text
E correct
E wrong
Reinforcement E
Challenge
```

If still struggling:

```text
E wrong
E wrong
Reinforcement E
Reinforcement E
Challenge
```

Never exceed 4 educational questions in one cycle.

The reinforcement question should:
- come from the same Topic
- preferably have equal or lower Difficulty
- not immediately repeat the exact same question if alternatives exist

---

# 12. CHALLENGE RULE

After educational/reinforcement questions, show a Challenge Question.

If Challenge is correct:

- cycle completes
- Topic ring progress increases
- play musical resolution
- move to next cycle or Topic

If Challenge is wrong:

Do NOT restart the Topic.

Perform:

```text
1 reinforcement Educational question
-> new Challenge question
```

Maximum Challenge attempts per cycle:

```text
2
```

If the second Challenge is also incorrect:

- allow the cycle to close
- mark cycle/topic as imperfect
- increment struggle score
- Topic may eventually become `CompletedWithScratch`

This prevents endless frustration.

---

# 13. SCRATCH RULE

A Topic receives a scratch if any of these are true:

- second Challenge attempt failed in any cycle
- total wrong answers for Topic >= 3
- reinforcement count >= 3

Keep the thresholds in one configurable game rules class.

Example:

```csharp
GameRulesOptions
{
    BaseEducationalQuestionCount = 2,
    MaxEducationalQuestionCount = 4,
    MaxChallengeAttempts = 2,
    WrongAnswersForScratch = 3,
    ReinforcementsForScratch = 3
}
```

Register through options/configuration.

---

# 14. TOPIC COMPLETION

When all required cycles finish:

If no scratch condition:

```text
CompletedClean
```

If scratch condition:

```text
CompletedWithScratch
```

Then unlock the next Topic.

The player cannot skip unfinished Topics.

---

# 15. REPAIR MODE

After a Topic has `CompletedWithScratch`, allow player to click the scratched ring.

Show button:

`Repair Topic`

Repair flow:

```text
1 Educational
1 Challenge
```

If both are correct:

```text
CompletedWithScratch -> Repaired
HasScratch = false
IsRepaired = true
```

Play a satisfying clean musical phrase.

If repair fails:

keep the scratch and allow retry later.

---

# 16. GAME ENGINE

Create a central service such as:

```csharp
IGameEngine
GameEngine
```

The UI must NOT contain business rules.

The engine should expose operations similar to:

```csharp
Task<GameStateDto> StartSessionAsync(Guid lessonId)
Task<GameStateDto> GetSessionAsync(Guid sessionId)
Task<AnswerResultDto> SubmitAnswerAsync(
    Guid sessionId,
    Guid questionId,
    string selectedOption)

Task<GameStateDto> StartRepairAsync(
    Guid sessionId,
    Guid topicId)
```

The engine is responsible for:

- selecting current Topic
- determining required cycle count
- selecting questions
- avoiding immediate duplicates
- reinforcement
- Challenge retry
- Topic completion
- scratches
- repair
- session completion

---

# 17. QUESTION SELECTION

For MVP:

Select questions randomly from the Topic.

Rules:

Educational:
- QuestionType.Educational

Challenge:
- QuestionType.Challenge

Prefer unused questions in current Topic/session.

If all are exhausted:
- allow reuse
- avoid repeating the immediately previous Question

Use a small service:

```csharp
IQuestionSelector
```

Do not overengineer adaptive-learning algorithms yet.

---

# 18. SAMPLE DATA

Seed at least:

## Exam

`Sample Biology Exam`

## Lesson

`Biology 1`

## Topics

Create at least 5:

1. Cell Structure
   Importance: .25

2. Digestion
   Importance: .45

3. Genetics
   Importance: .80

4. Plant Biology
   Importance: .60

5. Nervous System
   Importance: .90

For every Topic:

- at least 8 Educational Questions
- at least 5 Challenge Questions

Questions may be simple fictional/demo biology questions.

Use clear sample content.

Do not rely on external APIs.

---

# 19. HOME SCREEN

Route:

`/`

Create a real game landing screen.

Show:

- title: Exam Companion
- cartoon/game logo treatment
- small subtitle
- current sample exam
- large `Start Game` button
- subtle animated decorative elements
- simple lesson selector

Clicking Start Game should open/start the game.

---

# 20. GAME SCREEN

Route can be:

`/game/{sessionId}`

or a large overlay from the landing screen.

The main game screen must include:

- large music disc
- concentric Topic rings
- current Lesson
- current Topic
- current cycle state
- Start / Continue button
- question panel
- progress feedback
- sound toggle
- exit/back button

The disc must be visually dominant.

---

# 21. MUSIC DISC COMPONENT

Create reusable component:

```text
Components/Game/MusicDisc.razor
```

Inputs should include:

- Topics
- CurrentTopicId
- progress state
- playing topic
- scratch state

Use SVG.

For each Topic, create one concentric ring.

Possible SVG approach:

```text
<circle>
stroke
stroke-dasharray
stroke-dashoffset
```

Use ring stroke-dashoffset to represent completion percentage.

Ring styling by state:

Locked:
- low opacity
- dotted/broken-looking neutral groove

Available:
- clearer neutral color

Active:
- bright glow
- animated pulse

InProgress:
- partially filled arc

CompletedClean:
- stable bright ring

CompletedWithScratch:
- full ring with an irregular interrupting scratch overlay

Playing:
- soft rotation/glow

Repaired:
- polished clean ring + small sparkle cue

Do not use simple flat progress bars as the primary visualization.

---

# 22. SCRATCH VISUAL

Create a small SVG scratch effect.

It can be:
- jagged short radial path
- broken overlay segment
- two or three tiny noise marks

It should be visible but attractive.

Hover/click scratched ring:

Show tooltip:

`Needs reinforcement`

Allow Repair button after Topic is complete.

---

# 23. QUESTION PANEL

Create:

```text
QuestionPanel.razor
AnswerOption.razor
ChallengeBadge.razor
```

Educational question:

- 2 options
- friendly card
- lighter visual pressure
- large readable text

Challenge:

- 4 options
- stronger frame
- visually distinct checkpoint
- optional small animated icon
- no stressful countdown required for MVP

After selecting:

- lock answer buttons briefly
- show feedback
- play sound
- animate disc
- automatically move to next state after ~700–1200ms
- provide a Continue button if automatic transition becomes awkward

Choose whichever feels better in the game.

---

# 24. AUDIO SYSTEM

Create JS file:

```text
wwwroot/js/gameAudio.js
```

Use Web Audio API.

Expose via JS interop functions such as:

```javascript
playEducationalCorrect(topicIndex)
playEducationalWrong(topicIndex)
playChallengeSuccess(topicIndex)
playChallengeWrong(topicIndex)
playTopicComplete(topicIndex)
playScratch(topicIndex)
playRepair(topicIndex)
playLessonComplete()
```

Generate tones programmatically.

No external audio asset is required for MVP.

Use different base pitches per Topic.

Example tonal mapping:

Topic 1:
C major family

Topic 2:
D minor family

Topic 3:
E minor family

Topic 4:
F major family

Topic 5:
G major family

Correct Educational:
- single bright note

Educational Wrong:
- short unresolved/detuned note

Challenge Success:
- 2–3 note chord

Topic Complete:
- short melodic phrase

Scratch:
- chord plus subtle filtered noise

Repair:
- clean resolved chord

Lesson Complete:
- short combined phrase

Keep volume gentle.

Add sound on/off toggle.

---

# 25. GAME FEEL

Add subtle game feel:

Correct:
- answer button settles/glows
- tiny star particles or sparkles
- ring progress animates
- tone plays

Wrong:
- very small shake
- soft visual wobble
- no aggressive red flash
- unresolved tone

Challenge Success:
- ring pulse
- segment locks
- chord plays
- small burst

Topic Complete:
- ring completes
- glow travels around ring
- topic label pops in
- phrase plays

Scratch:
- small crack-like animation
- noise artifact
- text such as:
  `Completed — needs a little polish`

Repair:
- scratch disappears
- ring shines
- clean chord

Lesson Complete:
- disc briefly spins/glows
- all rings light
- final phrase plays
- completion panel

---

# 26. CARTOON STYLE GUIDANCE

Use a cohesive custom visual language.

Example aesthetic:

- deep navy/purple background
- warm cream text
- cyan/lime/coral/yellow accents
- chunky rounded buttons
- playful outlined icons
- soft inner shadows
- exaggerated but tasteful ring glow
- cartoon sparkles
- stylized labels

Do not randomly mix styles.

Create CSS variables.

Example:

```css
:root {
  --bg: ...;
  --panel: ...;
  --text: ...;
  --muted: ...;
  --accent-1: ...;
  --accent-2: ...;
  --success: ...;
  --scratch: ...;
}
```

Choose the actual palette yourself.

---

# 27. RESPONSIVENESS

Desktop first.

Target:

`1440 × 900`

Must still be usable at common laptop widths such as:

`1280 × 720`

On narrower screens:
- scale disc down
- stack question panel under/over disc if necessary

Do not invest time in a mobile-specific redesign yet.

---

# 28. STATE MANAGEMENT

For MVP:

The persistent authoritative state is the database.

In UI:
- load GameStateDto
- submit answer
- receive new state
- re-render

Do not create complicated client state libraries.

Use scoped services appropriately.

---

# 29. EF CORE CONFIGURATION

Use IEntityTypeConfiguration classes.

Configure:
- primary keys
- required fields
- relationships
- indexes where useful

Add migrations.

Seed through Infrastructure initialization code.

Avoid putting giant HasData blobs in DbContext if cumbersome.

A dedicated development seed service is acceptable.

---

# 30. DATABASE INITIALIZATION

At startup in Development:

```text
Database.MigrateAsync()
SeedAsync()
```

Use a scoped initialization routine.

---

# 31. VALIDATION / ERROR HANDLING

Handle:

- invalid session id
- invalid lesson id
- submitting a Question that is not current
- duplicate submit
- invalid option
- completed session
- missing questions

Do not crash.

Return safe UI states.

---

# 32. LOGGING

Use ILogger in GameEngine.

Log:
- session start
- Topic start
- cycle completion
- Topic completion
- scratch creation
- repair success
- Lesson completion

Do not overlog every render.

---

# 33. TESTS

Create Application tests for at least:

1. Importance .2 -> 1 cycle
2. Importance .5 -> 2 cycles
3. Importance .9 -> 3 cycles
4. educational wrong adds reinforcement
5. educational count never exceeds max
6. Challenge success completes cycle
7. second Challenge failure can close cycle with scratch
8. Topic cannot advance before completion
9. repair success clears scratch
10. final Topic completion completes GameSession

Use a simple fake/in-memory repository or SQLite in-memory.

Do not write fragile UI tests for MVP.

---

# 34. ROUTES

At minimum:

```text
/
 /game/{sessionId:guid}
```

Optional:

```text
/results/{sessionId:guid}
```

---

# 35. RESULT SCREEN

When Lesson is completed:

Show:

- full music disc
- all Topic states
- number clean
- number scratched
- Repair button for scratched Topics
- Play Disc button
- Restart Demo button

Keep it game-like.

Do not display a spreadsheet of metrics.

---

# 36. ACCESSIBILITY

Implement basic accessibility:

- keyboard-focusable answers
- visible focus states
- semantic buttons
- aria labels for disc rings
- do not rely on color alone
- reasonable text contrast
- sound toggle
- avoid rapid flashing

---

# 37. PERFORMANCE

Do not over-optimize.

But:
- use SVG efficiently
- avoid hundreds of DOM nodes per ring
- avoid unnecessary component re-renders
- keep JS interop small
- preload nothing heavy
- no large frontend libraries without a strong reason

---

# 38. NO EXTERNAL DEPENDENCIES WITHOUT NEED

Avoid adding:
- Bootstrap UI kits
- MudBlazor
- Radzen
- Syncfusion
- Telerik
- React wrappers
- heavy game engines

unless absolutely necessary.

The artistic UI should be custom.

The application is simple enough for:
- Blazor
- SVG
- CSS
- Web Audio API

---

# 39. FILE / COMPONENT SUGGESTION

A good Web project organization:

```text
ExamCompanion.Web/
  Components/
    Layout/
    Game/
      MusicDisc.razor
      MusicDiscRing.razor
      QuestionPanel.razor
      AnswerOption.razor
      TopicStatusPill.razor
      GameHud.razor
      GameCompletionPanel.razor

  Pages/
    Home.razor
    Game.razor

  Services/
    GameAudioService.cs

  wwwroot/
    css/
      app.css
      game.css
    js/
      gameAudio.js
```

Adjust if needed.

---

# 40. DTO SUGGESTION

Create DTOs such as:

```csharp
GameStateDto
TopicStateDto
CurrentQuestionDto
AnswerResultDto
```

GameStateDto should contain everything needed by UI:

```text
SessionId
LessonName
Status
CurrentTopicId
CurrentTopicName
CurrentCycle
RequiredCycles
CurrentStepType
CurrentQuestion
Topics[]
CanRepair
IsComplete
```

TopicStateDto:

```text
TopicId
Name
DisplayOrder
Importance
RequiredCycles
CompletedCycles
Status
HasScratch
IsRepaired
ProgressPercent
```

---

# 41. NO PLACEHOLDERS

Do not leave:

```text
TODO
Coming soon
implement later
fake click
dummy method
NotImplementedException
```

If a feature is listed in this MVP prompt, implement it.

---

# 42. BUILD QUALITY

Use:
- nullable reference types
- async/await
- cancellation tokens where appropriate
- clear naming
- small methods
- DI
- options pattern for game rules
- clean EF Core usage

Avoid:
- giant God components
- business logic in Razor markup
- static mutable game state
- excessive repository abstraction
- premature CQRS complexity

This is an MVP, not an enterprise framework exercise.

---

# 43. README

Create a README containing:

## Requirements
- .NET 8 SDK

## Run

```bash
dotnet restore
dotnet build
dotnet run --project src/ExamCompanion.Web
```

Explain:
- where SQLite file is stored
- that migrations run automatically in Development
- sample data is seeded automatically
- how to reset database

Also describe the game loop briefly.

---

# 44. DEFINITION OF DONE

The task is DONE only when:

- solution builds
- app launches
- landing page renders
- Start Game works
- sample Lesson loads
- music disc renders
- Topic rings reflect state
- educational questions work
- Challenge questions work
- wrong answers create reinforcement
- Topic cycles advance correctly
- scratch state can happen
- Topic completion works
- next Topic unlocks
- session completes
- repair flow works
- audio feedback works
- sound toggle works
- result screen works
- database persists progress
- tests pass
- README is complete

---

# 45. EXECUTION PROCESS

Work in this order:

## Phase 1 — Foundation
- create solution/projects
- references
- EF packages
- entities
- DbContext
- migration
- seed

## Phase 2 — Game Engine
- rules
- selector
- GameEngine
- DTOs
- tests

## Phase 3 — Basic Blazor Flow
- home
- session creation
- game route
- question submit
- completion

## Phase 4 — Game Art
- custom cartoon CSS
- disc SVG
- ring states
- question visuals
- animations

## Phase 5 — Audio
- Web Audio API
- JS interop
- note/chord feedback
- sound toggle

## Phase 6 — Scratch / Repair
- scratch state
- visual defect
- audio defect
- repair flow

## Phase 7 — Polish
- transitions
- completion screen
- accessibility
- error states

## Phase 8 — Verification
Run:

```bash
dotnet restore
dotnet build
dotnet test
```

Fix all build/test errors.

Then run the application and verify the complete happy path manually.

---

# 46. IMPORTANT DESIGN RULE

The Music Disc must NOT be a decorative progress chart.

It must function as the game object.

The user should see their learning transform the disc.

Each action should have a visible and/or audible consequence.

The app should feel like:

> "I am building and cleaning a musical artifact by learning."

Not:

> "I am answering questions while looking at a progress circle."

---

# 47. KEEP THE MVP EASY

Do not add:
- authentication
- roles
- admin panel
- payments
- real exam integrations
- external question bank
- multiplayer
- inventory
- coins
- avatar system
- leaderboard
- streaks
- notifications
- mobile app
- complex adaptive AI

Use sample data.

The purpose is to prove:

**Game loop + music disc + visual feedback + scratch/repair + Blazor implementation.**

---

# 48. START NOW

Do not return only a plan.

Begin implementation.

Create the full solution.

Make reasonable technical decisions without repeatedly asking for confirmation.

When a decision is uncertain:
- prefer the simpler .NET 8 / Blazor-compatible approach
- keep the game functional
- preserve the agreed mechanic
- preserve the cartoon/game-art direction

At the end, provide:

1. concise architecture summary
2. important game rules implemented
3. project tree
4. commands to run
5. database/migration notes
6. tests executed and results
7. any minor limitations that genuinely remain

But first: build the working application.
