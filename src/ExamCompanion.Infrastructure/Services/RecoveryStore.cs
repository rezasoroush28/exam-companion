using ChallengePrototype.Data;
using ChallengePrototype.Models;
using Microsoft.EntityFrameworkCore;

namespace ChallengePrototype.Services;

public sealed class RecoveryStore(IDbContextFactory<ChallengeDbContext> factory) : IRecoveryStore
{
    public async Task<RecoveryContext?> GetContextAsync(long lessonChallengeId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var challenge = await db.LessonChallenges.AsNoTracking().SingleOrDefaultAsync(x => x.Id == lessonChallengeId, cancellationToken);
        if (challenge is null) return null;
        var progresses = await db.TopicProgresses.AsNoTracking().Where(x => x.LessonChallengeId == lessonChallengeId)
            .Select(x => new { x.Id, x.TopicId, x.ImportanceSnapshot, x.QuestionsSeen, x.TotalBonusEarned, x.LastSeenAt })
            .ToArrayAsync(cancellationToken);
        var candidates = progresses.Select(x => new RecoveryTopicCandidate(x.Id, x.TopicId, x.ImportanceSnapshot,
            x.QuestionsSeen, x.TotalBonusEarned)).ToArray();
        var latestAssessmentAt = progresses.Where(x => x.LastSeenAt.HasValue)
            .Select(x => x.LastSeenAt).Max();
        return new RecoveryContext(challenge.Id, challenge.CurrentLevel, latestAssessmentAt, candidates);
    }

    public async Task<RecoverySuggestionSet?> GetActiveSuggestionSetAsync(long lessonChallengeId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        return await db.RecoverySuggestionSets.AsNoTracking().Include(x => x.Items)
            .Where(x => x.LessonChallengeId == lessonChallengeId && x.Status == SuggestionSetStatus.Active)
            // SQLite cannot translate ORDER BY for DateTimeOffset. Id follows creation order for this append-only set.
            .OrderByDescending(x => x.Id).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<RecoverySuggestionSet> CreateSuggestionSetAsync(long lessonChallengeId,
        IReadOnlyList<RecoverySuggestionDraft> items, CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var active = await db.RecoverySuggestionSets.Where(x => x.LessonChallengeId == lessonChallengeId
            && x.Status == SuggestionSetStatus.Active).ToArrayAsync(cancellationToken);
        foreach (var old in active) old.Status = SuggestionSetStatus.Expired;
        var set = new RecoverySuggestionSet
        {
            LessonChallengeId = lessonChallengeId,
            CreatedAt = DateTimeOffset.UtcNow,
            Status = SuggestionSetStatus.Active,
            Items = items.Select(x => new RecoverySuggestionItem
            {
                TopicProgressId = x.TopicProgressId,
                TopicId = x.TopicId,
                Rank = x.Rank,
                ImportanceSnapshot = x.Importance,
                QuestionsSeenSnapshot = x.QuestionsSeen,
                TotalBonusSnapshot = x.TotalBonusEarned,
                AverageBonusSnapshot = x.AverageBonus
            }).ToList()
        };
        db.Add(set);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return set;
    }

    public async Task<StudyClaim> CreateStudyClaimAsync(long suggestionSetId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var set = await db.RecoverySuggestionSets.Include(x => x.Items)
            .SingleOrDefaultAsync(x => x.Id == suggestionSetId, cancellationToken)
            ?? throw new InvalidOperationException("Suggestion set not found.");
        if (set.Status != SuggestionSetStatus.Active) throw new InvalidOperationException("Suggestion set is no longer active.");
        var selected = set.Items.OrderByDescending(x => x.ImportanceSnapshot).ThenBy(x => x.Rank).ToArray();
        if (selected.Length == 0) throw new InvalidOperationException("Suggestion set has no topics.");
        var now = DateTimeOffset.UtcNow;
        var claim = new StudyClaim
        {
            LessonChallengeId = set.LessonChallengeId,
            SetId = set.Id,
            CreatedAt = now,
            ClaimedAt = now,
            Topics = selected.Select((x, index) => new StudyClaimTopic
            {
                SuggestionItemId = x.Id,
                TopicProgressId = x.TopicProgressId,
                TopicId = x.TopicId,
                Importance = x.ImportanceSnapshot,
                Sequence = index
            }).ToList()
        };
        set.Status = SuggestionSetStatus.Claimed;
        db.Add(claim);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return claim;
    }

    public async Task<StudyClaim?> GetStudyClaimAsync(long studyClaimId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        return await db.StudyClaims.AsNoTracking().Include(x => x.Topics)
            .SingleOrDefaultAsync(x => x.Id == studyClaimId, cancellationToken);
    }

    public async Task<RecoveryMiniGameSession?> GetSessionForClaimAsync(long studyClaimId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        return await db.RecoveryMiniGameSessions.AsNoTracking()
            .SingleOrDefaultAsync(x => x.StudyClaimId == studyClaimId, cancellationToken);
    }

    public async Task<RecoveryMiniGameSession?> GetLatestSessionForChallengeAsync(long lessonChallengeId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        return await db.RecoveryMiniGameSessions.AsNoTracking()
            .Include(x => x.StudyClaim).ThenInclude(x => x.SuggestionSet)
            .Where(x => x.LessonChallengeId == lessonChallengeId)
            .OrderByDescending(x => x.Id).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> HasAssessmentActivityAfterAsync(long lessonChallengeId, DateTimeOffset since,
        CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var timestamps = await db.TopicProgresses.AsNoTracking()
            .Where(x => x.LessonChallengeId == lessonChallengeId && x.LastSeenAt != null)
            .Select(x => x.LastSeenAt).ToArrayAsync(cancellationToken);
        return timestamps.Any(x => x > since);
    }

    public async Task<RecoveryMiniGameSession> ReconcileSessionTopicsAsync(long sessionId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var session = await db.RecoveryMiniGameSessions
            .Include(x => x.Topics)
            .Include(x => x.StudyClaim).ThenInclude(x => x.Topics)
            .Include(x => x.StudyClaim).ThenInclude(x => x.SuggestionSet).ThenInclude(x => x.Items)
            .SingleOrDefaultAsync(x => x.Id == sessionId, cancellationToken)
            ?? throw new InvalidOperationException("Recovery session not found.");
        var desired = session.StudyClaim.SuggestionSet.Items
            .OrderByDescending(x => x.ImportanceSnapshot).ThenBy(x => x.Rank).ToArray();
        if (desired.Length == 0) throw new InvalidOperationException("Suggestion set has no topics.");
        var claimIsComplete = session.StudyClaim.Topics.OrderBy(x => x.Sequence).Select(x => x.SuggestionItemId)
            .SequenceEqual(desired.Select(x => x.Id));
        var sessionIsComplete = session.Topics.OrderBy(x => x.Sequence).Select(x => x.TopicProgressId)
            .SequenceEqual(desired.Select(x => x.TopicProgressId));
        var thresholdsAreCurrent = session.Topics.All(topic =>
            topic.RequiredCorrect == RecoveryRules.RequiredCorrect(topic.Importance));
        if (claimIsComplete && sessionIsComplete && thresholdsAreCurrent) return session;

        foreach (var topic in session.StudyClaim.Topics) topic.Sequence += 1000;
        foreach (var topic in session.Topics) topic.Sequence += 1000;
        await db.SaveChangesAsync(cancellationToken);

        for (var index = 0; index < desired.Length; index++)
        {
            var item = desired[index];
            var claimTopic = session.StudyClaim.Topics.FirstOrDefault(x => x.SuggestionItemId == item.Id);
            if (claimTopic is null)
            {
                claimTopic = new StudyClaimTopic
                {
                    ClaimId = session.StudyClaim.Id,
                    SuggestionItemId = item.Id,
                    TopicProgressId = item.TopicProgressId,
                    TopicId = item.TopicId,
                    Importance = item.ImportanceSnapshot
                };
                session.StudyClaim.Topics.Add(claimTopic);
            }
            claimTopic.Sequence = index;
            claimTopic.Importance = item.ImportanceSnapshot;

            var sessionTopic = session.Topics.FirstOrDefault(x => x.TopicProgressId == item.TopicProgressId);
            if (sessionTopic is null)
            {
                sessionTopic = new RecoveryTopicSession
                {
                    MiniGameSessionId = session.Id,
                    TopicProgressId = item.TopicProgressId,
                    TopicId = item.TopicId,
                    Importance = item.ImportanceSnapshot,
                    RequiredCorrect = RecoveryRules.RequiredCorrect(item.ImportanceSnapshot),
                    Status = TopicStatus.Pending
                };
                session.Topics.Add(sessionTopic);
            }
            sessionTopic.Sequence = index;
            sessionTopic.Importance = item.ImportanceSnapshot;
            sessionTopic.RequiredCorrect = RecoveryRules.RequiredCorrect(item.ImportanceSnapshot);
            if (sessionTopic.Status == TopicStatus.Stabilized)
            {
                sessionTopic.Stability = 1;
            }
            else
            {
                sessionTopic.Stability = RecoveryRules.Stability(sessionTopic.Correct, sessionTopic.RequiredCorrect);
                if (sessionTopic.Correct >= sessionTopic.RequiredCorrect)
                {
                    sessionTopic.Status = TopicStatus.Stabilized;
                    sessionTopic.StabilizedAt ??= DateTimeOffset.UtcNow;
                }
            }
        }

        var next = session.Topics.OrderBy(x => x.Sequence).FirstOrDefault(x => x.Status != TopicStatus.Stabilized);
        if (next is null)
        {
            session.Status = MiniGameStatus.Completed;
            session.CurrentTopicIndex = desired.Length;
            session.CompletedAt ??= DateTimeOffset.UtcNow;
        }
        else
        {
            foreach (var topic in session.Topics.Where(x => x.Status != TopicStatus.Stabilized))
                topic.Status = topic == next ? TopicStatus.Active : TopicStatus.Pending;
            next.StartedAt ??= DateTimeOffset.UtcNow;
            session.CurrentTopicIndex = next.Sequence;
            session.Status = MiniGameStatus.Active;
            session.CompletedAt = null;
        }
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return session;
    }

    public async Task<RecoveryMiniGameSession> CreateSessionAsync(StudyClaim claim,
        CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var ordered = claim.Topics.OrderBy(x => x.Sequence).ToArray();
        if (ordered.Length == 0) throw new InvalidOperationException("Claim has no topics.");
        var session = new RecoveryMiniGameSession
        {
            LessonChallengeId = claim.LessonChallengeId,
            StudyClaimId = claim.Id,
            StartedAt = now,
            Status = MiniGameStatus.Active,
            CurrentTopicIndex = 0,
            Topics = ordered.Select((x, index) => new RecoveryTopicSession
            {
                TopicProgressId = x.TopicProgressId,
                TopicId = x.TopicId,
                Importance = x.Importance,
                Sequence = index,
                RequiredCorrect = RecoveryRules.RequiredCorrect(x.Importance),
                Status = index == 0 ? TopicStatus.Active : TopicStatus.Pending,
                StartedAt = index == 0 ? now : null
            }).ToList()
        };
        db.Add(session);
        await db.SaveChangesAsync(cancellationToken);
        return session;
    }

    public async Task<RecoverySessionSnapshot?> GetSessionAsync(long sessionId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        return await db.RecoveryMiniGameSessions.AsNoTracking().Where(x => x.Id == sessionId)
            .Select(x => new RecoverySessionSnapshot(x.Id, x.LessonChallengeId, x.StudyClaimId, x.Status,
                x.CurrentTopicIndex, x.StartedAt, x.CompletedAt,
                x.Topics.OrderBy(t => t.Sequence).Select(t => new RecoveryTopicSnapshot(t.Id, t.TopicProgressId,
                    t.TopicId, t.Importance, t.Sequence, t.RequiredCorrect, t.Correct,
                    t.Incorrect, t.Asked, t.Stability, t.Status, t.StartedAt, t.StabilizedAt)).ToArray(),
                x.Attempts.OrderBy(a => a.Id).Select(a => new RecoveryAttemptSnapshot(a.QuestionId, a.TopicId,
                    a.ActualDifficulty, a.IsCorrect, a.ShownAt, a.AnsweredAt)).ToArray()))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task SaveAnswerAsync(RecoveryAnswerWrite answer, CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var session = await db.RecoveryMiniGameSessions.Include(x => x.Topics)
            .SingleOrDefaultAsync(x => x.Id == answer.SessionId, cancellationToken)
            ?? throw new InvalidOperationException("Recovery session not found.");
        if (session.Status != MiniGameStatus.Active) throw new InvalidOperationException("Recovery session is not active.");
        var topic = session.Topics.SingleOrDefault(x => x.Id == answer.TopicSessionId)
            ?? throw new InvalidOperationException("Recovery topic session not found.");
        topic.Correct = answer.CorrectAnswers;
        topic.Incorrect = answer.IncorrectAnswers;
        topic.Asked = answer.QuestionsAsked;
        topic.Stability = answer.Stability;
        topic.Status = answer.TopicStatus;
        topic.StabilizedAt = answer.StabilizedAt;
        session.CurrentTopicIndex = answer.CurrentTopicIndex;
        session.Status = answer.SessionStatus;
        session.CompletedAt = answer.CompletedAt;
        if (answer.TopicStatus == TopicStatus.Stabilized && answer.SessionStatus == MiniGameStatus.Active)
        {
            var next = session.Topics.Single(x => x.Sequence == answer.CurrentTopicIndex);
            next.Status = TopicStatus.Active;
            next.StartedAt ??= answer.AnsweredAt;
        }
        db.RecoveryQuestionAttempts.Add(new RecoveryQuestionAttempt
        {
            SessionId = answer.SessionId,
            TopicSessionId = answer.TopicSessionId,
            QuestionId = answer.QuestionId,
            TopicId = answer.TopicId,
            ActualDifficulty = answer.ActualDifficulty,
            ShownAt = answer.ShownAt,
            AnsweredAt = answer.AnsweredAt,
            SelectedAnswer = answer.SelectedAnswer,
            IsCorrect = answer.IsCorrect,
            ActiveThinkingMs = answer.ActiveThinkingMilliseconds,
            HintLevelUsed = answer.HintLevelUsed
        });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task SetSessionStatusAsync(long sessionId, MiniGameStatus status,
        CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var session = await db.RecoveryMiniGameSessions.SingleOrDefaultAsync(x => x.Id == sessionId, cancellationToken);
        if (session is null || session.Status == MiniGameStatus.Completed) return;
        session.Status = status;
        await db.SaveChangesAsync(cancellationToken);
    }
}
