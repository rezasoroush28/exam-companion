using ExamCompanion.Domain.Music;

namespace ExamCompanion.Application.Music;

public sealed record LessonDto(Guid Id, string Name, string ExamName, IReadOnlyList<TopicStateDto> Topics, Guid? ResumeSessionId);
public sealed record TopicStateDto(Guid Id, string Name, int Order, double Importance, string Color, string MusicalKey,
    int RequiredCycles, int CompletedCycles, TopicProgressStatus Status, bool HasScratch, bool IsRepaired, double Progress);
public sealed record OptionDto(string Key, string Text);
public sealed record CurrentQuestionDto(Guid Id, QuestionType Type, string Text, IReadOnlyList<OptionDto> Options);
public sealed record GameStateDto(Guid SessionId, string LessonName, GameSessionStatus Status, Guid CurrentTopicId,
    int CurrentCycle, int RequiredCycles, GameStepType Step, Guid TurnId, CurrentQuestionDto? Question,
    IReadOnlyList<TopicStateDto> Topics, bool IsRepair, int EducationalAnswered, int EducationalTarget, int ChallengeAttempts);
public sealed record AnswerResultDto(GameStateDto State, bool Correct, string CorrectOption, string? Explanation,
    AnswerResultType Result, string Message, int TopicOrder);
public interface IGameStore
{
    Task<Lesson?> GetLessonAsync(Guid? id, CancellationToken ct = default);
    Task<GameSession?> GetSessionAsync(Guid id, CancellationToken ct = default);
    Task<Guid?> LatestSessionAsync(Guid lessonId, CancellationToken ct = default);
    Task AddAsync(GameSession session, CancellationToken ct = default);
    Task SaveAsync(CancellationToken ct = default);
}
public interface IGameEngine
{
    Task<LessonDto> GetLessonAsync(CancellationToken ct = default);
    Task<GameStateDto> StartSessionAsync(Guid lessonId, CancellationToken ct = default);
    Task<GameStateDto> GetSessionAsync(Guid id, CancellationToken ct = default);
    Task<AnswerResultDto> SubmitAnswerAsync(Guid sessionId, Guid questionId, Guid turnId, string option, CancellationToken ct = default);
    Task<GameStateDto> StartRepairAsync(Guid sessionId, Guid topicId, CancellationToken ct = default);
}
public sealed class GameInputException(string message) : Exception(message);
public interface IQuestionSelector
{
    Question Select(GameSession session, Topic topic, QuestionType type, bool reinforcement);
}
public sealed class QuestionSelector : IQuestionSelector
{
    public Question Select(GameSession session, Topic topic, QuestionType type, bool reinforcement)
    {
        var pool = topic.Questions.Where(q => q.Type == type).ToArray();
        if (pool.Length == 0) throw new GameInputException("برای این بخش هنوز سؤال کافی در بانک وجود ندارد.");
        var last = session.Attempts.OrderByDescending(x => x.AnsweredAt).FirstOrDefault();
        if (pool.Length > 1) pool = pool.Where(q => q.Id != last?.QuestionId).ToArray();
        var used = session.Attempts.Select(a => a.QuestionId).ToHashSet();
        var previousDifficulty = topic.Questions.FirstOrDefault(q => q.Id == last?.QuestionId)?.Difficulty ?? 4;
        return pool.OrderBy(q => reinforcement && q.Difficulty > previousDifficulty ? 1 : 0)
            .ThenBy(q => used.Contains(q.Id) ? 1 : 0).ThenBy(_ => Random.Shared.Next()).First();
    }
}
