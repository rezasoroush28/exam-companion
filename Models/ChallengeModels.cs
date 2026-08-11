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

public sealed record ResultCube(long CubeId, long QuestionId, long TopicId, int ImportanceTier,
    GameDifficulty Difficulty, bool IsCorrect);

public static class BonusConfiguration
{
    public const int BaseBonus = 10;
    private static readonly double[] ImportanceMultipliers = [1.0, 1.25, 1.5, 1.8, 2.2];

    public static int ImportanceTier(double importance) => importance switch
    {
        <= 0.08 => 1,
        <= 0.16 => 2,
        <= 0.25 => 3,
        <= 0.36 => 4,
        _ => 5
    };

    public static double HardnessMultiplier(GameDifficulty difficulty) => difficulty switch
    {
        GameDifficulty.Easy => 1.0,
        GameDifficulty.Medium => 1.4,
        GameDifficulty.Hard => 2.0,
        GameDifficulty.VeryHard => 3.0,
        _ => 1.0
    };

    public static int Calculate(ResultCube cube) => (int)Math.Round(
        BaseBonus * ImportanceMultipliers[Math.Clamp(cube.ImportanceTier, 1, 5) - 1]
        * HardnessMultiplier(cube.Difficulty), MidpointRounding.AwayFromZero);
}
