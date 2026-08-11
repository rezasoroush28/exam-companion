namespace ChallengePrototype.Models;

public enum GameDifficulty { Easy, Medium, Hard, VeryHard }

public static class DifficultyExtensions
{
    public static string Label(this GameDifficulty value) => value switch
    {
        GameDifficulty.Easy => "آسان",
        GameDifficulty.Medium => "متوسط",
        GameDifficulty.Hard => "سخت",
        _ => "خیلی سخت"
    };
    public static string CssName(this GameDifficulty value) => value.ToString().ToLowerInvariant();
}

public sealed record GameTopic(long Id, string Title, double Importance);
public sealed record AnswerOption(string Option, string Html);
public sealed record ChallengeQuestion(long Id, GameTopic Topic, string TitleHtml, IReadOnlyList<AnswerOption> Options, string CorrectOption, GameDifficulty RequestedDifficulty, string? DatabaseLevel, bool IsDifficultyFallback);
public sealed record ChallengeSetup(string LessonTitle, IReadOnlyList<GameTopic> Topics, IReadOnlyList<ChallengeQuestion> Questions);

public static class GameTiming
{
    public const int InitialPauseMs = 250;
    public const int FeedbackMs = 650;
    public const int BetweenQuestionsMs = 220;
}
