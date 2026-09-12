# Exam Companion architecture

The solution uses four Clean Architecture projects.

| Project | Responsibility | May reference |
|---|---|---|
| `ExamCompanion.Domain` | Entities, enums, value objects, and stable game rules | Nothing |
| `ExamCompanion.Application` | Use cases, ports, health/progression engine, runtime blueprints | Domain |
| `ExamCompanion.Infrastructure` | EF Core, SQLite, migrations, question-bank and persistence adapters | Application, Domain |
| `ExamCompanion.Web` | Blazor UI, dependency composition, Matter.js and browser assets | Application, Infrastructure |

Dependencies point inward. Application code consumes ports such as `IQuestionService`, `ILevelDesignService`,
`IRecoveryStore`, and `IEducationalQuestionSource`; it does not depend on EF Core or SQLite implementations.

## Visual Studio

Open `ExamCompanion.sln`, select `ExamCompanion.Web` as the startup project, and run the `http` profile.

## Commands

```powershell
dotnet build ExamCompanion.sln
dotnet run --project ExamCompanion.Web.csproj
dotnet run --project ExamCompanion.Web.csproj -- --verify
```

Create a Code First migration in the Infrastructure project:

```powershell
dotnet tool run dotnet-ef migrations add MigrationName `
  --project src\ExamCompanion.Infrastructure\ExamCompanion.Infrastructure.csproj `
  --startup-project ExamCompanion.Web.csproj `
  --context ChallengeDbContext `
  --output-dir Persistence\Migrations
```

The external question-bank SQLite files are read-only reference sources. Application-owned challenge and progress data is managed through `ChallengeDbContext` and EF Core migrations.

## Recovery mini-game

Recovery suggestions snapshot up to three eligible `TopicProgress` rows ordered by lowest average bonus yield.
Study claims and recovery sessions are persisted in their own EF aggregates. `RecoveryQuestionAttempt` never updates
the assessment-side `TopicProgress`, `QuestionAttempt`, bonus, health, or coin data. Educational questions are read
through `IEducationalQuestionSource` from the read-only `TopicEducationalQuestions.sqlite` database.

The Application layer owns ranking, correct-answer thresholds, stability, difficulty selection, and completion.
Blazor renders the workflow at `/recovery/{lessonChallengeId}`. `recoverySpace.js` owns only continuous canvas
animation, generated audio, active-time measurement, and discrete hint notifications; it performs no persistence.

## Generated health economy

Each `ChallengeLevelProgress` owns one versioned `ChallengeHealthPattern`. Application code calculates it only after the level blueprint has finalized topic quotas, target question count, and actual difficulty quotas. Infrastructure persists the pattern and its four normalized difficulty factors; retries load the same row instead of recalculating it.

`ChallengeHealthEngine` applies the persisted pattern to each actual question difficulty and the question topic's importance relative to the lesson's average importance. Blazor supplies active elapsed time and renders the result, but it does not own authoritative health formulas. Existing `QuestionAttempt` health deltas remain historical records and are not rewritten when patterns are introduced or versioned.
