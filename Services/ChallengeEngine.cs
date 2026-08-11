using ChallengePrototype.Models;

namespace ChallengePrototype.Services;

public sealed class ChallengeEngine(QuestionService questions)
{
    private readonly Queue<ChallengeQuestion> _remaining = new();
    public string LessonTitle { get; private set; } = "";
    public IReadOnlyList<GameTopic> Topics { get; private set; } = [];
    public GameDifficulty CurrentDifficulty { get; private set; } = GameDifficulty.Easy;
    public int QuestionsPresented { get; private set; }
    public int QuestionsAnswered { get; private set; }
    public int TotalQuestions { get; private set; }
    public bool Complete { get; private set; }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        var setup = await questions.CreateFixedSetupAsync(cancellationToken);
        LessonTitle = setup.LessonTitle;
        Topics = setup.Topics;
        QuestionsAnswered = 0;
        QuestionsPresented = 0;
        TotalQuestions = setup.Questions.Count;
        Complete = false;
        _remaining.Clear();

        var groups = setup.Questions.GroupBy(x => x.Topic.Id)
            .ToDictionary(group => group.Key, group => new Queue<ChallengeQuestion>(group.OrderBy(_ => Random.Shared.Next())));
        var topicOrder = Topics.OrderBy(_ => Random.Shared.Next()).ToArray();
        while (groups.Values.Any(queue => queue.Count > 0))
        {
            foreach (var topic in topicOrder)
                if (groups.TryGetValue(topic.Id, out var queue) && queue.Count > 0)
                    _remaining.Enqueue(queue.Dequeue());
        }
    }

    public Task<ChallengeQuestion?> NextQuestionAsync()
    {
        if (_remaining.Count == 0) { Complete = true; return Task.FromResult<ChallengeQuestion?>(null); }
        var question = _remaining.Dequeue();
        QuestionsPresented = TotalQuestions - _remaining.Count;
        CurrentDifficulty = question.RequestedDifficulty;
        return Task.FromResult<ChallengeQuestion?>(question);
    }

    public void RecordAnswer() => QuestionsAnswered++;
}
