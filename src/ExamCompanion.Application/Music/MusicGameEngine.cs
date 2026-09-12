using ExamCompanion.Domain.Music;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ExamCompanion.Application.Music;

public sealed class MusicGameEngine(IGameStore store, IQuestionSelector selector,
    IOptions<GameRulesOptions> options, ILogger<MusicGameEngine> logger) : IGameEngine
{
    private readonly GameRulesOptions rules = options.Value;
    public async Task<LessonDto> GetLessonAsync(CancellationToken ct = default)
    {
        var lesson = await store.GetLessonAsync(null, ct) ?? throw new GameInputException("درس پیدا نشد.");
        return new(lesson.Id, lesson.Name, "آلبوم زیست‌شناسی", lesson.Topics.OrderBy(t => t.DisplayOrder)
            .Select(t => new TopicStateDto(t.Id, t.Name, t.DisplayOrder, t.Importance, t.MusicalColor, t.MusicalKey,
                GameRulesOptions.Cycles(t.Importance), 0, TopicProgressStatus.Locked, false, false, 0)).ToArray(),
            await store.LatestSessionAsync(lesson.Id, ct));
    }
    public async Task<GameStateDto> StartSessionAsync(Guid lessonId, CancellationToken ct = default)
    {
        var lesson = await store.GetLessonAsync(lessonId, ct) ?? throw new GameInputException("درس معتبر نیست.");
        var topics = lesson.Topics.OrderBy(t => t.DisplayOrder).ToArray();
        if (topics.Length == 0 || topics.Any(t => !t.Questions.Any(q => q.Type == QuestionType.Educational)
            || !t.Questions.Any(q => q.Type == QuestionType.Challenge))) throw new GameInputException("بانک سؤال این درس کامل نیست.");
        var session = new GameSession { Lesson = lesson, LessonId = lesson.Id, CurrentTopicId = topics[0].Id,
            EducationalTarget = rules.BaseEducationalQuestionCount,
            Topics = topics.Select((t, i) => new TopicProgress { TopicId = t.Id,
                RequiredCycles = GameRulesOptions.Cycles(t.Importance), Status = i == 0 ? TopicProgressStatus.Active : TopicProgressStatus.Locked }).ToList() };
        NextQuestion(session);
        await store.AddAsync(session, ct);
        logger.LogInformation("Music session {SessionId} started for {LessonId}", session.Id, lessonId);
        return Map(session);
    }
    public async Task<GameStateDto> GetSessionAsync(Guid id, CancellationToken ct = default) => Map(await Load(id, ct));
    private async Task<GameSession> Load(Guid id, CancellationToken ct) =>
        await store.GetSessionAsync(id, ct) ?? throw new GameInputException("این آلبوم پیدا نشد. یک بازی تازه شروع کن.");

    public async Task<AnswerResultDto> SubmitAnswerAsync(Guid sessionId, Guid questionId, Guid turnId, string option, CancellationToken ct = default)
    {
        var s = await Load(sessionId, ct);
        if (s.CurrentQuestionId != questionId || s.TurnId != turnId || s.Step == GameStepType.Complete)
            throw new GameInputException("این پاسخ قبلاً ثبت شده یا سؤال عوض شده است. ادامه را بزن.");
        var topic = s.Lesson.Topics.Single(t => t.Id == (s.RepairTopicId ?? s.CurrentTopicId));
        var q = topic.Questions.Single(q => q.Id == questionId);
        if (!Options(q).Any(o => o.Key == option)) throw new GameInputException("یک گزینه معتبر انتخاب کن.");
        var p = s.Topics.Single(p => p.TopicId == topic.Id);
        var correct = option == q.CorrectOption;
        var reinforcement = s.Step == GameStepType.Reinforcement;
        s.Attempts.Add(new QuestionAttempt { GameSessionId = s.Id, TopicId = topic.Id, QuestionId = q.Id, TurnId = s.TurnId,
            CycleIndex = s.CurrentCycleIndex, IsCorrect = correct, SelectedOption = option, WasReinforcement = reinforcement, WasRepair = s.RepairTopicId.HasValue });
        s.CurrentQuestionIndex++;
        var result = correct ? AnswerResultType.Correct : AnswerResultType.Reinforcement;
        var message = correct ? "یک نت روشن‌تر!" : "اشکالی نداره؛ یک‌بار دیگه کشفش می‌کنیم.";
        var resumeSuspendedQuestion = false;
        if (s.RepairTopicId.HasValue)
        {
            if (s.Step == GameStepType.RepairEducational)
            {
                s.RepairEducationalCorrect = correct;
                s.Step = GameStepType.RepairChallenge;
            }
            else
            {
                if (correct && s.RepairEducationalCorrect)
                {
                    p.HasScratch = false; p.IsRepaired = true; p.Status = TopicProgressStatus.Repaired;
                    result = AnswerResultType.Repaired; message = "خش پاک شد. این ملودی دوباره می‌درخشه!";
                    logger.LogInformation("Topic {TopicId} repaired in {SessionId}", topic.Id, s.Id);
                }
                else { result = AnswerResultType.RepairFailed; message = "ملودی هنوز کمی تمرین می‌خواد؛ هر وقت خواستی برگرد."; }
                s.RepairTopicId = null;
                s.Step = s.SuspendedStep ?? GameStepType.Complete;
                s.CurrentQuestionId = s.SuspendedQuestionId;
                s.SuspendedStep = null; s.SuspendedQuestionId = null;
                s.TurnId = Guid.NewGuid();
                resumeSuspendedQuestion = true;
            }
        }
        else
        {
            if (!correct) p.TotalWrongAnswers++;
            if (reinforcement) p.ReinforcementCount++;
            p.Status = TopicProgressStatus.InProgress;
            if (q.Type == QuestionType.Educational)
            {
                s.EducationalAnswered++;
                if (!correct && s.ChallengeAttempts == 0)
                    s.EducationalTarget = Math.Min(rules.MaxEducationalQuestionCount, s.EducationalTarget + 1);
                s.Step = StepForCycle(s);
            }
            else
            {
                s.ChallengeAttempts++;
                if (correct || s.ChallengeAttempts >= rules.MaxChallengeAttempts)
                {
                    if (!correct) p.HasScratch = true;
                    p.CompletedCycles++;
                    result = AnswerResultType.CycleCompleted; message = "یک بخش از ملودی ساخته شد.";
                    logger.LogInformation("Cycle {Cycle} completed for topic {TopicId}", p.CompletedCycles, topic.Id);
                }
                else s.Step = GameStepType.Reinforcement;
            }
            var hadScratch = p.HasScratch;
            p.HasScratch |= p.TotalWrongAnswers >= rules.WrongAnswersForScratch || p.ReinforcementCount >= rules.ReinforcementsForScratch;
            if (!hadScratch && p.HasScratch) logger.LogInformation("Topic {TopicId} gained a scratch", topic.Id);
            if (result == AnswerResultType.CycleCompleted)
            {
                if (p.CompletedCycles >= p.RequiredCycles)
                {
                    p.Status = p.HasScratch ? TopicProgressStatus.CompletedWithScratch : TopicProgressStatus.CompletedClean;
                    p.CompletedAt = DateTime.UtcNow;
                    logger.LogInformation("Topic {TopicId} completed; scratched: {Scratched}", topic.Id, p.HasScratch);
                    result = p.HasScratch ? AnswerResultType.Scratched : AnswerResultType.TopicCompleted;
                    message = p.HasScratch ? "قطعه کامل شد؛ یک خش کوچک برای صیقل‌دادن باقی موند." : "این قطعه کامل شد. ملودی بعدی منتظرته!";
                    var next = s.Lesson.Topics.OrderBy(t => t.DisplayOrder).FirstOrDefault(t => s.Topics.Single(x => x.TopicId == t.Id).CompletedAt == null);
                    if (next == null)
                    {
                        s.Status = GameSessionStatus.Completed; s.CompletedAt = DateTime.UtcNow; s.Step = GameStepType.Complete;
                        result = AnswerResultType.LessonCompleted; message = "آلبومت کامل شد. حالا به صدای یادگیریت گوش بده.";
                        logger.LogInformation("Lesson completed in session {SessionId}", s.Id);
                    }
                    else
                    {
                        s.CurrentTopicId = next.Id;
                        s.Topics.Single(x => x.TopicId == next.Id).Status = TopicProgressStatus.Active;
                        ResetCycle(s);
                        logger.LogInformation("Topic {TopicId} started", next.Id);
                    }
                }
                else ResetCycle(s);
            }
        }
        if (!resumeSuspendedQuestion) NextQuestion(s);
        s.Revision = Guid.NewGuid();
        await store.SaveAsync(ct);
        return new(Map(s), correct, q.CorrectOption, q.Explanation, result, message, topic.DisplayOrder);
    }
    private GameStepType StepForCycle(GameSession s) => s.ChallengeAttempts > 0 || s.EducationalAnswered >= s.EducationalTarget
        ? GameStepType.Challenge : s.EducationalAnswered >= rules.BaseEducationalQuestionCount ? GameStepType.Reinforcement : GameStepType.Educational;
    private void ResetCycle(GameSession s)
    {
        s.CurrentCycleIndex = s.Topics.Single(p => p.TopicId == s.CurrentTopicId).CompletedCycles;
        s.EducationalAnswered = 0; s.EducationalTarget = rules.BaseEducationalQuestionCount;
        s.ChallengeAttempts = 0; s.Step = GameStepType.Educational;
    }
    private void NextQuestion(GameSession s)
    {
        s.TurnId = Guid.NewGuid();
        if (s.Step == GameStepType.Complete) { s.CurrentQuestionId = null; return; }
        var topic = s.Lesson.Topics.Single(t => t.Id == (s.RepairTopicId ?? s.CurrentTopicId));
        var type = s.Step is GameStepType.Challenge or GameStepType.RepairChallenge ? QuestionType.Challenge : QuestionType.Educational;
        s.CurrentQuestionId = selector.Select(s, topic, type, s.Step == GameStepType.Reinforcement).Id;
    }
    public async Task<GameStateDto> StartRepairAsync(Guid sessionId, Guid topicId, CancellationToken ct = default)
    {
        var s = await Load(sessionId, ct);
        var p = s.Topics.SingleOrDefault(p => p.TopicId == topicId);
        if (s.RepairTopicId.HasValue || p?.Status != TopicProgressStatus.CompletedWithScratch)
            throw new GameInputException("این قطعه هنوز آماده صیقل‌دادن نیست.");
        s.SuspendedStep = s.Step; s.SuspendedQuestionId = s.CurrentQuestionId;
        s.RepairTopicId = topicId; s.RepairEducationalCorrect = false; s.Step = GameStepType.RepairEducational;
        NextQuestion(s); s.Revision = Guid.NewGuid(); await store.SaveAsync(ct);
        return Map(s);
    }
    private static IReadOnlyList<OptionDto> Options(Question q) => new[] {
        new OptionDto("A", q.OptionA), new("B", q.OptionB), new("C", q.OptionC ?? ""), new("D", q.OptionD ?? "") }.Where(o => o.Text.Length > 0).ToArray();
    private static GameStateDto Map(GameSession s)
    {
        var active = s.RepairTopicId ?? s.CurrentTopicId;
        var p = s.Topics.Single(p => p.TopicId == active);
        var q = s.Lesson.Topics.SelectMany(t => t.Questions).FirstOrDefault(q => q.Id == s.CurrentQuestionId);
        var topics = s.Lesson.Topics.OrderBy(t => t.DisplayOrder).Select(t => {
            var tp = s.Topics.Single(p => p.TopicId == t.Id);
            var partial = t.Id == s.CurrentTopicId && tp.CompletedAt == null ? Math.Min(.8, s.EducationalAnswered * .18) : 0;
            return new TopicStateDto(t.Id, t.Name, t.DisplayOrder, t.Importance, t.MusicalColor, t.MusicalKey,
                tp.RequiredCycles, tp.CompletedCycles, tp.Status, tp.HasScratch, tp.IsRepaired,
                Math.Min(1, (tp.CompletedCycles + partial) / tp.RequiredCycles));
        }).ToArray();
        return new(s.Id, s.Lesson.Name, s.Status, active, s.CurrentCycleIndex + 1, p.RequiredCycles, s.Step, s.TurnId,
            q == null ? null : new(q.Id, q.Type, q.Text, Options(q)), topics, s.RepairTopicId.HasValue,
            s.EducationalAnswered, s.EducationalTarget, s.ChallengeAttempts);
    }
}
