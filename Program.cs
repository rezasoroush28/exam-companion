using ChallengePrototype.Components;
using ChallengePrototype.Data;
using ChallengePrototype.Models;
using ChallengePrototype.Services;
using ExamCompanion.Application;
using ExamCompanion.Infrastructure;
using Microsoft.EntityFrameworkCore;

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

    var suggestionRanker = new TopicSuggestionService(null!, null!);
    var ranked = suggestionRanker.Rank([
        new RecoveryTopicCandidate(1, 11, 5, 2, 20),
        new RecoveryTopicCandidate(2, 12, 1, 1, 5),
        new RecoveryTopicCandidate(3, 13, 2, 3, 15),
        new RecoveryTopicCandidate(4, 14, 4, 3, 15),
        new RecoveryTopicCandidate(5, 15, 5, 0, 0)
    ]);
    if (ranked.Count != 3 || !ranked.Select(x => x.TopicId).SequenceEqual([14L, 13L, 12L]) ||
        ranked.Select(x => x.Rank).SequenceEqual([1, 2, 3]) is false ||
        ranked.Single(x => x.TopicId == 14).AverageBonus != 15d / 3 ||
        suggestionRanker.Rank([new RecoveryTopicCandidate(9, 99, 5, 0, 0)]).Count != 0)
        throw new InvalidOperationException("Recovery suggestion ordering failed.");
    if (RecoveryRules.RequiredCorrect(1) != 1 || RecoveryRules.RequiredCorrect(2) != 2 ||
        RecoveryRules.RequiredCorrect(3) != 3 || RecoveryRules.RequiredCorrect(4) != 4 ||
        RecoveryRules.RequiredCorrect(5) != 5 || RecoveryRules.Stability(2, 4) != .5 ||
        RecoveryRules.Stability(8, 4) != 1 || RecoveryRules.HintLevel(9_999) != 0 ||
        RecoveryRules.HintLevel(10_000) != 1 || RecoveryRules.HintLevel(18_000) != 2 ||
        RecoveryRules.HintLevel(28_000) != 3)
        throw new InvalidOperationException("Recovery threshold or stability rules failed.");
    var wrongRecovery = RecoveryRules.ApplyAnswer(1, 0, 1, 3, false);
    var exactThreshold = RecoveryRules.ApplyAnswer(2, 1, 3, 3, true);
    if (wrongRecovery.Correct != 1 || wrongRecovery.Incorrect != 1 || wrongRecovery.Stability != 1d / 3 ||
        wrongRecovery.Stabilized || exactThreshold.Correct != 3 || !exactThreshold.Stabilized ||
        !RecoveryRules.IsSessionComplete(3, 3, true) || RecoveryRules.IsSessionComplete(2, 3, true))
        throw new InvalidOperationException("Recovery no-rollback, exact stabilization, or completion rules failed.");
    if (RecoveryRules.RecoveryStartDifficulty(ChallengeLevel.Easy) != QuestionDifficulty.Easy ||
        RecoveryRules.RecoveryStartDifficulty(ChallengeLevel.Medium) != QuestionDifficulty.Easy ||
        RecoveryRules.RecoveryStartDifficulty(ChallengeLevel.Hard) != QuestionDifficulty.Medium ||
        RecoveryRules.RecoveryStartDifficulty(ChallengeLevel.VeryHard) != QuestionDifficulty.Hard ||
        RecoveryRules.NextDifficulty(QuestionDifficulty.Medium, true, ChallengeLevel.Hard) != QuestionDifficulty.Hard ||
        RecoveryRules.NextDifficulty(QuestionDifficulty.Medium, false, ChallengeLevel.Hard) != QuestionDifficulty.Medium ||
        RecoveryRules.NextDifficulty(QuestionDifficulty.Hard, true, ChallengeLevel.Hard) != QuestionDifficulty.Hard)
        throw new InvalidOperationException("Recovery difficulty policy failed.");

    var educational = scope.ServiceProvider.GetRequiredService<IEducationalQuestionSource>();
    var educationalTopicIds = engine.Topics.Select(x => x.Id).ToArray();
    var educationalCount = 0;
    foreach (var topicId in educationalTopicIds)
    {
        var topicQuestions = await educational.GetQuestionsAsync(topicId);
        if (topicQuestions.Count != 20 || topicQuestions.Any(x => x.Answers.Count != 2 || x.Answers.Count(a => a.IsCorrect) != 1))
            throw new InvalidOperationException($"Educational question inventory failed for topic {topicId}.");
        educationalCount += topicQuestions.Count;
    }
    if (educationalCount != 100) throw new InvalidOperationException("Educational question inventory must contain 100 mapped questions.");

    var verifyDbPath = Path.Combine(Path.GetTempPath(), $"exam-companion-recovery-{Guid.NewGuid():N}.sqlite");
    var verifyOptions = new DbContextOptionsBuilder<ChallengeDbContext>().UseSqlite($"Data Source={verifyDbPath}").Options;
    var verifyFactory = new VerificationDbContextFactory(verifyOptions);
    try
    {
        long challengeId;
        long topicProgressId;
        await using (var verifyDb = await verifyFactory.CreateDbContextAsync())
        {
            await verifyDb.Database.MigrateAsync();
            var exam = new ExamSession
            {
                UserId = "recovery-verifier", Title = "Recovery verifier", ExamDate = DateTimeOffset.UtcNow,
                CreatedAt = DateTimeOffset.UtcNow, Status = ExamSessionStatus.Active
            };
            var challenge = new ExamLessonChallenge
            {
                ExamSession = exam, LessonId = 1, LevelDesignId = 1, LevelDesignVersion = 1,
                CurrentLevel = ChallengeLevel.Hard, Status = LessonChallengeStatus.Active, CreatedAt = DateTimeOffset.UtcNow
            };
            var progress = new TopicProgress
            {
                LessonChallenge = challenge, TopicId = educationalTopicIds[0], ImportanceSnapshot = 3,
                QuestionsSeen = 4, CorrectAnswers = 2, IncorrectAnswers = 2, TotalBonusEarned = 28
            };
            verifyDb.AddRange(progress,
                new TopicProgress
                {
                    LessonChallenge = challenge, TopicId = educationalTopicIds[1], ImportanceSnapshot = 5,
                    QuestionsSeen = 2, CorrectAnswers = 1, IncorrectAnswers = 1, TotalBonusEarned = 10
                },
                new TopicProgress
                {
                    LessonChallenge = challenge, TopicId = educationalTopicIds[2], ImportanceSnapshot = 1,
                    QuestionsSeen = 2, CorrectAnswers = 1, IncorrectAnswers = 1, TotalBonusEarned = 12
                });
            await verifyDb.SaveChangesAsync();
            challengeId = challenge.Id;
            topicProgressId = progress.Id;
        }
        var recoveryStore = new RecoveryStore(verifyFactory);
        var recoverySuggestions = new TopicSuggestionService(recoveryStore, new VerificationEducationalSource());
        var suggestionPage = await recoverySuggestions.GetOrCreateAsync(challengeId);
        if (suggestionPage.Suggestions.Count != 3 ||
            !suggestionPage.Suggestions.OrderBy(x => x.Rank).Select(x => x.TopicId)
                .SequenceEqual([educationalTopicIds[1], educationalTopicIds[2], educationalTopicIds[0]]))
            throw new InvalidOperationException("Persisted recovery suggestion loading failed.");
        await using (var newerAssessmentDb = await verifyFactory.CreateDbContextAsync())
        {
            var newerProgress = await newerAssessmentDb.TopicProgresses.ToArrayAsync();
            foreach (var progressRow in newerProgress) progressRow.LastSeenAt = DateTimeOffset.UtcNow.AddMinutes(5);
            await newerAssessmentDb.SaveChangesAsync();
        }
        var refreshedSuggestionPage = await recoverySuggestions.GetOrCreateAsync(challengeId);
        if (refreshedSuggestionPage.SuggestionSetId == suggestionPage.SuggestionSetId ||
            refreshedSuggestionPage.Suggestions.Count != 3)
            throw new InvalidOperationException("New assessment activity did not create a fresh recovery suggestion cycle.");
        suggestionPage = refreshedSuggestionPage;
        var claim = await recoveryStore.CreateStudyClaimAsync(suggestionPage.SuggestionSetId);
        if (!claim.Topics.OrderBy(x => x.Sequence).Select(x => x.TopicId)
            .SequenceEqual([educationalTopicIds[1], educationalTopicIds[0], educationalTopicIds[2]]))
            throw new InvalidOperationException("Claimed recovery topics were not ordered by importance.");
        await using (var legacyDb = await verifyFactory.CreateDbContextAsync())
        {
            var omitted = await legacyDb.StudyClaimTopics.Where(x => x.ClaimId == claim.Id && x.Sequence > 0).ToArrayAsync();
            legacyDb.RemoveRange(omitted);
            await legacyDb.SaveChangesAsync();
        }
        var legacyClaim = await recoveryStore.GetStudyClaimAsync(claim.Id)
            ?? throw new InvalidOperationException("Legacy recovery claim setup failed.");
        var recoverySession = await recoveryStore.CreateSessionAsync(legacyClaim);
        var repairedSession = await recoveryStore.ReconcileSessionTopicsAsync(recoverySession.Id);
        if (repairedSession.Topics.Count != 3 || repairedSession.StudyClaim.Topics.Count != 3 ||
            !repairedSession.Topics.OrderBy(x => x.Sequence).Select(x => x.TopicId)
                .SequenceEqual([educationalTopicIds[1], educationalTopicIds[0], educationalTopicIds[2]]))
            throw new InvalidOperationException("Legacy one-topic recovery session reconciliation failed.");
        var snapshot = await recoveryStore.GetSessionAsync(recoverySession.Id)
            ?? throw new InvalidOperationException("Recovery session persistence failed.");
        var recoveryTopic = snapshot.Topics.OrderBy(x => x.Sequence).First();
        await recoveryStore.SaveAnswerAsync(new RecoveryAnswerWrite(snapshot.Id, recoveryTopic.Id, "verify-question",
            recoveryTopic.TopicId, QuestionDifficulty.Medium, DateTimeOffset.UtcNow.AddSeconds(-2), DateTimeOffset.UtcNow,
            "1", true, 2_000, 0, 1, 0, 1, RecoveryRules.Stability(1, recoveryTopic.RequiredCorrect),
            TopicStatus.Active, null, 0, MiniGameStatus.Active, null));
        await using var assertDb = await verifyFactory.CreateDbContextAsync();
        var unchanged = await assertDb.TopicProgresses.AsNoTracking().SingleAsync(x => x.Id == topicProgressId);
        if (unchanged.QuestionsSeen != 4 || unchanged.CorrectAnswers != 2 || unchanged.IncorrectAnswers != 2 ||
            unchanged.TotalBonusEarned != 28 || await assertDb.QuestionAttempts.CountAsync() != 0 ||
            await assertDb.RecoveryQuestionAttempts.CountAsync() != 1)
            throw new InvalidOperationException("Recovery isolation or attempt persistence failed.");
        var persistedRecovery = await recoveryStore.GetSessionAsync(snapshot.Id);
        if (persistedRecovery?.Topics.Single(x => x.Sequence == 0).CorrectAnswers != 1 || persistedRecovery.Attempts.Count != 1)
            throw new InvalidOperationException("Recovery progression did not persist.");
    }
    finally
    {
        await using var cleanupDb = await verifyFactory.CreateDbContextAsync();
        await cleanupDb.Database.EnsureDeletedAsync();
    }

    Console.WriteLine("VERIFY COMPLETE: main challenge, recovery ranking, thresholds, difficulty, educational inventory, EF isolation and persistence passed");
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

file sealed class VerificationDbContextFactory(DbContextOptions<ChallengeDbContext> options)
    : IDbContextFactory<ChallengeDbContext>
{
    public ChallengeDbContext CreateDbContext() => new(options);
    public Task<ChallengeDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(new ChallengeDbContext(options));
}

file sealed class VerificationEducationalSource : IEducationalQuestionSource
{
    public Task<IReadOnlyDictionary<long, string>> GetTopicTitlesAsync(IEnumerable<long> topicIds,
        CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyDictionary<long, string>>(
            topicIds.Distinct().ToDictionary(x => x, x => $"Topic {x}"));

    public Task<IReadOnlyList<EducationalQuestion>> GetQuestionsAsync(long topicId,
        CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<EducationalQuestion>>([]);
}
