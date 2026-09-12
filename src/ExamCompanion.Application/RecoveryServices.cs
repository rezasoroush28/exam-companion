using ChallengePrototype.Models;

namespace ChallengePrototype.Services;

public sealed class TopicSuggestionService(IRecoveryStore store, IEducationalQuestionSource questions)
    : ITopicSuggestionService
{
    public IReadOnlyList<RecoverySuggestionDraft> Rank(IEnumerable<RecoveryTopicCandidate> candidates, int maximum = 3) =>
        candidates.Where(x => x.QuestionsSeen > 0)
            .OrderBy(x => x.AverageBonus)
            .ThenByDescending(x => x.QuestionsSeen)
            .ThenByDescending(x => x.Importance)
            .ThenBy(x => x.TopicId)
            .Take(Math.Max(0, maximum))
            .Select((x, index) => new RecoverySuggestionDraft(x.TopicProgressId, x.TopicId, index + 1,
                x.Importance, x.QuestionsSeen, x.TotalBonusEarned, x.AverageBonus))
            .ToArray();

    public async Task<RecoverySuggestionPage> GetOrCreateAsync(long lessonChallengeId,
        CancellationToken cancellationToken = default)
    {
        var context = await store.GetContextAsync(lessonChallengeId, cancellationToken)
            ?? throw new InvalidOperationException("Challenge not found.");
        var set = await store.GetActiveSuggestionSetAsync(lessonChallengeId, cancellationToken);
        if (set is not null && context.LatestAssessmentAt > set.CreatedAt) set = null;
        if (set is null)
        {
            var ranked = Rank(context.Candidates);
            if (ranked.Count == 0)
                return new RecoverySuggestionPage(0, SuggestionSetStatus.Active, context.CurrentLevel, []);
            set = await store.CreateSuggestionSetAsync(lessonChallengeId, ranked, cancellationToken);
        }

        var titles = await questions.GetTopicTitlesAsync(set.Items.Select(x => x.TopicId), cancellationToken);
        var cards = set.Items.OrderBy(x => x.Rank).Select(x => new RecoverySuggestionCard(x.Id,
            x.TopicProgressId, x.TopicId, titles.GetValueOrDefault(x.TopicId, $"Topic {x.TopicId}"), x.Rank,
            x.ImportanceSnapshot, x.QuestionsSeenSnapshot, x.TotalBonusSnapshot, x.AverageBonusSnapshot)).ToArray();
        return new RecoverySuggestionPage(set.Id, set.Status, context.CurrentLevel, cards);
    }
}

public sealed class StudyClaimService(IRecoveryStore store) : IStudyClaimService
{
    public async Task<StudyClaimResult> ClaimAsync(long suggestionSetId,
        CancellationToken cancellationToken = default)
    {
        var claim = await store.CreateStudyClaimAsync(suggestionSetId, cancellationToken);
        return new StudyClaimResult(claim.Id, claim.Topics.OrderBy(x => x.Sequence).Select(x => x.TopicId).ToArray());
    }
}

public sealed class RecoveryMiniGameService(IRecoveryStore store, IEducationalQuestionSource questions)
    : IRecoveryMiniGameService
{
    public async Task<RecoverySessionSnapshot?> ResumeLatestAsync(long lessonChallengeId,
        CancellationToken cancellationToken = default)
    {
        var existing = await store.GetLatestSessionForChallengeAsync(lessonChallengeId, cancellationToken);
        if (existing is null) return null;
        if (existing.Status == MiniGameStatus.Completed)
        {
            var suggestionCreatedAt = existing.StudyClaim.SuggestionSet.CreatedAt;
            if (await store.HasAssessmentActivityAfterAsync(lessonChallengeId, suggestionCreatedAt, cancellationToken))
                return null;
            return await store.GetSessionAsync(existing.Id, cancellationToken);
        }
        existing = await store.ReconcileSessionTopicsAsync(existing.Id, cancellationToken);
        if (existing.Status is MiniGameStatus.Interrupted or MiniGameStatus.NotStarted)
            await store.SetSessionStatusAsync(existing.Id, MiniGameStatus.Active, cancellationToken);
        return await store.GetSessionAsync(existing.Id, cancellationToken);
    }

    public async Task<RecoverySessionSnapshot> StartOrResumeAsync(long studyClaimId,
        CancellationToken cancellationToken = default)
    {
        var existing = await store.GetSessionForClaimAsync(studyClaimId, cancellationToken);
        if (existing is null)
        {
            var claim = await store.GetStudyClaimAsync(studyClaimId, cancellationToken)
                ?? throw new InvalidOperationException("Study claim not found.");
            existing = await store.CreateSessionAsync(claim, cancellationToken);
        }
        else if (existing.Status is MiniGameStatus.Interrupted or MiniGameStatus.NotStarted)
        {
            await store.SetSessionStatusAsync(existing.Id, MiniGameStatus.Active, cancellationToken);
        }
        existing = await store.ReconcileSessionTopicsAsync(existing.Id, cancellationToken);
        return await store.GetSessionAsync(existing.Id, cancellationToken)
            ?? throw new InvalidOperationException("Recovery session could not be loaded.");
    }

    public Task<RecoverySessionSnapshot?> GetAsync(long sessionId, CancellationToken cancellationToken = default) =>
        store.GetSessionAsync(sessionId, cancellationToken);

    public async Task<EducationalQuestion?> GetNextQuestionAsync(RecoverySessionSnapshot session,
        CancellationToken cancellationToken = default)
    {
        if (session.Status == MiniGameStatus.Completed) return null;
        var topic = session.Topics.OrderBy(x => x.Sequence).ElementAtOrDefault(session.CurrentTopicIndex)
            ?? session.Topics.OrderBy(x => x.Sequence).FirstOrDefault(x => x.Status != TopicStatus.Stabilized);
        if (topic is null) return null;
        var bank = await questions.GetQuestionsAsync(topic.TopicId, cancellationToken);
        if (bank.Count == 0) return null;
        var context = await store.GetContextAsync(session.LessonChallengeId, cancellationToken)
            ?? throw new InvalidOperationException("Challenge not found.");
        var topicAttempts = session.Attempts.Where(x => x.TopicId == topic.TopicId).ToArray();
        var desired = topicAttempts.Length == 0
            ? RecoveryRules.RecoveryStartDifficulty(context.CurrentLevel)
            : RecoveryRules.NextDifficulty(topicAttempts[^1].ActualDifficulty, topicAttempts[^1].IsCorrect, context.CurrentLevel);
        var used = topicAttempts.Select(x => x.QuestionId).ToHashSet(StringComparer.Ordinal);
        var last = topicAttempts.LastOrDefault()?.QuestionId;
        return bank.OrderBy(x => used.Contains(x.Id) ? 1 : 0)
            .ThenBy(x => x.Id == last ? 1 : 0)
            .ThenBy(x => Math.Abs((int)x.Difficulty - (int)desired))
            .ThenBy(_ => Random.Shared.Next())
            .First();
    }

    public async Task<RecoveryAnswerResult> AnswerAsync(RecoverySessionSnapshot session,
        EducationalQuestion question, string selectedAnswer, DateTimeOffset shownAt,
        long activeThinkingMilliseconds, int hintLevelUsed, CancellationToken cancellationToken = default)
    {
        if (session.Status == MiniGameStatus.Completed) throw new InvalidOperationException("Session is already complete.");
        var topic = session.Topics.OrderBy(x => x.Sequence).ElementAt(session.CurrentTopicIndex);
        if (topic.TopicId != question.TopicId) throw new InvalidOperationException("Question does not match the active topic.");
        var correct = StringComparer.Ordinal.Equals(selectedAnswer, question.CorrectOption);
        var progress = RecoveryRules.ApplyAnswer(topic.CorrectAnswers, topic.IncorrectAnswers, topic.QuestionsAsked,
            topic.RequiredCorrect, correct);
        var nextIndex = RecoveryRules.NextTopicIndex(session.CurrentTopicIndex, progress.Stabilized);
        var complete = RecoveryRules.IsSessionComplete(nextIndex, session.Topics.Count, progress.Stabilized);
        var now = DateTimeOffset.UtcNow;
        await store.SaveAnswerAsync(new RecoveryAnswerWrite(session.Id, topic.Id, question.Id, topic.TopicId,
            question.Difficulty, shownAt, now, selectedAnswer, correct, Math.Max(0, activeThinkingMilliseconds),
            Math.Clamp(hintLevelUsed, 0, 3), progress.Correct, progress.Incorrect, progress.Asked, progress.Stability,
            progress.Stabilized ? TopicStatus.Stabilized : TopicStatus.Active, progress.Stabilized ? now : null,
            nextIndex, complete ? MiniGameStatus.Completed : MiniGameStatus.Active, complete ? now : null), cancellationToken);
        return new RecoveryAnswerResult(correct, progress.Stabilized, complete, progress.Stability, progress.Correct, topic.RequiredCorrect);
    }

    public Task InterruptAsync(long sessionId, CancellationToken cancellationToken = default) =>
        store.SetSessionStatusAsync(sessionId, MiniGameStatus.Interrupted, cancellationToken);
}
