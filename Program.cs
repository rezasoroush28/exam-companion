using ChallengePrototype.Components;
using ChallengePrototype.Models;
using ChallengePrototype.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddSingleton<QuestionService>();
builder.Services.AddScoped<ChallengeEngine>();
builder.Services.AddSingleton(new HealthProgressionOptions());
builder.Services.AddSingleton<ChallengeHealthEngine>();

var app = builder.Build();

if (args.Contains("--verify", StringComparer.OrdinalIgnoreCase))
{
    await using var scope = app.Services.CreateAsyncScope();
    var engine = scope.ServiceProvider.GetRequiredService<ChallengeEngine>();
    var health = scope.ServiceProvider.GetRequiredService<ChallengeHealthEngine>();
    await engine.InitializeAsync(CancellationToken.None);
    Console.WriteLine($"VERIFY LESSON: {engine.LessonTitle}");
    foreach (var topic in engine.Topics)
        Console.WriteLine($"VERIFY TOPIC: {topic.Id} | {topic.Title} | importance={topic.Importance:0.######}");
    var targetMatrix = new[] { new[] { 1, 1, 2, 2, 3 }, new[] { 1, 2, 2, 2, 3 }, new[] { 2, 2, 2, 3, 3 }, new[] { 2, 2, 3, 3, 3 } };
    foreach (var level in Enum.GetValues<GameDifficulty>())
    {
        for (var tier = 1; tier <= 5; tier++)
            if (health.GetTargetQuestions(level, tier) != targetMatrix[(int)level][tier - 1])
                throw new InvalidOperationException($"Target matrix mismatch: {level}, tier {tier}.");
        engine.StartLevel(level);
        var gainTotal = engine.TopicProgress.Sum(x => x.TargetQuestionCount * health.GetCorrectGain(level,
            BonusConfiguration.ImportanceTier(x.Topic.Importance), engine.TotalGainWeight));
        if (Math.Abs(gainTotal - health.Options.PositiveHealthBudget[level]) > .000001)
            throw new InvalidOperationException($"Gain budget mismatch: {level}.");
        for (var index = 0; index < engine.LevelTargetCount; index++)
        {
            var question = await engine.NextQuestionAsync() ?? throw new InvalidOperationException("Schedule ended early.");
            if (question.Options.Count != 4) throw new InvalidOperationException($"Question {question.Id} does not have four options.");
            engine.RecordAnswer(question.Topic.Id, true);
        }
        if (!engine.IsEvidenceGateOpen || engine.TopicProgress.Any(x => !x.HasMinimumExposure))
            throw new InvalidOperationException($"Evidence gate failed for {level}.");
        if (await engine.NextQuestionAsync() is null) throw new InvalidOperationException("Adaptive continuation failed.");
    }
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
    if (Math.Abs(health.GetWrongDamage(GameDifficulty.Easy, 5) - 21) > .000001 ||
        Math.Abs(health.GetWrongDamage(GameDifficulty.VeryHard, 5) - 11.25) > .000001 ||
        health.GetWrongDamage(GameDifficulty.Easy, 5) <= health.GetWrongDamage(GameDifficulty.Easy, 1))
        throw new InvalidOperationException("Wrong-damage balance failed.");
    if (health.GetTimeLoss(TimeSpan.FromSeconds(2), GameDifficulty.Easy, 5) != 0)
        throw new InvalidOperationException("Health grace period failed.");
    var oneSecondLoss = health.GetTimeLoss(TimeSpan.FromSeconds(3), GameDifficulty.Easy, 5);
    var twoSecondLoss = health.GetTimeLoss(TimeSpan.FromSeconds(4), GameDifficulty.Easy, 5);
    if (Math.Abs(twoSecondLoss - oneSecondLoss * 2) > .000001 || health.Clamp(101, true) != 100 || health.Clamp(101, false) != 99 || health.Clamp(-1, true) != 0)
        throw new InvalidOperationException("Health linear decay or clamping failed.");
    var gated = health.ApplyAnswer(98, TimeSpan.Zero, GameDifficulty.Easy, 5, true, 1, false);
    var promoted = health.ApplyAnswer(98, TimeSpan.Zero, GameDifficulty.Easy, 5, true, 1, true);
    if (gated.NewHealth != 99 || promoted.NewHealth != 100) throw new InvalidOperationException("Promotion health cap failed.");
    engine.StartLevel(GameDifficulty.Easy);
    if (!engine.AdvanceLevel() || engine.CurrentDifficulty != GameDifficulty.Medium || !engine.AdvanceLevel() ||
        engine.CurrentDifficulty != GameDifficulty.Hard || !engine.AdvanceLevel() || engine.CurrentDifficulty != GameDifficulty.VeryHard || engine.AdvanceLevel())
        throw new InvalidOperationException("Hardness progression failed.");
    Console.WriteLine("VERIFY COMPLETE: progressive health, allocation, evidence, overtime and progression passed");
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
