using ChallengePrototype.Models;

namespace ChallengePrototype.Services;

public sealed record TopicLevelProgress(GameTopic Topic, int TargetQuestionCount, int QuestionsAsked, int CorrectAnswers, int IncorrectAnswers)
{
    public double Accuracy => QuestionsAsked == 0 ? 0 : (double)CorrectAnswers / QuestionsAsked;
    public bool HasMinimumExposure => QuestionsAsked > 0;
}

public sealed class ChallengeEngine(QuestionService questions, ChallengeHealthEngine health, ILogger<ChallengeEngine> logger)
{
    private readonly Queue<ChallengeQuestion> _remaining = new();
    private readonly HashSet<long> _usedQuestionIds = [];
    private readonly Dictionary<long, TopicLevelProgress> _progress = [];
    private IReadOnlyList<ChallengeQuestion> _bank = [];
    private long? _lastTopicId;
    public string LessonTitle { get; private set; } = "";
    public IReadOnlyList<GameTopic> Topics { get; private set; } = [];
    public GameDifficulty CurrentDifficulty { get; private set; } = GameDifficulty.Easy;
    public int QuestionsPresented { get; private set; }
    public int QuestionsAnswered { get; private set; }
    public int LevelTargetCount { get; private set; }
    public int MinimumEvidenceRequired => health.GetMinimumEvidence(LevelTargetCount);
    public double TotalGainWeight { get; private set; }
    public bool IsEvidenceGateOpen => QuestionsAnswered >= MinimumEvidenceRequired && _progress.Values.All(x => x.HasMinimumExposure);
    public bool IsScheduleExhausted => _remaining.Count == 0;
    public IReadOnlyCollection<TopicLevelProgress> TopicProgress => _progress.Values;

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        var setup = await questions.CreateFixedSetupAsync(cancellationToken);
        LessonTitle = setup.LessonTitle; Topics = setup.Topics; _bank = setup.Questions;
        _usedQuestionIds.Clear(); StartLevel(GameDifficulty.Easy);
    }

    public void StartLevel(GameDifficulty difficulty)
    {
        CurrentDifficulty = difficulty; QuestionsPresented = 0; QuestionsAnswered = 0; _lastTopicId = null;
        _remaining.Clear(); _progress.Clear();
        foreach (var topic in Topics)
        {
            var target = health.GetTargetQuestions(difficulty, BonusConfiguration.ImportanceTier(topic.Importance));
            _progress[topic.Id] = new(topic, target, 0, 0, 0);
        }
        LevelTargetCount = _progress.Values.Sum(x => x.TargetQuestionCount);
        TotalGainWeight = _progress.Values.Sum(x => x.TargetQuestionCount * health.GetGainWeight(BonusConfiguration.ImportanceTier(x.Topic.Importance)));
        BuildSchedule();
    }

    public bool AdvanceLevel()
    {
        if (CurrentDifficulty == GameDifficulty.VeryHard) return false;
        StartLevel(CurrentDifficulty + 1); return true;
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

    public void RecordAnswer(long topicId, bool correct)
    {
        QuestionsAnswered++;
        var item = _progress[topicId];
        _progress[topicId] = item with { QuestionsAsked = item.QuestionsAsked + 1, CorrectAnswers = item.CorrectAnswers + (correct ? 1 : 0), IncorrectAnswers = item.IncorrectAnswers + (correct ? 0 : 1) };
    }

    private void BuildSchedule()
    {
        var slots = _progress.Values.SelectMany(x => Enumerable.Repeat(x.Topic, x.TargetQuestionCount)).ToList();
        var localUsed = new HashSet<long>();
        foreach (var topic in Interleave(slots))
        {
            var selected = SelectQuestion(topic, localUsed);
            if (selected is not null) { _remaining.Enqueue(selected); localUsed.Add(selected.Id); }
        }
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

    private ChallengeQuestion? SelectQuestion(GameTopic topic, ISet<long>? localUsed = null)
    {
        var topicBank = _bank.Where(x => x.Topic.Id == topic.Id).ToArray();
        var exact = topicBank.Where(x => x.RequestedDifficulty == CurrentDifficulty).ToArray();
        var candidates = exact.Length > 0 ? exact : topicBank.OrderBy(x => Math.Abs((int)x.RequestedDifficulty - (int)CurrentDifficulty)).ToArray();
        if (exact.Length == 0) logger.LogWarning("No exact {Level} question for topic {TopicId}; using closest bank level.", CurrentDifficulty, topic.Id);
        return candidates.FirstOrDefault(x => !_usedQuestionIds.Contains(x.Id) && (localUsed is null || !localUsed.Contains(x.Id)))
            ?? candidates.FirstOrDefault(x => localUsed is null || !localUsed.Contains(x.Id)) ?? candidates.FirstOrDefault();
    }

    private ChallengeQuestion? SelectOvertimeQuestion()
    {
        var ranked = _progress.Values.OrderByDescending(x => (1 - x.Accuracy) * BonusConfiguration.ImportanceTier(x.Topic.Importance)
                * (_bank.Any(q => q.Topic.Id == x.Topic.Id && !_usedQuestionIds.Contains(q.Id)) ? 1.25 : 1))
            .ThenBy(_ => Random.Shared.Next()).Select(x => x.Topic).ToList();
        var topic = ranked.FirstOrDefault(x => x.Id != _lastTopicId) ?? ranked.FirstOrDefault();
        return topic is null ? null : SelectQuestion(topic);
    }
}
