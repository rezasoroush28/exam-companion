namespace ChallengePrototype.Models;

public enum ChallengeLevel { Easy, Medium, Hard, VeryHard }
public enum QuestionDifficulty { Easy, Medium, Hard, VeryHard }
public enum ExamSessionStatus { Planned, Active, Completed, Cancelled }
public enum LessonChallengeStatus { NotStarted, Active, Completed }
public enum LevelProgressStatus { NotStarted, Active, HealthDepleted, Completed }
public enum ChallengeRunStatus { Active, Completed, HealthDepleted, Abandoned, Interrupted }

public sealed class ExamSession
{
    public long Id { get; set; }
    public string UserId { get; set; } = "";
    public string Title { get; set; } = "";
    public DateTimeOffset ExamDate { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public ExamSessionStatus Status { get; set; }
    public List<ExamLessonChallenge> LessonChallenges { get; set; } = [];
}

public sealed class ExamLessonChallenge
{
    public long Id { get; set; }
    public long ExamSessionId { get; set; }
    public long LessonId { get; set; }
    public long LevelDesignId { get; set; }
    public int LevelDesignVersion { get; set; }
    public ChallengeLevel CurrentLevel { get; set; }
    public LessonChallengeStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public ExamSession ExamSession { get; set; } = null!;
    public LevelDesign LevelDesign { get; set; } = null!;
    public List<TopicProgress> TopicProgresses { get; set; } = [];
    public List<ChallengeLevelProgress> LevelProgresses { get; set; } = [];
}

public sealed class TopicProgress
{
    public long Id { get; set; }
    public long LessonChallengeId { get; set; }
    public long TopicId { get; set; }
    public int ImportanceSnapshot { get; set; }
    public int QuestionsSeen { get; set; }
    public int CorrectAnswers { get; set; }
    public int IncorrectAnswers { get; set; }
    public int TotalBonusEarned { get; set; }
    public DateTimeOffset? FirstSeenAt { get; set; }
    public DateTimeOffset? LastSeenAt { get; set; }
    public ExamLessonChallenge LessonChallenge { get; set; } = null!;
}

public sealed class ChallengeLevelProgress
{
    public long Id { get; set; }
    public long LessonChallengeId { get; set; }
    public ChallengeLevel ChallengeLevel { get; set; }
    public LevelProgressStatus Status { get; set; }
    public double StartingHealth { get; set; }
    public double CurrentHealth { get; set; }
    public int TargetQuestionCount { get; set; }
    public int QuestionsAnswered { get; set; }
    public int CorrectAnswers { get; set; }
    public int IncorrectAnswers { get; set; }
    public int EvidenceRequired { get; set; }
    public int EvidenceCollected { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public ExamLessonChallenge LessonChallenge { get; set; } = null!;
    public List<ChallengeRun> ChallengeRuns { get; set; } = [];
}

public sealed class LevelDesign
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public int Version { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public List<LevelRule> LevelRules { get; set; } = [];
}

public sealed class LevelRule
{
    public long Id { get; set; }
    public long LevelDesignId { get; set; }
    public ChallengeLevel ChallengeLevel { get; set; }
    public double AverageQuestionsPerTopic { get; set; }
    public int MinimumQuestionsPerTopic { get; set; }
    public int MaximumQuestionsPerTopic { get; set; }
    public int MaximumTotalQuestions { get; set; }
    public double EasyQuestionWeight { get; set; }
    public double MediumQuestionWeight { get; set; }
    public double HardQuestionWeight { get; set; }
    public double VeryHardQuestionWeight { get; set; }
    public double ImportanceFocus { get; set; }
    public double StartingHealth { get; set; }
    public double PromotionHealth { get; set; }
    public double FailureHealth { get; set; }
    public int MinimumEvidencePercent { get; set; }
    public LevelDesign LevelDesign { get; set; } = null!;
}

public sealed class ChallengeRun
{
    public long Id { get; set; }
    public long LevelProgressId { get; set; }
    public int RunNumber { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? EndedAt { get; set; }
    public double StartHealth { get; set; }
    public double? EndHealth { get; set; }
    public ChallengeRunStatus Status { get; set; }
    public string? StopReason { get; set; }
    public ChallengeLevelProgress LevelProgress { get; set; } = null!;
    public List<QuestionAttempt> QuestionAttempts { get; set; } = [];
}

public sealed class QuestionAttempt
{
    public long Id { get; set; }
    public long ChallengeRunId { get; set; }
    public long TopicProgressId { get; set; }
    public long QuestionId { get; set; }
    public ChallengeLevel ChallengeLevel { get; set; }
    public QuestionDifficulty ActualQuestionDifficulty { get; set; }
    public int ImportanceSnapshot { get; set; }
    public DateTimeOffset ShownAt { get; set; }
    public DateTimeOffset? AnsweredAt { get; set; }
    public string? SelectedAnswer { get; set; }
    public bool? IsCorrect { get; set; }
    public long? ActiveThinkingMilliseconds { get; set; }
    public double HealthBefore { get; set; }
    public double TimeHealthDelta { get; set; }
    public double AnswerHealthDelta { get; set; }
    public double? HealthAfter { get; set; }
    public int BonusEarned { get; set; }
    public ChallengeRun ChallengeRun { get; set; } = null!;
    public TopicProgress TopicProgress { get; set; } = null!;
}

public static class LevelTypeConversions
{
    public static ChallengeLevel ToChallengeLevel(this GameDifficulty value) => (ChallengeLevel)(int)value;
    public static QuestionDifficulty ToQuestionDifficulty(this GameDifficulty value) => (QuestionDifficulty)(int)value;
    public static GameDifficulty ToGameDifficulty(this ChallengeLevel value) => (GameDifficulty)(int)value;
    public static GameDifficulty ToGameDifficulty(this QuestionDifficulty value) => (GameDifficulty)(int)value;
}
