using ChallengePrototype.Models;

namespace ChallengePrototype.Services;

public sealed class HealthProgressionOptions
{
    public double MinHealth { get; init; } = 0;
    public double MaxHealth { get; init; } = 100;
    public double StartingHealth { get; init; } = 50;
    public double PromotionMinimumFraction { get; init; } = .75;
    public IReadOnlyDictionary<GameDifficulty, double> QuestionLevelBase { get; init; } = new Dictionary<GameDifficulty, double> { [GameDifficulty.Easy] = 1, [GameDifficulty.Medium] = 1.25, [GameDifficulty.Hard] = 1.5, [GameDifficulty.VeryHard] = 1.75 };
    public IReadOnlyDictionary<int, double> ImportanceQuestionBonus { get; init; } = new Dictionary<int, double> { [1] = 0, [2] = .4, [3] = .8, [4] = 1.2, [5] = 1.6 };
    public IReadOnlyDictionary<GameDifficulty, IReadOnlyDictionary<GameDifficulty, double>> DifficultyDistribution { get; init; } =
        new Dictionary<GameDifficulty, IReadOnlyDictionary<GameDifficulty, double>>
        {
            [GameDifficulty.Easy] = new Dictionary<GameDifficulty, double> { [GameDifficulty.Easy] = .65, [GameDifficulty.Medium] = .20, [GameDifficulty.Hard] = .10, [GameDifficulty.VeryHard] = .05 },
            [GameDifficulty.Medium] = new Dictionary<GameDifficulty, double> { [GameDifficulty.Easy] = .20, [GameDifficulty.Medium] = .50, [GameDifficulty.Hard] = .20, [GameDifficulty.VeryHard] = .10 },
            [GameDifficulty.Hard] = new Dictionary<GameDifficulty, double> { [GameDifficulty.Easy] = .10, [GameDifficulty.Medium] = .20, [GameDifficulty.Hard] = .50, [GameDifficulty.VeryHard] = .20 },
            [GameDifficulty.VeryHard] = new Dictionary<GameDifficulty, double> { [GameDifficulty.Easy] = .05, [GameDifficulty.Medium] = .10, [GameDifficulty.Hard] = .25, [GameDifficulty.VeryHard] = .60 }
        };
    public IReadOnlyDictionary<GameDifficulty, double> BaseCorrectGain { get; init; } = new Dictionary<GameDifficulty, double> { [GameDifficulty.Easy] = 7, [GameDifficulty.Medium] = 8, [GameDifficulty.Hard] = 9, [GameDifficulty.VeryHard] = 10 };
    public IReadOnlyDictionary<int, double> ImportanceGainWeight { get; init; } = new Dictionary<int, double> { [1] = .9, [2] = .95, [3] = 1, [4] = 1.05, [5] = 1.1 };
    public IReadOnlyDictionary<GameDifficulty, double> BaseWrongDamage { get; init; } = new Dictionary<GameDifficulty, double> { [GameDifficulty.Easy] = 6.5, [GameDifficulty.Medium] = 6.25, [GameDifficulty.Hard] = 6, [GameDifficulty.VeryHard] = 5.75 };
    public IReadOnlyDictionary<int, double> ImportanceDamageMultiplier { get; init; } = new Dictionary<int, double> { [1] = .9, [2] = .95, [3] = 1, [4] = 1.05, [5] = 1.1 };
    public IReadOnlyDictionary<GameDifficulty, double> BaseTimeDecay { get; init; } = new Dictionary<GameDifficulty, double> { [GameDifficulty.Easy] = .40, [GameDifficulty.Medium] = .32, [GameDifficulty.Hard] = .25, [GameDifficulty.VeryHard] = .20 };
    public double MinimumCorrectNetGain { get; init; } = 1;
    public IReadOnlyDictionary<int, double> ImportanceTimeMultiplier { get; init; } = new Dictionary<int, double> { [1] = .9, [2] = .96, [3] = 1, [4] = 1.07, [5] = 1.15 };
    public IReadOnlyDictionary<GameDifficulty, int> GracePeriodMs { get; init; } = new Dictionary<GameDifficulty, int> { [GameDifficulty.Easy] = 2000, [GameDifficulty.Medium] = 2500, [GameDifficulty.Hard] = 3500, [GameDifficulty.VeryHard] = 4500 };
}

public sealed record HealthResult(double PreviousHealth, double NewHealth, double TimeLoss, double AnswerEffect, bool IsCorrect)
{
    public double SignedDelta => NewHealth - PreviousHealth;
    public bool IsDepleted => NewHealth <= 0;
    public bool IsFull => NewHealth >= 100;
}

public sealed class ChallengeHealthEngine(HealthProgressionOptions options)
{
    public HealthProgressionOptions Options { get; } = options;
    public int GetTargetQuestions(GameDifficulty level, int importance) => Math.Clamp((int)Math.Floor(Options.QuestionLevelBase[level] + Options.ImportanceQuestionBonus[Tier(importance)] + .5), 1, 4);
    public int GetMinimumEvidence(int targetCount) => (int)Math.Ceiling(targetCount * Options.PromotionMinimumFraction);
    public double GetGainWeight(int importance) => Options.ImportanceGainWeight[Tier(importance)];
    public double GetCorrectGain(GameDifficulty challengeLevel, GameDifficulty actualDifficulty, int importance) =>
        Options.BaseCorrectGain[actualDifficulty] * GetGainWeight(importance) * GetContextMultiplier(challengeLevel, actualDifficulty);
    public double GetWrongDamage(GameDifficulty actualDifficulty, int importance) => Options.BaseWrongDamage[actualDifficulty] * Options.ImportanceDamageMultiplier[Tier(importance)];
    public double GetTimeDecayPerSecond(GameDifficulty actualDifficulty, int importance) => Options.BaseTimeDecay[actualDifficulty] * Options.ImportanceTimeMultiplier[Tier(importance)];
    public int GetGracePeriodMs(GameDifficulty actualDifficulty) => Options.GracePeriodMs[actualDifficulty];
    public double GetTimeLoss(TimeSpan activeTime, GameDifficulty actualDifficulty, int importance) => Math.Max(0, activeTime.TotalSeconds - GetGracePeriodMs(actualDifficulty) / 1000d) * GetTimeDecayPerSecond(actualDifficulty, importance);
    public TimeSpan GetTimeToDepletion(double health, GameDifficulty actualDifficulty, int importance) => TimeSpan.FromMilliseconds(GetGracePeriodMs(actualDifficulty) + Math.Max(0, health) / GetTimeDecayPerSecond(actualDifficulty, importance) * 1000);
    public HealthResult ApplyAnswer(double health, TimeSpan activeTime, GameDifficulty challengeLevel, GameDifficulty actualDifficulty, int importance, bool correct, bool evidenceGateOpen)
    {
        var previous = Clamp(health, true);
        var rawTimeLoss = GetTimeLoss(activeTime, actualDifficulty, importance);
        var effect = correct ? GetCorrectGain(challengeLevel, actualDifficulty, importance) : GetWrongDamage(actualDifficulty, importance);
        // Time remains visible and consequential while thinking, but a correct answer must never be a negative event.
        var timeLoss = correct ? Math.Min(rawTimeLoss, Math.Max(0, effect - Options.MinimumCorrectNetGain)) : rawTimeLoss;
        var calculated = previous - timeLoss + (correct ? effect : -effect);
        if (correct) calculated = Math.Max(calculated, previous + Options.MinimumCorrectNetGain);
        var next = Clamp(calculated, evidenceGateOpen);
        return new(previous, next, timeLoss, effect, correct);
    }
    public double Clamp(double value, bool evidenceGateOpen) => Math.Clamp(value, Options.MinHealth, evidenceGateOpen ? Options.MaxHealth : 99);
    private static int Tier(int value) => Math.Clamp(value, 1, 5);
    private static double GetContextMultiplier(GameDifficulty challenge, GameDifficulty actual) => ((int)actual - (int)challenge) switch
    {
        0 => 1, -1 => .85, -2 => .70, <= -3 => .60, 1 => 1.10, _ => 1.15
    };
}
