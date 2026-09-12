using ChallengePrototype.Models;

namespace ChallengePrototype.Services;

public interface IQuestionService
{
    Task<ChallengeSetup> CreateFixedSetupAsync(CancellationToken cancellationToken);
}

public sealed record BlueprintTopic(long TopicId, int Importance, int QuestionQuota);
public sealed record LevelBlueprint(ChallengeLevel ChallengeLevel, LevelRule Rule,
    IReadOnlyList<BlueprintTopic> Topics, IReadOnlyDictionary<QuestionDifficulty, int> DifficultyQuotas)
{
    public int TargetQuestionCount => Topics.Sum(x => x.QuestionQuota);
    public int EvidenceRequired => (int)Math.Ceiling(TargetQuestionCount * Rule.MinimumEvidencePercent / 100d);
}

public sealed record TopicPerformanceReport(long TopicId, int Importance, int QuestionsSeen,
    int CorrectAnswers, int IncorrectAnswers, double Accuracy, int TotalBonusEarned);

public interface ILevelDesignService
{
    Task<LevelDesign> GetActiveDesignAsync(CancellationToken cancellationToken = default);
    IReadOnlyList<LevelBlueprint> GenerateSegments(LevelRule rule, IReadOnlyCollection<(long TopicId, int Importance)> topics);
    LevelBlueprint Generate(LevelRule rule, IReadOnlyCollection<(long TopicId, int Importance)> topics);
    IReadOnlyDictionary<QuestionDifficulty, int> AllocateDifficulties(LevelRule rule, int total);
    Task<ExamLessonChallenge> GetOrCreateLessonChallengeAsync(ChallengeSetup setup, LevelDesign design, CancellationToken cancellationToken = default);
    ChallengeHealthPattern GetOrCreateHealthPattern(long lessonChallengeId, LevelBlueprint blueprint);
    void SetCurrentLevel(long lessonChallengeId, ChallengeLevel level);
    long BeginRun(long lessonChallengeId, ChallengeLevel level, double health);
    void RecordAttempt(long runId, long lessonChallengeId, ChallengeQuestion question, string? selectedAnswer,
        long thinkingMilliseconds, double healthBefore, double timeLoss, double answerEffect, double healthAfter, bool correct);
    Task<IReadOnlyList<TopicPerformanceReport>> GetTopicPerformanceAsync(long lessonChallengeId, CancellationToken cancellationToken = default);
    void EndRun(long runId, double health, ChallengeRunStatus status, string reason);
    void CompleteLessonChallenge(long lessonChallengeId);
}

public interface IDevelopmentDataResetService
{
    Task ResetProgressAsync(CancellationToken cancellationToken = default);
}
