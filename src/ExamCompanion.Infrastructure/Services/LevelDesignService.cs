using ChallengePrototype.Data;
using ChallengePrototype.Models;
using Microsoft.EntityFrameworkCore;

namespace ChallengePrototype.Services;

public sealed class LevelDesignService(IDbContextFactory<ChallengeDbContext> contextFactory) : ILevelDesignService
{
    public async Task<LevelDesign> GetActiveDesignAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var design = await db.LevelDesigns.AsNoTracking().Include(x => x.LevelRules)
            .SingleAsync(x => x.IsActive, cancellationToken);
        Validate(design);
        return design;
    }

    public IReadOnlyList<LevelBlueprint> GenerateSegments(LevelRule rule, IReadOnlyCollection<(long TopicId, int Importance)> topics)
    {
        if (topics.Count == 0) throw new ArgumentException("At least one topic is required.", nameof(topics));
        var segmentCapacity = Math.Max(1, rule.MaximumTotalQuestions / rule.MinimumQuestionsPerTopic);
        return topics.Chunk(segmentCapacity).Select(segment => Generate(rule, segment)).ToArray();
    }

    public async Task<ExamLessonChallenge> GetOrCreateLessonChallengeAsync(ChallengeSetup setup, LevelDesign design,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var existing = await db.LessonChallenges.AsNoTracking().Include(x => x.TopicProgresses).Include(x => x.LevelProgresses)
            .Where(x => x.ExamSession.UserId == "mvp-local" && x.LessonId == setup.LessonId && x.Status != LessonChallengeStatus.Completed)
            .OrderByDescending(x => x.Id).FirstOrDefaultAsync(cancellationToken);
        if (existing is not null) return existing;

        var now = DateTimeOffset.UtcNow;
        var session = new ExamSession { UserId = "mvp-local", Title = setup.LessonTitle, ExamDate = now.AddMonths(1),
            CreatedAt = now, StartedAt = now, Status = ExamSessionStatus.Active };
        var challenge = new ExamLessonChallenge { ExamSession = session, LessonId = setup.LessonId, LevelDesignId = design.Id,
            LevelDesignVersion = design.Version, CurrentLevel = ChallengeLevel.Easy, Status = LessonChallengeStatus.Active, CreatedAt = now };
        challenge.TopicProgresses.AddRange(setup.Topics.Select(topic => new TopicProgress
        { TopicId = topic.Id, ImportanceSnapshot = BonusConfiguration.ImportanceTier(topic.Importance) }));
        foreach (var rule in design.LevelRules.OrderBy(x => x.ChallengeLevel))
        {
            var blueprint = Generate(rule, challenge.TopicProgresses.Select(x => (x.TopicId, x.ImportanceSnapshot)).ToArray());
            challenge.LevelProgresses.Add(new ChallengeLevelProgress { ChallengeLevel = rule.ChallengeLevel,
                Status = rule.ChallengeLevel == ChallengeLevel.Easy ? LevelProgressStatus.Active : LevelProgressStatus.NotStarted,
                StartingHealth = rule.StartingHealth, CurrentHealth = rule.StartingHealth,
                TargetQuestionCount = blueprint.TargetQuestionCount, EvidenceRequired = blueprint.EvidenceRequired,
                StartedAt = rule.ChallengeLevel == ChallengeLevel.Easy ? now : null });
        }
        db.LessonChallenges.Add(challenge);
        await db.SaveChangesAsync(cancellationToken);
        return challenge;
    }

    public void SetCurrentLevel(long lessonChallengeId, ChallengeLevel level)
    {
        using var db = contextFactory.CreateDbContext();
        var challenge = db.LessonChallenges.Include(x => x.LevelProgresses).Single(x => x.Id == lessonChallengeId);
        var previous = challenge.LevelProgresses.Single(x => x.ChallengeLevel == challenge.CurrentLevel);
        previous.Status = LevelProgressStatus.Completed;
        previous.CompletedAt = DateTimeOffset.UtcNow;
        challenge.CurrentLevel = level;
        foreach (var progress in challenge.LevelProgresses)
            if (progress.ChallengeLevel == level && progress.Status == LevelProgressStatus.NotStarted)
            { progress.Status = LevelProgressStatus.Active; progress.StartedAt = DateTimeOffset.UtcNow; }
        db.SaveChanges();
    }

    public long BeginRun(long lessonChallengeId, ChallengeLevel level, double health)
    {
        using var db = contextFactory.CreateDbContext();
        var progress = db.LevelProgresses.Include(x => x.ChallengeRuns)
            .Single(x => x.LessonChallengeId == lessonChallengeId && x.ChallengeLevel == level);
        var run = new ChallengeRun { RunNumber = progress.ChallengeRuns.Count + 1, StartedAt = DateTimeOffset.UtcNow,
            StartHealth = health, Status = ChallengeRunStatus.Active };
        progress.ChallengeRuns.Add(run); db.SaveChanges(); return run.Id;
    }

    public void RecordAttempt(long runId, long lessonChallengeId, ChallengeQuestion question, string? selectedAnswer,
        long thinkingMilliseconds, double healthBefore, double timeLoss, double answerEffect, double healthAfter, bool correct)
    {
        using var db = contextFactory.CreateDbContext();
        var run = db.ChallengeRuns.Include(x => x.LevelProgress).Single(x => x.Id == runId);
        var topic = db.TopicProgresses.Single(x => x.LessonChallengeId == lessonChallengeId && x.TopicId == question.Topic.Id);
        var now = DateTimeOffset.UtcNow;
        var bonus = correct ? BonusConfiguration.Calculate(topic.ImportanceSnapshot, question.RequestedDifficulty) : 0;
        db.QuestionAttempts.Add(new QuestionAttempt { ChallengeRunId = runId, TopicProgressId = topic.Id,
            QuestionId = question.Id, ChallengeLevel = run.LevelProgress.ChallengeLevel,
            ActualQuestionDifficulty = question.RequestedDifficulty.ToQuestionDifficulty(), ImportanceSnapshot = topic.ImportanceSnapshot,
            ShownAt = now.AddMilliseconds(-thinkingMilliseconds), AnsweredAt = now, SelectedAnswer = selectedAnswer,
            IsCorrect = correct, ActiveThinkingMilliseconds = thinkingMilliseconds, HealthBefore = healthBefore,
            TimeHealthDelta = -timeLoss, AnswerHealthDelta = correct ? answerEffect : -answerEffect, HealthAfter = healthAfter,
            BonusEarned = bonus });
        topic.QuestionsSeen++; topic.CorrectAnswers += correct ? 1 : 0; topic.IncorrectAnswers += correct ? 0 : 1;
        topic.TotalBonusEarned += bonus;
        topic.FirstSeenAt ??= now; topic.LastSeenAt = now;
        run.LevelProgress.QuestionsAnswered++; run.LevelProgress.CorrectAnswers += correct ? 1 : 0;
        run.LevelProgress.IncorrectAnswers += correct ? 0 : 1; run.LevelProgress.EvidenceCollected++;
        run.LevelProgress.CurrentHealth = healthAfter;
        db.SaveChanges();
    }

    public async Task<IReadOnlyList<TopicPerformanceReport>> GetTopicPerformanceAsync(long lessonChallengeId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.TopicProgresses.AsNoTracking().Where(x => x.LessonChallengeId == lessonChallengeId)
            .OrderByDescending(x => x.TotalBonusEarned).ThenBy(x => x.TopicId)
            .Select(x => new TopicPerformanceReport(x.TopicId, x.ImportanceSnapshot, x.QuestionsSeen,
                x.CorrectAnswers, x.IncorrectAnswers,
                x.QuestionsSeen == 0 ? 0 : 100d * x.CorrectAnswers / x.QuestionsSeen, x.TotalBonusEarned))
            .ToListAsync(cancellationToken);
    }

    public void EndRun(long runId, double health, ChallengeRunStatus status, string reason)
    {
        using var db = contextFactory.CreateDbContext();
        var run = db.ChallengeRuns.Single(x => x.Id == runId);
        run.EndedAt = DateTimeOffset.UtcNow; run.EndHealth = health; run.Status = status; run.StopReason = reason;
        db.SaveChanges();
    }

    public void CompleteLessonChallenge(long lessonChallengeId)
    {
        using var db = contextFactory.CreateDbContext();
        var challenge = db.LessonChallenges.Include(x => x.LevelProgresses).Single(x => x.Id == lessonChallengeId);
        var now = DateTimeOffset.UtcNow;
        var current = challenge.LevelProgresses.Single(x => x.ChallengeLevel == challenge.CurrentLevel);
        current.Status = LevelProgressStatus.Completed; current.CompletedAt = now;
        challenge.Status = LessonChallengeStatus.Completed; challenge.CompletedAt = now;
        db.SaveChanges();
    }

    public LevelBlueprint Generate(LevelRule rule, IReadOnlyCollection<(long TopicId, int Importance)> topics)
    {
        if (topics.Count == 0) throw new ArgumentException("At least one topic is required.", nameof(topics));
        if (topics.Count * rule.MinimumQuestionsPerTopic > rule.MaximumTotalQuestions)
            throw new InvalidOperationException("This topic set requires multiple level segments; call GenerateSegments.");

        var normalized = topics.Select(x => (x.TopicId, Importance: Math.Clamp(x.Importance, 1, 5))).ToArray();
        var minimumTotal = normalized.Length * rule.MinimumQuestionsPerTopic;
        var capacityTotal = Math.Min(rule.MaximumTotalQuestions, normalized.Length * rule.MaximumQuestionsPerTopic);
        var desiredTotal = Math.Clamp((int)Math.Round(normalized.Length * rule.AverageQuestionsPerTopic,
            MidpointRounding.AwayFromZero), minimumTotal, capacityTotal);
        var quotas = normalized.ToDictionary(x => x.TopicId, x => rule.MinimumQuestionsPerTopic);

        while (quotas.Values.Sum() < desiredTotal)
        {
            var eligible = normalized.Where(x => quotas[x.TopicId] < rule.MaximumQuestionsPerTopic)
                .OrderByDescending(x => Math.Pow(x.Importance, rule.ImportanceFocus) / (quotas[x.TopicId] + 1d))
                .ThenByDescending(x => x.Importance).ThenBy(x => x.TopicId).ToArray();
            if (eligible.Length == 0) break;
            quotas[eligible[0].TopicId]++;
        }

        var blueprintTopics = normalized.Select(x => new BlueprintTopic(x.TopicId, x.Importance, quotas[x.TopicId])).ToArray();
        return new LevelBlueprint(rule.ChallengeLevel, rule, blueprintTopics, AllocateDifficulties(rule, desiredTotal));
    }

    public IReadOnlyDictionary<QuestionDifficulty, int> AllocateDifficulties(LevelRule rule, int total)
    {
        var weights = new Dictionary<QuestionDifficulty, double>
        {
            [QuestionDifficulty.Easy] = rule.EasyQuestionWeight, [QuestionDifficulty.Medium] = rule.MediumQuestionWeight,
            [QuestionDifficulty.Hard] = rule.HardQuestionWeight, [QuestionDifficulty.VeryHard] = rule.VeryHardQuestionWeight
        };
        var weightTotal = weights.Values.Sum();
        if (weightTotal <= 0) throw new InvalidOperationException("Difficulty weights must have a positive sum.");
        var raw = weights.ToDictionary(x => x.Key, x => x.Value / weightTotal * total);
        var result = raw.ToDictionary(x => x.Key, x => (int)Math.Floor(x.Value));
        foreach (var difficulty in raw.OrderByDescending(x => x.Value - Math.Floor(x.Value)).ThenBy(x => x.Key)
                     .Take(total - result.Values.Sum()).Select(x => x.Key)) result[difficulty]++;
        if (total >= 4)
            foreach (var missing in result.Where(x => x.Value == 0 && weights[x.Key] > 0).Select(x => x.Key).ToArray())
            {
                var donor = result.Where(x => x.Value > 1).OrderByDescending(x => x.Value).ThenByDescending(x => weights[x.Key]).First().Key;
                result[donor]--; result[missing]++;
            }
        return result;
    }

    public static void Validate(LevelDesign design)
    {
        if (design.LevelRules.Count != 4 || Enum.GetValues<ChallengeLevel>().Any(level => design.LevelRules.Count(x => x.ChallengeLevel == level) != 1))
            throw new InvalidOperationException("A LevelDesign must contain exactly one rule for every challenge level.");
    }
}
