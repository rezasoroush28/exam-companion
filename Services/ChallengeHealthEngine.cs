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
    public IReadOnlyDictionary<GameDifficulty, double> PositiveHealthBudget { get; init; } = new Dictionary<GameDifficulty, double> { [GameDifficulty.Easy] = 58, [GameDifficulty.Medium] = 60, [GameDifficulty.Hard] = 62, [GameDifficulty.VeryHard] = 64 };
    public IReadOnlyDictionary<int, double> ImportanceGainWeight { get; init; } = new Dictionary<int, double> { [1] = .9, [2] = .95, [3] = 1, [4] = 1.05, [5] = 1.1 };
    public IReadOnlyDictionary<GameDifficulty, double> BaseWrongDamage { get; init; } = new Dictionary<GameDifficulty, double> { [GameDifficulty.Easy] = 14, [GameDifficulty.Medium] = 11, [GameDifficulty.Hard] = 9, [GameDifficulty.VeryHard] = 7.5 };
    public IReadOnlyDictionary<int, double> ImportanceDamageMultiplier { get; init; } = new Dictionary<int, double> { [1] = .85, [2] = 1, [3] = 1.15, [4] = 1.3, [5] = 1.5 };
    public IReadOnlyDictionary<GameDifficulty, double> BaseTimeDecay { get; init; } = new Dictionary<GameDifficulty, double> { [GameDifficulty.Easy] = .055, [GameDifficulty.Medium] = .045, [GameDifficulty.Hard] = .035, [GameDifficulty.VeryHard] = .028 };
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
    public double GetCorrectGain(GameDifficulty level, int importance, double totalGainWeight) => Options.PositiveHealthBudget[level] * GetGainWeight(importance) / Math.Max(.001, totalGainWeight);
    public double GetWrongDamage(GameDifficulty level, int importance) => Options.BaseWrongDamage[level] * Options.ImportanceDamageMultiplier[Tier(importance)];
    public double GetTimeDecayPerSecond(GameDifficulty level, int importance) => Options.BaseTimeDecay[level] * Options.ImportanceTimeMultiplier[Tier(importance)];
    public int GetGracePeriodMs(GameDifficulty level) => Options.GracePeriodMs[level];
    public double GetTimeLoss(TimeSpan activeTime, GameDifficulty level, int importance) => Math.Max(0, activeTime.TotalSeconds - GetGracePeriodMs(level) / 1000d) * GetTimeDecayPerSecond(level, importance);
    public TimeSpan GetTimeToDepletion(double health, GameDifficulty level, int importance) => TimeSpan.FromMilliseconds(GetGracePeriodMs(level) + Math.Max(0, health) / GetTimeDecayPerSecond(level, importance) * 1000);
    public HealthResult ApplyAnswer(double health, TimeSpan activeTime, GameDifficulty level, int importance, bool correct, double totalGainWeight, bool evidenceGateOpen)
    {
        var previous = Clamp(health, true);
        var timeLoss = GetTimeLoss(activeTime, level, importance);
        var effect = correct ? GetCorrectGain(level, importance, totalGainWeight) : GetWrongDamage(level, importance);
        var next = Clamp(previous - timeLoss + (correct ? effect : -effect), evidenceGateOpen);
        return new(previous, next, timeLoss, effect, correct);
    }
    public double Clamp(double value, bool evidenceGateOpen) => Math.Clamp(value, Options.MinHealth, evidenceGateOpen ? Options.MaxHealth : 99);
    private static int Tier(int value) => Math.Clamp(value, 1, 5);
}
