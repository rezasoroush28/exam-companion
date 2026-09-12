using ChallengePrototype.Models;
using Microsoft.Extensions.Logging;

namespace ChallengePrototype.Services;

public sealed record TopicLevelProgress(GameTopic Topic, int TargetQuestionCount, int QuestionsAsked, int CorrectAnswers, int IncorrectAnswers)
{
    public double Accuracy => QuestionsAsked == 0 ? 0 : (double)CorrectAnswers / QuestionsAsked;
    public bool HasMinimumExposure => QuestionsAsked > 0;
}

public sealed class ChallengeEngine(IQuestionService questions, ChallengeHealthEngine health, ILevelDesignService levelDesigns, ILogger<ChallengeEngine> logger)
{
    private readonly Queue<ChallengeQuestion> _remaining = new();
    private readonly HashSet<long> _usedQuestionIds = [];
    private readonly Dictionary<long, TopicLevelProgress> _progress = [];
    private readonly Dictionary<GameDifficulty, int> _difficultyAsked = [];
    private IReadOnlyList<ChallengeQuestion> _bank = [];
    private LevelDesign? _levelDesign;
    private LevelBlueprint? _blueprint;
    private long _lessonChallengeId;
    private long _currentRunId;
    private long? _lastTopicId;
    public string LessonTitle { get; private set; } = "";
    public long LessonChallengeId => _lessonChallengeId;
    public IReadOnlyList<GameTopic> Topics { get; private set; } = [];
    public GameDifficulty CurrentDifficulty { get; private set; } = GameDifficulty.Easy;
    public ChallengeHealthPattern CurrentHealthPattern { get; private set; } = null!;
    public int QuestionsPresented { get; private set; }
    public int QuestionsAnswered { get; private set; }
    public int LevelTargetCount { get; private set; }
    public int MinimumEvidenceRequired => _blueprint?.EvidenceRequired ?? health.GetMinimumEvidence(LevelTargetCount);
    public int DominantDifficultyTarget { get; private set; }
    public int DominantDifficultyRequired => (int)Math.Ceiling(DominantDifficultyTarget * .5);
    public int DominantDifficultyAnswered => _difficultyAsked.GetValueOrDefault(CurrentDifficulty);
    public bool IsEvidenceGateOpen => QuestionsAnswered >= MinimumEvidenceRequired && _progress.Values.All(x => x.HasMinimumExposure)
        && DominantDifficultyAnswered >= DominantDifficultyRequired;
    public bool IsScheduleExhausted => _remaining.Count == 0;
    public IReadOnlyCollection<TopicLevelProgress> TopicProgress => _progress.Values;

    public void BeginRun(double health) => _currentRunId = levelDesigns.BeginRun(_lessonChallengeId, CurrentDifficulty.ToChallengeLevel(), health);
    public void PersistAttempt(ChallengeQuestion question, string? answer, TimeSpan thinkingTime, HealthResult result) =>
        levelDesigns.RecordAttempt(_currentRunId, _lessonChallengeId, question, answer, (long)thinkingTime.TotalMilliseconds,
            result.PreviousHealth, result.TimeLoss, result.AnswerEffect, result.NewHealth, result.IsCorrect);
    public void EndRun(double health, ChallengeRunStatus status, string reason)
    {
        if (_currentRunId == 0) return;
        levelDesigns.EndRun(_currentRunId, health, status, reason); _currentRunId = 0;
    }
    public void CompleteLessonChallenge() => levelDesigns.CompleteLessonChallenge(_lessonChallengeId);

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        var setup = await questions.CreateFixedSetupAsync(cancellationToken);
        LessonTitle = setup.LessonTitle; Topics = setup.Topics; _bank = setup.Questions;
        _levelDesign = await levelDesigns.GetActiveDesignAsync(cancellationToken);
        var lessonChallenge = await levelDesigns.GetOrCreateLessonChallengeAsync(setup, _levelDesign, cancellationToken);
        if (lessonChallenge.LevelDesign is { LevelRules.Count: > 0 })
            _levelDesign = lessonChallenge.LevelDesign;
        _lessonChallengeId = lessonChallenge.Id;
        _usedQuestionIds.Clear(); StartLevel(lessonChallenge.CurrentLevel.ToGameDifficulty());
    }

    public void StartLevel(GameDifficulty difficulty)
    {
        CurrentDifficulty = difficulty; QuestionsPresented = 0; QuestionsAnswered = 0; _lastTopicId = null;
        _remaining.Clear(); _progress.Clear(); _difficultyAsked.Clear();
        var rule = _levelDesign?.LevelRules.Single(x => x.ChallengeLevel == difficulty.ToChallengeLevel())
            ?? throw new InvalidOperationException("Level design has not been loaded.");
        _blueprint = levelDesigns.Generate(rule, Topics.Select(topic => (topic.Id, BonusConfiguration.ImportanceTier(topic.Importance))).ToArray());
        foreach (var topic in Topics)
        {
            var target = _blueprint.Topics.Single(x => x.TopicId == topic.Id).QuestionQuota;
            _progress[topic.Id] = new(topic, target, 0, 0, 0);
        }
        LevelTargetCount = _progress.Values.Sum(x => x.TargetQuestionCount);
        CurrentHealthPattern = levelDesigns.GetOrCreateHealthPattern(_lessonChallengeId, _blueprint);
        BuildSchedule();
    }

    public bool AdvanceLevel(bool persist = true)
    {
        if (CurrentDifficulty == GameDifficulty.VeryHard) return false;
        var next = CurrentDifficulty + 1;
        if (persist) levelDesigns.SetCurrentLevel(_lessonChallengeId, next.ToChallengeLevel());
        StartLevel(next); return true;
    }

    public Task<ChallengeQuestion?> NextQuestionAsync()
    {
        var continued = _remaining.Count == 0;
        var question = continued ? SelectOvertimeQuestion() : _remaining.Dequeue();
        if (question is null) return Task.FromResult<ChallengeQuestion?>(null);
        QuestionsPresented++; _lastTopicId = question.Topic.Id; _usedQuestionIds.Add(question.Id);
        if (continued) logger.LogInformation("Level {Level} entered adaptive continuation after {Count} scheduled questions.", CurrentDifficulty, LevelTargetCount);
        return Task.FromResult<ChallengeQuestion?>(question);
    }

    public void RecordAnswer(long topicId, GameDifficulty actualDifficulty, bool correct)
    {
        QuestionsAnswered++;
        var item = _progress[topicId];
        _difficultyAsked[actualDifficulty] = _difficultyAsked.GetValueOrDefault(actualDifficulty) + 1;
        _progress[topicId] = item with { QuestionsAsked = item.QuestionsAsked + 1, CorrectAnswers = item.CorrectAnswers + (correct ? 1 : 0), IncorrectAnswers = item.IncorrectAnswers + (correct ? 0 : 1) };
    }

    private void BuildSchedule()
    {
        var slots = _progress.Values.SelectMany(x => Enumerable.Repeat(x.Topic, x.TargetQuestionCount)).ToList();
        var difficulties = BuildDifficultySlots(LevelTargetCount);
        DominantDifficultyTarget = difficulties.Count(x => x == CurrentDifficulty);
        var localUsed = new HashSet<long>();
        foreach (var pair in Interleave(slots).Zip(InterleaveDifficulties(difficulties)))
        {
            var selected = SelectQuestion(pair.First, pair.Second, localUsed);
            if (selected is not null) { _remaining.Enqueue(selected); localUsed.Add(selected.Id); }
        }
    }

    public IReadOnlyDictionary<GameDifficulty, int> GetDifficultyAllocation(int count)
    {
        if (_blueprint is null) throw new InvalidOperationException("Level blueprint has not been generated.");
        return levelDesigns.AllocateDifficulties(_blueprint.Rule, count)
            .ToDictionary(x => x.Key.ToGameDifficulty(), x => x.Value);
    }

    private List<GameDifficulty> BuildDifficultySlots(int count) => GetDifficultyAllocation(count)
        .SelectMany(x => Enumerable.Repeat(x.Key, x.Value)).ToList();

    private static IReadOnlyList<GameDifficulty> InterleaveDifficulties(List<GameDifficulty> slots)
    {
        var result = new List<GameDifficulty>(slots.Count);
        while (slots.Count > 0)
        {
            var choices = slots.Where(x => result.Count < 3 || result.TakeLast(3).Any(previous => previous != x)).ToArray();
            if (choices.Length == 0) choices = slots.ToArray();
            var chosen = choices[Random.Shared.Next(choices.Length)]; result.Add(chosen); slots.Remove(chosen);
        }
        return result;
    }

    private static IReadOnlyList<GameTopic> Interleave(List<GameTopic> slots)
    {
        var result = new List<GameTopic>(slots.Count);
        while (slots.Count > 0)
        {
            var choices = slots.Where(x => x.Id != result.LastOrDefault()?.Id).ToArray();
            if (choices.Length == 0) choices = slots.ToArray();
            var topic = choices[Random.Shared.Next(choices.Length)]; result.Add(topic); slots.Remove(topic);
        }
        return result;
    }

    private ChallengeQuestion? SelectQuestion(GameTopic topic, GameDifficulty desiredDifficulty, ISet<long>? localUsed = null)
    {
        var topicBank = _bank.Where(x => x.Topic.Id == topic.Id).ToArray();
        var exact = topicBank.Where(x => x.RequestedDifficulty == desiredDifficulty).ToArray();
        var candidates = exact.Length > 0 ? exact : topicBank.OrderBy(x => Math.Abs((int)x.RequestedDifficulty - (int)desiredDifficulty)).ToArray();
        if (exact.Length == 0) logger.LogWarning("No exact {Difficulty} question for topic {TopicId}; using closest bank level.", desiredDifficulty, topic.Id);
        return candidates.FirstOrDefault(x => !_usedQuestionIds.Contains(x.Id) && (localUsed is null || !localUsed.Contains(x.Id)))
            ?? candidates.FirstOrDefault(x => localUsed is null || !localUsed.Contains(x.Id)) ?? candidates.FirstOrDefault();
    }

    private ChallengeQuestion? SelectOvertimeQuestion()
    {
        var ranked = _progress.Values.OrderByDescending(x => (1 - x.Accuracy) * BonusConfiguration.ImportanceTier(x.Topic.Importance)
                * (_bank.Any(q => q.Topic.Id == x.Topic.Id && !_usedQuestionIds.Contains(q.Id)) ? 1.25 : 1))
            .ThenBy(_ => Random.Shared.Next()).Select(x => x.Topic).ToList();
        if (DominantDifficultyAnswered < DominantDifficultyRequired)
        {
            var dominantTopics = ranked.Where(topic => _bank.Any(q => q.Topic.Id == topic.Id && q.RequestedDifficulty == CurrentDifficulty)).ToList();
            var dominantTopic = dominantTopics.FirstOrDefault(x => x.Id != _lastTopicId) ?? dominantTopics.FirstOrDefault();
            if (dominantTopic is not null) return SelectQuestion(dominantTopic, CurrentDifficulty);
        }
        var topic = ranked.FirstOrDefault(x => x.Id != _lastTopicId) ?? ranked.FirstOrDefault();
        if (topic is null) return null;
        var difficulty = WeightedDifficulty();
        return SelectQuestion(topic, difficulty);
    }

    private GameDifficulty WeightedDifficulty()
    {
        var roll = Random.Shared.NextDouble();
        var cumulative = 0d;
        if (_blueprint is null) return CurrentDifficulty;
        foreach (var item in new[]
        {
            (GameDifficulty.Easy, _blueprint.Rule.EasyQuestionWeight),
            (GameDifficulty.Medium, _blueprint.Rule.MediumQuestionWeight),
            (GameDifficulty.Hard, _blueprint.Rule.HardQuestionWeight),
            (GameDifficulty.VeryHard, _blueprint.Rule.VeryHardQuestionWeight)
        })
        {
            cumulative += item.Item2;
            if (roll <= cumulative) return item.Item1;
        }
        return CurrentDifficulty;
    }
}
