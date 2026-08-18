using ChallengePrototype.Components;
using ChallengePrototype.Models;
using ChallengePrototype.Services;
using ExamCompanion.Application;
using ExamCompanion.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();
await app.Services.MigrateInfrastructureAsync();

if (args.Contains("--verify", StringComparer.OrdinalIgnoreCase))
{
    await using var scope = app.Services.CreateAsyncScope();
    var engine = scope.ServiceProvider.GetRequiredService<ChallengeEngine>();
    var health = scope.ServiceProvider.GetRequiredService<ChallengeHealthEngine>();
    var healthPatterns = scope.ServiceProvider.GetRequiredService<IChallengeHealthPatternCalculator>();
    var levelDesigns = scope.ServiceProvider.GetRequiredService<ILevelDesignService>();
    var design = await levelDesigns.GetActiveDesignAsync();
    if (design.LevelRules.Count != 4 || design.LevelRules.OrderBy(x => x.ChallengeLevel).Select(x => x.ImportanceFocus)
        .SequenceEqual(design.LevelRules.OrderBy(x => x.ChallengeLevel).Select(x => x.ImportanceFocus).OrderBy(x => x)) is false)
        throw new InvalidOperationException("Default LevelDesign structure is invalid.");
    foreach (var rule in design.LevelRules)
    {
        var blueprint = levelDesigns.Generate(rule, Enumerable.Range(1, 5).Select(x => ((long)x, x)).ToArray());
        if (blueprint.Topics.Sum(x => x.QuestionQuota) != blueprint.TargetQuestionCount ||
            blueprint.DifficultyQuotas.Values.Sum() != blueprint.TargetQuestionCount ||
            blueprint.Topics.Any(x => x.QuestionQuota < rule.MinimumQuestionsPerTopic || x.QuestionQuota > rule.MaximumQuestionsPerTopic) ||
            blueprint.Topics.OrderBy(x => x.Importance).Select(x => x.QuestionQuota).SequenceEqual(
                blueprint.Topics.OrderBy(x => x.Importance).Select(x => x.QuestionQuota).OrderBy(x => x)) is false)
            throw new InvalidOperationException($"Generic blueprint allocation failed: {rule.ChallengeLevel}.");
        if (blueprint.TargetQuestionCount >= 4 && blueprint.DifficultyQuotas.Values.Any(x => x == 0))
            throw new InvalidOperationException($"Difficulty mixture failed: {rule.ChallengeLevel}.");
        var dominant = (QuestionDifficulty)(int)rule.ChallengeLevel;
        if (blueprint.DifficultyQuotas[dominant] != blueprint.DifficultyQuotas.Values.Max())
            throw new InvalidOperationException($"Dominant difficulty failed: {rule.ChallengeLevel}.");
    }
    var largeRule = design.LevelRules.Single(x => x.ChallengeLevel == ChallengeLevel.Easy);
    var segments = levelDesigns.GenerateSegments(largeRule, Enumerable.Range(1, 30).Select(x => ((long)x, (x % 5) + 1)).ToArray());
    if (segments.Count <= 1 || segments.SelectMany(x => x.Topics).Select(x => x.TopicId).Distinct().Count() != 30)
        throw new InvalidOperationException("Large lesson segmentation omitted topics.");
    await engine.InitializeAsync(CancellationToken.None);
    Console.WriteLine($"VERIFY LESSON: {engine.LessonTitle}");
    foreach (var topic in engine.Topics)
        Console.WriteLine($"VERIFY TOPIC: {topic.Id} | {topic.Title} | importance={topic.Importance:0.######}");
    foreach (var level in Enum.GetValues<GameDifficulty>())
    {
        engine.StartLevel(level);
        if (engine.CurrentHealthPattern.Id <= 0 || engine.CurrentHealthPattern.PatternVersion != 1 ||
            engine.CurrentHealthPattern.DifficultyFactors.Count != 4 ||
            engine.CurrentHealthPattern.TargetQuestionCount != engine.LevelTargetCount)
            throw new InvalidOperationException($"Persisted health pattern failed: {level}.");
        var allocation = engine.GetDifficultyAllocation(engine.LevelTargetCount);
        if (allocation.Values.Sum() != engine.LevelTargetCount || allocation.Values.Any(x => x == 0) ||
            allocation[level] != allocation.Values.Max())
            throw new InvalidOperationException($"Mixed difficulty allocation failed: {level}.");
        for (var index = 0; index < engine.LevelTargetCount; index++)
        {
            var question = await engine.NextQuestionAsync() ?? throw new InvalidOperationException("Schedule ended early.");
            if (question.Options.Count != 4) throw new InvalidOperationException($"Question {question.Id} does not have four options.");
            engine.RecordAnswer(question.Topic.Id, question.RequestedDifficulty, true);
        }
        if (engine.TopicProgress.Any(x => !x.HasMinimumExposure) || engine.QuestionsAnswered < engine.MinimumEvidenceRequired)
            throw new InvalidOperationException($"Topic evidence failed for {level}.");
        for (var overtime = 0; !engine.IsEvidenceGateOpen && overtime < 20; overtime++)
        {
            var question = await engine.NextQuestionAsync() ?? throw new InvalidOperationException("Adaptive continuation failed.");
            engine.RecordAnswer(question.Topic.Id, question.RequestedDifficulty, true);
        }
        if (!engine.IsEvidenceGateOpen) throw new InvalidOperationException($"Dominant difficulty evidence failed for {level}.");
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
    ChallengeHealthPattern CreatePattern(LevelRule rule, int targetCount, params int[] importances)
    {
        var topicRows = importances.Select((importance, index) => new TopicProgress
        {
            TopicId = index + 1,
            ImportanceSnapshot = importance
        }).ToArray();
        var baseQuota = targetCount / topicRows.Length;
        var remainder = targetCount % topicRows.Length;
        var blueprintTopics = topicRows.Select((topic, index) => new BlueprintTopic(topic.TopicId,
            topic.ImportanceSnapshot, baseQuota + (index < remainder ? 1 : 0))).ToArray();
        var blueprint = new LevelBlueprint(rule.ChallengeLevel, rule, blueprintTopics,
            levelDesigns.AllocateDifficulties(rule, targetCount));
        var progress = new ChallengeLevelProgress
        {
            ChallengeLevel = rule.ChallengeLevel,
            TargetQuestionCount = targetCount,
            StartingHealth = rule.StartingHealth
        };
        return healthPatterns.Calculate(progress, rule, blueprint, topicRows);
    }

    var patternsByLevel = design.LevelRules.ToDictionary(x => x.ChallengeLevel,
        x => CreatePattern(x, 20, 1, 2, 3, 4, 5));
    var fiveQuestionPattern = CreatePattern(largeRule, 5, 1, 2, 3, 4, 5);
    var thirtyQuestionPattern = CreatePattern(largeRule, 30, 1, 2, 3, 4, 5);
    if (Math.Abs(fiveQuestionPattern.HealthUnit - 10) > .000001 ||
        Math.Abs(thirtyQuestionPattern.HealthUnit - (50d / 30)) > .000001 ||
        fiveQuestionPattern.HealthUnit <= thirtyQuestionPattern.HealthUnit)
        throw new InvalidOperationException("HealthUnit does not scale with generated question count.");

    var zeroTargetRejected = false;
    try { _ = CreatePattern(largeRule, 0, 3); }
    catch (InvalidOperationException) { zeroTargetRejected = true; }
    var oneTopicPattern = CreatePattern(largeRule, 5, 4);
    var zeroSharePattern = CreatePattern(largeRule, 1, 3);
    if (!zeroTargetRejected || Math.Abs(health.GetCorrectTopicFactor(oneTopicPattern, 4) - 1) > .000001 ||
        Math.Abs(health.GetWrongTopicFactor(oneTopicPattern, 4) - 1) > .000001 ||
        Math.Abs(health.GetTimeTopicFactor(oneTopicPattern, 4) - 1) > .000001 ||
        zeroSharePattern.DifficultyFactors.Any(x => !double.IsFinite(x.CorrectMultiplier)
            || !double.IsFinite(x.WrongMultiplier) || !double.IsFinite(x.TimeMultiplier)))
        throw new InvalidOperationException("Health-pattern edge-case handling failed.");

    foreach (var (level, pattern) in patternsByLevel)
    {
        if (Math.Abs(pattern.HealthUnit - pattern.PromotionDistance / pattern.TargetQuestionCount) > .000001)
            throw new InvalidOperationException($"HealthUnit formula failed: {level}.");
        var expectedCorrectFactor = (1 + ((1 - pattern.TargetAccuracy) * pattern.WrongSeverity)
            + pattern.ExpectedTimeBudgetFraction) / pattern.TargetAccuracy;
        if (Math.Abs(pattern.BaseCorrectGain / pattern.HealthUnit - expectedCorrectFactor) > .000001 ||
            pattern.DifficultyFactors.Count != 4)
            throw new InvalidOperationException($"Health calibration failed: {level}.");

        var patternRule = design.LevelRules.Single(x => x.ChallengeLevel == level);
        var difficultyQuotas = levelDesigns.AllocateDifficulties(patternRule, pattern.TargetQuestionCount);
        double WeightedAverage(Func<ChallengeHealthDifficultyFactor, double> selector) =>
            pattern.DifficultyFactors.Sum(factor => selector(factor)
                * difficultyQuotas[factor.QuestionDifficulty] / (double)pattern.TargetQuestionCount);

        if (Math.Abs(WeightedAverage(x => x.CorrectMultiplier) - 1) > .000001 ||
            Math.Abs(WeightedAverage(x => x.WrongMultiplier) - 1) > .000001 ||
            Math.Abs(WeightedAverage(x => x.TimeMultiplier) - 1) > .000001)
            throw new InvalidOperationException($"Difficulty normalization failed: {level}.");
    }

    var healthPattern = patternsByLevel[ChallengeLevel.Hard];
    if (health.GetWrongDamage(healthPattern, GameDifficulty.Easy, 3)
            <= health.GetWrongDamage(healthPattern, GameDifficulty.VeryHard, 3) ||
        health.GetCorrectGain(healthPattern, GameDifficulty.VeryHard, 3)
            <= health.GetCorrectGain(healthPattern, GameDifficulty.Easy, 3))
        throw new InvalidOperationException("Actual difficulty health behavior failed.");

    var correctImportanceRatio = health.GetCorrectGain(healthPattern, GameDifficulty.Medium, 5)
        / health.GetCorrectGain(healthPattern, GameDifficulty.Medium, 1);
    var wrongImportanceRatio = health.GetWrongDamage(healthPattern, GameDifficulty.Medium, 5)
        / health.GetWrongDamage(healthPattern, GameDifficulty.Medium, 1);
    var timeImportanceRatio = health.GetQuestionTimeBudget(healthPattern, GameDifficulty.Medium, 5)
        / health.GetQuestionTimeBudget(healthPattern, GameDifficulty.Medium, 1);
    if (wrongImportanceRatio <= correctImportanceRatio || wrongImportanceRatio <= timeImportanceRatio)
        throw new InvalidOperationException("Relative topic-importance sensitivity failed.");

    var equalImportancePattern = CreatePattern(largeRule, 20, 5, 5, 5, 5, 5);
    if (Math.Abs(health.GetCorrectTopicFactor(equalImportancePattern, 5) - 1) > .000001 ||
        Math.Abs(health.GetWrongTopicFactor(equalImportancePattern, 5) - 1) > .000001 ||
        Math.Abs(health.GetTimeTopicFactor(equalImportancePattern, 5) - 1) > .000001)
        throw new InvalidOperationException("Equal topic importance was not neutral.");

    if (health.GetTimeLoss(healthPattern, TimeSpan.FromMilliseconds(3499), GameDifficulty.Hard, 3) != 0)
        throw new InvalidOperationException("Health grace period failed.");
    var oneSecondLoss = health.GetTimeLoss(healthPattern, TimeSpan.FromSeconds(4.5), GameDifficulty.Hard, 3);
    var twoSecondLoss = health.GetTimeLoss(healthPattern, TimeSpan.FromSeconds(5.5), GameDifficulty.Hard, 3);
    if (Math.Abs(twoSecondLoss - oneSecondLoss * 2) > .000001 ||
        health.Clamp(healthPattern, 101, true) != 100 || health.Clamp(healthPattern, 101, false) != 99 ||
        health.Clamp(healthPattern, -1, true) != 0)
        throw new InvalidOperationException("Health linear decay or clamping failed.");

    foreach (var difficulty in Enum.GetValues<GameDifficulty>())
    {
        var timedCorrect = health.ApplyAnswer(healthPattern, 50, TimeSpan.FromMinutes(30),
            difficulty, 1, true, true);
        if (timedCorrect.NewHealth < timedCorrect.PreviousHealth)
            throw new InvalidOperationException($"Correct answer reduced health: {difficulty}.");
    }
    var gated = health.ApplyAnswer(healthPattern, 98, TimeSpan.Zero, GameDifficulty.VeryHard, 5, true, false);
    var promoted = health.ApplyAnswer(healthPattern, 98, TimeSpan.Zero, GameDifficulty.VeryHard, 5, true, true);
    var depleted = health.ApplyAnswer(healthPattern, 1, TimeSpan.FromHours(1), GameDifficulty.Easy, 5, false, true);
    if (gated.NewHealth != 99 || promoted.NewHealth != 100 || !depleted.IsDepleted)
        throw new InvalidOperationException("Health caps or depletion flow failed.");

    engine.StartLevel(GameDifficulty.Easy);
    var persistedPatternId = engine.CurrentHealthPattern.Id;
    var persistedGeneratedAt = engine.CurrentHealthPattern.GeneratedAt;
    engine.StartLevel(GameDifficulty.Easy);
    if (persistedPatternId <= 0 || engine.CurrentHealthPattern.Id != persistedPatternId ||
        engine.CurrentHealthPattern.GeneratedAt != persistedGeneratedAt)
        throw new InvalidOperationException("Health pattern was regenerated instead of reused.");
    engine.StartLevel(GameDifficulty.Easy);
    if (!engine.AdvanceLevel(false) || engine.CurrentDifficulty != GameDifficulty.Medium || !engine.AdvanceLevel(false) ||
        engine.CurrentDifficulty != GameDifficulty.Hard || !engine.AdvanceLevel(false) || engine.CurrentDifficulty != GameDifficulty.VeryHard || engine.AdvanceLevel(false))
        throw new InvalidOperationException("Hardness progression failed.");
    Console.WriteLine("VERIFY COMPLETE: generated health patterns, normalization, persistence, allocation, evidence, overtime and progression passed");
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
