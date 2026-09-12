namespace ExamCompanion.Domain.Music;

public enum QuestionType { Educational, Challenge }
public enum GameSessionStatus { Active, Completed }
public enum TopicProgressStatus { Locked, Active, InProgress, CompletedClean, CompletedWithScratch, Repaired }
public enum GameStepType { Educational, Reinforcement, Challenge, RepairEducational, RepairChallenge, Complete }
public enum AnswerResultType { Correct, Reinforcement, CycleCompleted, TopicCompleted, Scratched, Repaired, RepairFailed, LessonCompleted }

public sealed class Exam
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "آلبوم زیست‌شناسی";
    public DateTime ExamDate { get; set; }
}
public sealed class Lesson
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public long SourceId { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public List<Topic> Topics { get; set; } = [];
}
public sealed class Topic
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LessonId { get; set; }
    public Lesson Lesson { get; set; } = null!;
    public long SourceId { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public double Importance { get; set; }
    public int DisplayOrder { get; set; }
    public string MusicalKey { get; set; } = "C";
    public string MusicalColor { get; set; } = "#beee72";
    public List<Question> Questions { get; set; } = [];
}
public sealed class Question
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TopicId { get; set; }
    public Topic Topic { get; set; } = null!;
    public string SourceId { get; set; } = "";
    public QuestionType Type { get; set; }
    public string Text { get; set; } = "";
    public string OptionA { get; set; } = "";
    public string OptionB { get; set; } = "";
    public string? OptionC { get; set; }
    public string? OptionD { get; set; }
    public string CorrectOption { get; set; } = "A";
    public int Difficulty { get; set; }
    public string? Explanation { get; set; }
}
public sealed class GameSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LessonId { get; set; }
    public Lesson Lesson { get; set; } = null!;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public Guid CurrentTopicId { get; set; }
    public int CurrentCycleIndex { get; set; }
    public int CurrentQuestionIndex { get; set; }
    public GameSessionStatus Status { get; set; }
    public GameStepType Step { get; set; }
    public Guid? CurrentQuestionId { get; set; }
    public Guid TurnId { get; set; } = Guid.NewGuid();
    public Guid Revision { get; set; } = Guid.NewGuid();
    public int EducationalTarget { get; set; } = 2;
    public int EducationalAnswered { get; set; }
    public int ChallengeAttempts { get; set; }
    public Guid? RepairTopicId { get; set; }
    public GameStepType? SuspendedStep { get; set; }
    public Guid? SuspendedQuestionId { get; set; }
    public bool RepairEducationalCorrect { get; set; }
    public List<TopicProgress> Topics { get; set; } = [];
    public List<QuestionAttempt> Attempts { get; set; } = [];
}
public sealed class TopicProgress
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GameSessionId { get; set; }
    public Guid TopicId { get; set; }
    public int RequiredCycles { get; set; }
    public int CompletedCycles { get; set; }
    public TopicProgressStatus Status { get; set; }
    public int TotalWrongAnswers { get; set; }
    public int ReinforcementCount { get; set; }
    public bool HasScratch { get; set; }
    public bool IsRepaired { get; set; }
    public DateTime? CompletedAt { get; set; }
}
public sealed class QuestionAttempt
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GameSessionId { get; set; }
    public Guid TopicId { get; set; }
    public Guid QuestionId { get; set; }
    public Guid TurnId { get; set; }
    public bool IsCorrect { get; set; }
    public string SelectedOption { get; set; } = "";
    public DateTime AnsweredAt { get; set; } = DateTime.UtcNow;
    public int CycleIndex { get; set; }
    public bool WasReinforcement { get; set; }
    public bool WasRepair { get; set; }
}
public sealed class GameRulesOptions
{
    public int BaseEducationalQuestionCount { get; set; } = 2;
    public int MaxEducationalQuestionCount { get; set; } = 4;
    public int MaxChallengeAttempts { get; set; } = 2;
    public int WrongAnswersForScratch { get; set; } = 3;
    public int ReinforcementsForScratch { get; set; } = 3;
    public static int Cycles(double importance) => importance < .34 ? 1 : importance < .67 ? 2 : 3;
}
