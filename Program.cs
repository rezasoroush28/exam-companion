using ChallengePrototype.Components;
using ChallengePrototype.Models;
using ChallengePrototype.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddSingleton<QuestionService>();
builder.Services.AddScoped<ChallengeEngine>();

var app = builder.Build();

if (args.Contains("--verify", StringComparer.OrdinalIgnoreCase))
{
    await using var scope = app.Services.CreateAsyncScope();
    var engine = scope.ServiceProvider.GetRequiredService<ChallengeEngine>();
    await engine.InitializeAsync(CancellationToken.None);
    Console.WriteLine($"VERIFY LESSON: {engine.LessonTitle}");
    foreach (var topic in engine.Topics)
        Console.WriteLine($"VERIFY TOPIC: {topic.Id} | {topic.Title} | importance={topic.Importance:0.######}");
    var questionCount = 0;
    var requestedLevels = new HashSet<string>();
    while (await engine.NextQuestionAsync() is { } question)
    {
        questionCount++;
        requestedLevels.Add(question.RequestedDifficulty.ToString());
        if (question.Options.Count != 4) throw new InvalidOperationException($"Question {question.Id} does not have four options.");
        engine.RecordAnswer();
    }
    if (engine.Topics.Count != 5 || questionCount != engine.TotalQuestions || questionCount <= 5 || requestedLevels.Count < 3)
        throw new InvalidOperationException($"Invalid fixed MVP pool: topics={engine.Topics.Count}, questions={questionCount}, levels={requestedLevels.Count}.");
    var bonusChecks = new[]
    {
        (new ResultCube(1, 1, 1, 1, GameDifficulty.Easy, true), 10),
        (new ResultCube(2, 2, 2, 3, GameDifficulty.Medium, true), 21),
        (new ResultCube(3, 3, 3, 4, GameDifficulty.Hard, true), 36),
        (new ResultCube(4, 4, 4, 5, GameDifficulty.VeryHard, true), 66)
    };
    foreach (var (cube, expected) in bonusChecks)
        if (BonusConfiguration.Calculate(cube) != expected)
            throw new InvalidOperationException($"Bonus formula mismatch for cube {cube.CubeId}.");
    Console.WriteLine($"VERIFY COMPLETE: questions={questionCount}, levels={string.Join(',', requestedLevels)}");
    return;
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
