using ChallengePrototype.Models;

namespace ChallengePrototype.Services;

public sealed class HealthGameOptions
{
    public double MinHealth { get; init; } = 0;
    public double MaxHealth { get; init; } = 100;
    public double StartingHealth { get; init; } = 70;
    public int GracePeriodMs { get; init; } = 2000;
    public double BaseWrongDamage { get; init; } = 5;
    public double BaseCorrectGain { get; init; } = 8;
    public double BaseTimeDecay { get; init; } = 0.8;
    public IReadOnlyDictionary<GameDifficulty, double> DifficultyPenalty { get; init; } =
        new Dictionary<GameDifficulty, double> { [GameDifficulty.Easy] = 1, [GameDifficulty.Medium] = .8, [GameDifficulty.Hard] = .62, [GameDifficulty.VeryHard] = .5 };
    public IReadOnlyDictionary<GameDifficulty, double> DifficultyReward { get; init; } =
        new Dictionary<GameDifficulty, double> { [GameDifficulty.Easy] = 1, [GameDifficulty.Medium] = 1.25, [GameDifficulty.Hard] = 1.6, [GameDifficulty.VeryHard] = 2 };
    public IReadOnlyDictionary<GameDifficulty, double> DifficultyTime { get; init; } =
        new Dictionary<GameDifficulty, double> { [GameDifficulty.Easy] = 1, [GameDifficulty.Medium] = .85, [GameDifficulty.Hard] = .72, [GameDifficulty.VeryHard] = .6 };
    public double AnswerImportanceBase { get; init; } = .90;
    public double AnswerImportanceScale { get; init; } = .45;
    public double TimeImportanceBase { get; init; } = .90;
    public double TimeImportanceScale { get; init; } = .35;
}

public sealed record HealthResult(double PreviousHealth, double NewHealth, double TimeLoss,
    double AnswerEffect, bool IsCorrect)
{
    public double SignedDelta => NewHealth - PreviousHealth;
    public bool IsDepleted => NewHealth <= 0;
}

public sealed class ChallengeHealthEngine(HealthGameOptions options)
{
    public HealthGameOptions Options { get; } = options;

    public double GetTimeDecayPerSecond(GameDifficulty difficulty, int importance) =>
        Options.BaseTimeDecay * Options.DifficultyTime[difficulty]
        * (Options.TimeImportanceBase + Options.TimeImportanceScale * NormalizeImportance(importance));

    public double GetWrongDamage(GameDifficulty difficulty, int importance) => RoundWhole(
        Options.BaseWrongDamage * Options.DifficultyPenalty[difficulty]
        * (Options.AnswerImportanceBase + Options.AnswerImportanceScale * NormalizeImportance(importance)));

    public double GetCorrectGain(GameDifficulty difficulty, int importance) => RoundWhole(
        Options.BaseCorrectGain * Options.DifficultyReward[difficulty]
        * (Options.AnswerImportanceBase + Options.AnswerImportanceScale * NormalizeImportance(importance)));

    public double GetTimeLoss(TimeSpan activeAnswerTime, GameDifficulty difficulty, int importance)
    {
        var chargeableSeconds = Math.Max(0, activeAnswerTime.TotalSeconds - Options.GracePeriodMs / 1000d);
        return chargeableSeconds * GetTimeDecayPerSecond(difficulty, importance);
    }

    public TimeSpan GetTimeToDepletion(double currentHealth, GameDifficulty difficulty, int importance)
    {
        var seconds = Math.Max(0, currentHealth) / GetTimeDecayPerSecond(difficulty, importance);
        return TimeSpan.FromMilliseconds(Options.GracePeriodMs + seconds * 1000);
    }

    public HealthResult ApplyAnswer(double currentHealth, TimeSpan activeAnswerTime,
        GameDifficulty difficulty, int importance, bool isCorrect)
    {
        var previous = Clamp(currentHealth);
        var timeLoss = GetTimeLoss(activeAnswerTime, difficulty, importance);
        var effect = isCorrect ? GetCorrectGain(difficulty, importance) : GetWrongDamage(difficulty, importance);
        var next = Clamp(previous - timeLoss + (isCorrect ? effect : -effect));
        return new HealthResult(previous, next, timeLoss, effect, isCorrect);
    }

    public double Clamp(double value) => Math.Clamp(value, Options.MinHealth, Options.MaxHealth);
    private static double NormalizeImportance(int importance) => (Math.Clamp(importance, 1, 5) - 1) / 4d;
    private static double RoundWhole(double value) => Math.Round(value, MidpointRounding.AwayFromZero);
}
