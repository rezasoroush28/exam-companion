using ChallengePrototype.Models;

namespace ChallengePrototype.Services;

public interface IChallengeHealthPatternCalculator
{
    ChallengeHealthPattern Calculate(ChallengeLevelProgress progress, LevelRule rule,
        LevelBlueprint blueprint, IReadOnlyCollection<TopicProgress> topics);
}

public sealed class ChallengeHealthPatternCalculator : IChallengeHealthPatternCalculator
{
    public const int CurrentPatternVersion = 1;

    private static readonly IReadOnlyDictionary<ChallengeLevel, HealthCalibration> Calibrations =
        new Dictionary<ChallengeLevel, HealthCalibration>
        {
            [ChallengeLevel.Easy] = new(.78, 1.15, .12, .15),
            [ChallengeLevel.Medium] = new(.72, 1.05, .10, .20),
            [ChallengeLevel.Hard] = new(.66, .95, .08, .25),
            [ChallengeLevel.VeryHard] = new(.60, .85, .06, .30)
        };

    private static readonly IReadOnlyDictionary<QuestionDifficulty, RawDifficultyFactor> RawFactors =
        new Dictionary<QuestionDifficulty, RawDifficultyFactor>
        {
            [QuestionDifficulty.Easy] = new(.80, 1.30, 1.25, 2000, 25),
            [QuestionDifficulty.Medium] = new(.95, 1.10, 1.05, 2500, 35),
            [QuestionDifficulty.Hard] = new(1.15, .90, .90, 3500, 50),
            [QuestionDifficulty.VeryHard] = new(1.35, .75, .75, 4500, 70)
        };

    public ChallengeHealthPattern Calculate(ChallengeLevelProgress progress, LevelRule rule,
        LevelBlueprint blueprint, IReadOnlyCollection<TopicProgress> topics)
    {
        ArgumentNullException.ThrowIfNull(progress);
        ArgumentNullException.ThrowIfNull(rule);
        ArgumentNullException.ThrowIfNull(blueprint);
        ArgumentNullException.ThrowIfNull(topics);

        if (blueprint.TargetQuestionCount <= 0)
            throw new InvalidOperationException("A health pattern cannot be generated for a level with no questions.");
        if (topics.Count == 0)
            throw new InvalidOperationException("A health pattern requires at least one topic.");
        if (topics.Select(x => x.TopicId).Distinct().Count() != topics.Count)
            throw new InvalidOperationException("A health pattern cannot contain duplicate topics.");
        if (blueprint.ChallengeLevel != rule.ChallengeLevel || progress.ChallengeLevel != rule.ChallengeLevel)
            throw new InvalidOperationException("The level progress, rule, and blueprint must describe the same challenge level.");
        if (progress.TargetQuestionCount > 0 && progress.TargetQuestionCount != blueprint.TargetQuestionCount)
            throw new InvalidOperationException("Persisted level progress does not match the generated question target.");
        if (blueprint.DifficultyQuotas.Values.Sum() != blueprint.TargetQuestionCount)
            throw new InvalidOperationException("Generated difficulty quotas must equal the target question count.");

        var calibration = Calibrations[rule.ChallengeLevel];
        var promotionDistance = rule.PromotionHealth - rule.StartingHealth;
        if (promotionDistance <= 0 || rule.StartingHealth <= rule.FailureHealth)
            throw new InvalidOperationException("Health thresholds must satisfy Promotion > Starting > Failure.");

        var healthUnit = promotionDistance / blueprint.TargetQuestionCount;
        var correctFactor = (1 + ((1 - calibration.TargetAccuracy) * calibration.WrongSeverity)
            + calibration.ExpectedTimeBudgetFraction) / calibration.TargetAccuracy;
        var totalImportance = topics.Sum(x => Math.Clamp(x.ImportanceSnapshot, 1, 5));
        var pattern = new ChallengeHealthPattern
        {
            PatternVersion = CurrentPatternVersion,
            ChallengeLevel = rule.ChallengeLevel,
            TopicCount = topics.Count,
            TotalImportance = totalImportance,
            AverageImportance = (double)totalImportance / topics.Count,
            TargetQuestionCount = blueprint.TargetQuestionCount,
            StartingHealth = rule.StartingHealth,
            PromotionHealth = rule.PromotionHealth,
            FailureHealth = rule.FailureHealth,
            PromotionDistance = promotionDistance,
            HealthUnit = healthUnit,
            TargetAccuracy = calibration.TargetAccuracy,
            WrongSeverity = calibration.WrongSeverity,
            ExpectedTimeBudgetFraction = calibration.ExpectedTimeBudgetFraction,
            ImportanceSensitivity = calibration.ImportanceSensitivity,
            BaseCorrectGain = healthUnit * correctFactor,
            BaseWrongDamage = healthUnit * calibration.WrongSeverity,
            BaseTimeBudgetPerQuestion = healthUnit * calibration.ExpectedTimeBudgetFraction,
            GeneratedAt = DateTimeOffset.UtcNow
        };

        var shares = Enum.GetValues<QuestionDifficulty>().ToDictionary(difficulty => difficulty,
            difficulty => blueprint.DifficultyQuotas.GetValueOrDefault(difficulty) / (double)blueprint.TargetQuestionCount);
        var averageCorrect = WeightedAverage(shares, factor => factor.CorrectMultiplier);
        var averageWrong = WeightedAverage(shares, factor => factor.WrongMultiplier);
        var averageTime = WeightedAverage(shares, factor => factor.TimeMultiplier);

        pattern.DifficultyFactors.AddRange(Enum.GetValues<QuestionDifficulty>().Select(difficulty =>
        {
            var raw = RawFactors[difficulty];
            return new ChallengeHealthDifficultyFactor
            {
                QuestionDifficulty = difficulty,
                CorrectMultiplier = raw.CorrectMultiplier / averageCorrect,
                WrongMultiplier = raw.WrongMultiplier / averageWrong,
                TimeMultiplier = raw.TimeMultiplier / averageTime,
                GracePeriodMilliseconds = raw.GracePeriodMilliseconds,
                PressureWindowSeconds = raw.PressureWindowSeconds
            };
        }));

        return pattern;
    }

    private static double WeightedAverage(IReadOnlyDictionary<QuestionDifficulty, double> shares,
        Func<RawDifficultyFactor, double> selector)
    {
        var average = shares.Sum(item => item.Value * selector(RawFactors[item.Key]));
        if (average <= 0 || !double.IsFinite(average))
            throw new InvalidOperationException("Difficulty normalization requires a positive generated difficulty share.");
        return average;
    }

    private sealed record HealthCalibration(double TargetAccuracy, double WrongSeverity,
        double ExpectedTimeBudgetFraction, double ImportanceSensitivity);

    private sealed record RawDifficultyFactor(double CorrectMultiplier, double WrongMultiplier,
        double TimeMultiplier, int GracePeriodMilliseconds, double PressureWindowSeconds);
}
