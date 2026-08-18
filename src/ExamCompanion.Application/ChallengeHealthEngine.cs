using ChallengePrototype.Models;

namespace ChallengePrototype.Services;

public sealed class HealthProgressionOptions
{
    public double EarlyEvidenceCap { get; init; } = 99;
    public double MinimumCorrectNetGain { get; init; } = 1;
    public double MinimumEvidenceFraction { get; init; } = .75;
}

public sealed record HealthResult(double PreviousHealth, double NewHealth, double TimeLoss, double AnswerEffect,
    bool IsCorrect, double FailureHealth = 0, double PromotionHealth = 100)
{
    public double SignedDelta => NewHealth - PreviousHealth;
    public bool IsDepleted => NewHealth <= FailureHealth;
    public bool IsFull => NewHealth >= PromotionHealth;
}

public sealed class ChallengeHealthEngine(HealthProgressionOptions options)
{
    public HealthProgressionOptions Options { get; } = options;

    public int GetMinimumEvidence(int targetCount) =>
        (int)Math.Ceiling(targetCount * Options.MinimumEvidenceFraction);

    public double GetCorrectTopicFactor(ChallengeHealthPattern pattern, int importance) => Math.Clamp(
        1 + (pattern.ImportanceSensitivity * .60 * GetImportanceDeviation(pattern, importance)), .90, 1.15);

    public double GetWrongTopicFactor(ChallengeHealthPattern pattern, int importance) => Math.Clamp(
        1 + (pattern.ImportanceSensitivity * GetImportanceDeviation(pattern, importance)), .80, 1.25);

    public double GetTimeTopicFactor(ChallengeHealthPattern pattern, int importance) => Math.Clamp(
        1 + (pattern.ImportanceSensitivity * .35 * GetImportanceDeviation(pattern, importance)), .90, 1.10);

    public double GetCorrectGain(ChallengeHealthPattern pattern, GameDifficulty actualDifficulty, int importance)
    {
        var value = pattern.BaseCorrectGain * GetCorrectTopicFactor(pattern, importance)
            * GetFactor(pattern, actualDifficulty).CorrectMultiplier;
        return Math.Clamp(value, pattern.HealthUnit * .50, pattern.HealthUnit * 3);
    }

    public double GetWrongDamage(ChallengeHealthPattern pattern, GameDifficulty actualDifficulty, int importance)
    {
        var value = pattern.BaseWrongDamage * GetWrongTopicFactor(pattern, importance)
            * GetFactor(pattern, actualDifficulty).WrongMultiplier;
        return Math.Clamp(value, pattern.HealthUnit * .40, pattern.HealthUnit * 2.50);
    }

    public double GetQuestionTimeBudget(ChallengeHealthPattern pattern, GameDifficulty actualDifficulty, int importance) =>
        pattern.BaseTimeBudgetPerQuestion * GetTimeTopicFactor(pattern, importance)
        * GetFactor(pattern, actualDifficulty).TimeMultiplier;

    public double GetTimeDecayPerSecond(ChallengeHealthPattern pattern, GameDifficulty actualDifficulty, int importance)
    {
        var factor = GetFactor(pattern, actualDifficulty);
        if (factor.PressureWindowSeconds <= 0)
            throw new InvalidOperationException("A health difficulty factor must have a positive pressure window.");
        return GetQuestionTimeBudget(pattern, actualDifficulty, importance) / factor.PressureWindowSeconds;
    }

    public int GetGracePeriodMs(ChallengeHealthPattern pattern, GameDifficulty actualDifficulty) =>
        GetFactor(pattern, actualDifficulty).GracePeriodMilliseconds;

    public double GetTimeLoss(ChallengeHealthPattern pattern, TimeSpan activeTime,
        GameDifficulty actualDifficulty, int importance) => Math.Max(0,
        activeTime.TotalSeconds - GetGracePeriodMs(pattern, actualDifficulty) / 1000d)
        * GetTimeDecayPerSecond(pattern, actualDifficulty, importance);

    public TimeSpan GetTimeToDepletion(ChallengeHealthPattern pattern, double health,
        GameDifficulty actualDifficulty, int importance)
    {
        var decay = GetTimeDecayPerSecond(pattern, actualDifficulty, importance);
        if (decay <= 0) return Timeout.InfiniteTimeSpan;
        var healthDistance = Math.Max(0, health - pattern.FailureHealth);
        return TimeSpan.FromMilliseconds(GetGracePeriodMs(pattern, actualDifficulty)
            + healthDistance / decay * 1000);
    }

    public HealthResult ApplyAnswer(ChallengeHealthPattern pattern, double health, TimeSpan activeTime,
        GameDifficulty actualDifficulty, int importance, bool correct, bool evidenceGateOpen)
    {
        var previous = Clamp(pattern, health, true);
        var timeLoss = GetTimeLoss(pattern, activeTime, actualDifficulty, importance);
        var effect = correct
            ? GetCorrectGain(pattern, actualDifficulty, importance)
            : GetWrongDamage(pattern, actualDifficulty, importance);
        var calculated = previous - timeLoss + (correct ? effect : -effect);

        // Correctness remains an authoritative positive event even after a long valid thinking period.
        if (correct)
            calculated = Math.Max(calculated, previous + Options.MinimumCorrectNetGain);

        var next = Clamp(pattern, calculated, evidenceGateOpen);
        return new(previous, next, timeLoss, effect, correct, pattern.FailureHealth, pattern.PromotionHealth);
    }

    public double Clamp(ChallengeHealthPattern pattern, double value, bool evidenceGateOpen)
    {
        var cap = evidenceGateOpen ? pattern.PromotionHealth : Math.Min(pattern.PromotionHealth, Options.EarlyEvidenceCap);
        return Math.Clamp(value, pattern.FailureHealth, cap);
    }

    private static double GetImportanceDeviation(ChallengeHealthPattern pattern, int importance) =>
        (Math.Clamp(importance, 1, 5) - pattern.AverageImportance) / 4d;

    private static ChallengeHealthDifficultyFactor GetFactor(ChallengeHealthPattern pattern, GameDifficulty difficulty) =>
        pattern.DifficultyFactors.SingleOrDefault(x => x.QuestionDifficulty == difficulty.ToQuestionDifficulty())
        ?? throw new InvalidOperationException($"Health pattern {pattern.Id} has no factor for {difficulty}.");
}
