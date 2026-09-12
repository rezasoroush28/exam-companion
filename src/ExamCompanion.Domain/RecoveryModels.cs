namespace ChallengePrototype.Models;

public enum SuggestionSetStatus { Active, Claimed, Expired }
public enum MiniGameStatus { NotStarted, Active, Completed, Abandoned, Interrupted }
public enum TopicStatus { Pending, Active, Stabilized }

public sealed class RecoverySuggestionSet
{
    public long Id { get; set; }
    public long LessonChallengeId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public SuggestionSetStatus Status { get; set; }
    public ExamLessonChallenge LessonChallenge { get; set; } = null!;
    public List<RecoverySuggestionItem> Items { get; set; } = [];
}

public sealed class RecoverySuggestionItem
{
    public long Id { get; set; }
    public long SetId { get; set; }
    public long TopicProgressId { get; set; }
    public long TopicId { get; set; }
    public int Rank { get; set; }
    public int ImportanceSnapshot { get; set; }
    public int QuestionsSeenSnapshot { get; set; }
    public int TotalBonusSnapshot { get; set; }
    public double AverageBonusSnapshot { get; set; }
    public RecoverySuggestionSet SuggestionSet { get; set; } = null!;
    public TopicProgress TopicProgress { get; set; } = null!;
}

public sealed class StudyClaim
{
    public long Id { get; set; }
    public long LessonChallengeId { get; set; }
    public long SetId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ClaimedAt { get; set; }
    public ExamLessonChallenge LessonChallenge { get; set; } = null!;
    public RecoverySuggestionSet SuggestionSet { get; set; } = null!;
    public List<StudyClaimTopic> Topics { get; set; } = [];
}

public sealed class StudyClaimTopic
{
    public long Id { get; set; }
    public long ClaimId { get; set; }
    public long SuggestionItemId { get; set; }
    public long TopicProgressId { get; set; }
    public long TopicId { get; set; }
    public int Importance { get; set; }
    public int Sequence { get; set; }
    public StudyClaim StudyClaim { get; set; } = null!;
    public RecoverySuggestionItem SuggestionItem { get; set; } = null!;
    public TopicProgress TopicProgress { get; set; } = null!;
}

public sealed class RecoveryMiniGameSession
{
    public long Id { get; set; }
    public long LessonChallengeId { get; set; }
    public long StudyClaimId { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public MiniGameStatus Status { get; set; }
    public int CurrentTopicIndex { get; set; }
    public ExamLessonChallenge LessonChallenge { get; set; } = null!;
    public StudyClaim StudyClaim { get; set; } = null!;
    public List<RecoveryTopicSession> Topics { get; set; } = [];
    public List<RecoveryQuestionAttempt> Attempts { get; set; } = [];
}

public sealed class RecoveryTopicSession
{
    public long Id { get; set; }
    public long MiniGameSessionId { get; set; }
    public long TopicProgressId { get; set; }
    public long TopicId { get; set; }
    public int Importance { get; set; }
    public int Sequence { get; set; }
    public int RequiredCorrect { get; set; }
    public int Correct { get; set; }
    public int Incorrect { get; set; }
    public int Asked { get; set; }
    public double Stability { get; set; }
    public TopicStatus Status { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? StabilizedAt { get; set; }
    public RecoveryMiniGameSession MiniGameSession { get; set; } = null!;
    public TopicProgress TopicProgress { get; set; } = null!;
    public List<RecoveryQuestionAttempt> Attempts { get; set; } = [];
}

public sealed class RecoveryQuestionAttempt
{
    public long Id { get; set; }
    public long SessionId { get; set; }
    public long TopicSessionId { get; set; }
    public string QuestionId { get; set; } = "";
    public long TopicId { get; set; }
    public QuestionDifficulty ActualDifficulty { get; set; }
    public DateTimeOffset ShownAt { get; set; }
    public DateTimeOffset? AnsweredAt { get; set; }
    public string? SelectedAnswer { get; set; }
    public bool IsCorrect { get; set; }
    public long ActiveThinkingMs { get; set; }
    public int HintLevelUsed { get; set; }
    public RecoveryMiniGameSession MiniGameSession { get; set; } = null!;
    public RecoveryTopicSession TopicSession { get; set; } = null!;
}

public static class RecoveryRules
{
    public sealed record Progress(int Correct, int Incorrect, int Asked, double Stability, bool Stabilized);

    public static int RequiredCorrect(int importance) => Math.Clamp(importance, 1, 5);

    public static double Stability(int correct, int required) => required <= 0
        ? 0
        : Math.Clamp((double)correct / required, 0, 1);

    public static Progress ApplyAnswer(int correct, int incorrect, int asked, int required, bool isCorrect)
    {
        var nextCorrect = correct + (isCorrect ? 1 : 0);
        var nextIncorrect = incorrect + (isCorrect ? 0 : 1);
        return new Progress(nextCorrect, nextIncorrect, asked + 1,
            Stability(nextCorrect, required), nextCorrect >= required);
    }

    public static int NextTopicIndex(int currentTopicIndex, bool stabilized) =>
        stabilized ? currentTopicIndex + 1 : currentTopicIndex;

    public static bool IsSessionComplete(int nextTopicIndex, int topicCount, bool stabilized) =>
        stabilized && nextTopicIndex >= topicCount;

    public static int HintLevel(long activeThinkingMilliseconds) => activeThinkingMilliseconds switch
    {
        >= 28_000 => 3,
        >= 18_000 => 2,
        >= 10_000 => 1,
        _ => 0
    };

    public static QuestionDifficulty RecoveryStartDifficulty(ChallengeLevel currentLevel) => currentLevel switch
    {
        ChallengeLevel.Easy => QuestionDifficulty.Easy,
        ChallengeLevel.Medium => QuestionDifficulty.Easy,
        ChallengeLevel.Hard => QuestionDifficulty.Medium,
        _ => QuestionDifficulty.Hard
    };

    public static QuestionDifficulty NextDifficulty(QuestionDifficulty actual, bool correct, ChallengeLevel currentLevel)
    {
        if (!correct) return actual;
        var cap = (QuestionDifficulty)(int)currentLevel;
        return actual < cap ? actual + 1 : actual;
    }
}
