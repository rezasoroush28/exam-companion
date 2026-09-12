using ChallengePrototype.Models;

namespace ChallengePrototype.Services;

public sealed record RecoveryTopicCandidate(long TopicProgressId, long TopicId, int Importance,
    int QuestionsSeen, int TotalBonusEarned)
{
    public double AverageBonus => QuestionsSeen == 0 ? 0 : (double)TotalBonusEarned / QuestionsSeen;
}

public sealed record RecoveryContext(long LessonChallengeId, ChallengeLevel CurrentLevel,
    DateTimeOffset? LatestAssessmentAt, IReadOnlyList<RecoveryTopicCandidate> Candidates);

public sealed record RecoverySuggestionDraft(long TopicProgressId, long TopicId, int Rank, int Importance,
    int QuestionsSeen, int TotalBonusEarned, double AverageBonus);

public sealed record RecoverySuggestionCard(long Id, long TopicProgressId, long TopicId, string TopicTitle,
    int Rank, int Importance, int QuestionsSeen, int TotalBonusEarned, double AverageBonus);

public sealed record RecoverySuggestionPage(long SuggestionSetId, SuggestionSetStatus Status,
    ChallengeLevel CurrentLevel, IReadOnlyList<RecoverySuggestionCard> Suggestions);

public sealed record StudyClaimResult(long StudyClaimId, IReadOnlyList<long> TopicIds);

public sealed record EducationalAnswer(string Option, string Text, bool IsCorrect);
public sealed record EducationalQuestion(string Id, long TopicId, string TopicTitle, string Text,
    string? Explanation, QuestionDifficulty Difficulty, IReadOnlyList<EducationalAnswer> Answers)
{
    public string CorrectOption => Answers.Single(x => x.IsCorrect).Option;
}

public sealed record RecoveryTopicSnapshot(long Id, long TopicProgressId, long TopicId, int Importance,
    int Sequence, int RequiredCorrect, int CorrectAnswers, int IncorrectAnswers, int QuestionsAsked,
    double Stability, TopicStatus Status, DateTimeOffset? StartedAt, DateTimeOffset? StabilizedAt);

public sealed record RecoveryAttemptSnapshot(string QuestionId, long TopicId, QuestionDifficulty ActualDifficulty,
    bool IsCorrect, DateTimeOffset ShownAt, DateTimeOffset? AnsweredAt);

public sealed record RecoverySessionSnapshot(long Id, long LessonChallengeId, long StudyClaimId,
    MiniGameStatus Status, int CurrentTopicIndex, DateTimeOffset? StartedAt, DateTimeOffset? CompletedAt,
    IReadOnlyList<RecoveryTopicSnapshot> Topics, IReadOnlyList<RecoveryAttemptSnapshot> Attempts);

public sealed record RecoveryAnswerWrite(long SessionId, long TopicSessionId, string QuestionId, long TopicId,
    QuestionDifficulty ActualDifficulty, DateTimeOffset ShownAt, DateTimeOffset AnsweredAt,
    string SelectedAnswer, bool IsCorrect, long ActiveThinkingMilliseconds, int HintLevelUsed,
    int CorrectAnswers, int IncorrectAnswers, int QuestionsAsked, double Stability,
    TopicStatus TopicStatus, DateTimeOffset? StabilizedAt, int CurrentTopicIndex,
    MiniGameStatus SessionStatus, DateTimeOffset? CompletedAt);

public sealed record RecoveryAnswerResult(bool IsCorrect, bool TopicStabilized, bool SessionCompleted,
    double Stability, int CorrectAnswers, int RequiredCorrect);

public interface IRecoveryStore
{
    Task<RecoveryContext?> GetContextAsync(long lessonChallengeId, CancellationToken cancellationToken = default);
    Task<RecoverySuggestionSet?> GetActiveSuggestionSetAsync(long lessonChallengeId, CancellationToken cancellationToken = default);
    Task<RecoverySuggestionSet> CreateSuggestionSetAsync(long lessonChallengeId,
        IReadOnlyList<RecoverySuggestionDraft> items, CancellationToken cancellationToken = default);
    Task<StudyClaim> CreateStudyClaimAsync(long suggestionSetId, CancellationToken cancellationToken = default);
    Task<StudyClaim?> GetStudyClaimAsync(long studyClaimId, CancellationToken cancellationToken = default);
    Task<RecoveryMiniGameSession?> GetSessionForClaimAsync(long studyClaimId, CancellationToken cancellationToken = default);
    Task<RecoveryMiniGameSession?> GetLatestSessionForChallengeAsync(long lessonChallengeId, CancellationToken cancellationToken = default);
    Task<RecoveryMiniGameSession> ReconcileSessionTopicsAsync(long sessionId, CancellationToken cancellationToken = default);
    Task<bool> HasAssessmentActivityAfterAsync(long lessonChallengeId, DateTimeOffset since,
        CancellationToken cancellationToken = default);
    Task<RecoveryMiniGameSession> CreateSessionAsync(StudyClaim claim, CancellationToken cancellationToken = default);
    Task<RecoverySessionSnapshot?> GetSessionAsync(long sessionId, CancellationToken cancellationToken = default);
    Task SaveAnswerAsync(RecoveryAnswerWrite answer, CancellationToken cancellationToken = default);
    Task SetSessionStatusAsync(long sessionId, MiniGameStatus status, CancellationToken cancellationToken = default);
}

public interface IEducationalQuestionSource
{
    Task<IReadOnlyDictionary<long, string>> GetTopicTitlesAsync(IEnumerable<long> topicIds,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EducationalQuestion>> GetQuestionsAsync(long topicId,
        CancellationToken cancellationToken = default);
}

public interface ITopicSuggestionService
{
    IReadOnlyList<RecoverySuggestionDraft> Rank(IEnumerable<RecoveryTopicCandidate> candidates, int maximum = 3);
    Task<RecoverySuggestionPage> GetOrCreateAsync(long lessonChallengeId, CancellationToken cancellationToken = default);
}

public interface IStudyClaimService
{
    Task<StudyClaimResult> ClaimAsync(long suggestionSetId, CancellationToken cancellationToken = default);
}

public interface IRecoveryMiniGameService
{
    Task<RecoverySessionSnapshot> StartOrResumeAsync(long studyClaimId, CancellationToken cancellationToken = default);
    Task<RecoverySessionSnapshot?> ResumeLatestAsync(long lessonChallengeId, CancellationToken cancellationToken = default);
    Task<RecoverySessionSnapshot?> GetAsync(long sessionId, CancellationToken cancellationToken = default);
    Task<EducationalQuestion?> GetNextQuestionAsync(RecoverySessionSnapshot session,
        CancellationToken cancellationToken = default);
    Task<RecoveryAnswerResult> AnswerAsync(RecoverySessionSnapshot session, EducationalQuestion question,
        string selectedAnswer, DateTimeOffset shownAt, long activeThinkingMilliseconds, int hintLevelUsed,
        CancellationToken cancellationToken = default);
    Task InterruptAsync(long sessionId, CancellationToken cancellationToken = default);
}
