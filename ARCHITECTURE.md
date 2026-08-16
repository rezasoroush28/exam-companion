# Exam Companion architecture

The solution uses four Clean Architecture projects.

| Project | Responsibility | May reference |
|---|---|---|
| `ExamCompanion.Domain` | Entities, enums, value objects, and stable game rules | Nothing |
| `ExamCompanion.Application` | Use cases, ports, health/progression engine, runtime blueprints | Domain |
| `ExamCompanion.Infrastructure` | EF Core, SQLite, migrations, question-bank and persistence adapters | Application, Domain |
| `ExamCompanion.Web` | Blazor UI, dependency composition, Matter.js and browser assets | Application, Infrastructure |

Dependencies point inward. Application code consumes `IQuestionService` and `ILevelDesignService`; it does not depend on EF Core or SQLite implementations.

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
