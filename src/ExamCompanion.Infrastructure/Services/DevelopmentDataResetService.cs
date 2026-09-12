using ChallengePrototype.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ChallengePrototype.Services;

public sealed class DevelopmentDataResetService(
    IDbContextFactory<ChallengeDbContext> contextFactory,
    IHostEnvironment environment,
    ILogger<DevelopmentDataResetService> logger) : IDevelopmentDataResetService
{
    public async Task ResetProgressAsync(CancellationToken cancellationToken = default)
    {
        if (!environment.IsDevelopment())
            throw new InvalidOperationException("Development data reset is disabled outside the Development environment.");

        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        await db.RecoveryQuestionAttempts.ExecuteDeleteAsync(cancellationToken);
        await db.RecoveryTopicSessions.ExecuteDeleteAsync(cancellationToken);
        await db.RecoveryMiniGameSessions.ExecuteDeleteAsync(cancellationToken);
        await db.StudyClaimTopics.ExecuteDeleteAsync(cancellationToken);
        await db.StudyClaims.ExecuteDeleteAsync(cancellationToken);
        await db.RecoverySuggestionItems.ExecuteDeleteAsync(cancellationToken);
        await db.RecoverySuggestionSets.ExecuteDeleteAsync(cancellationToken);

        await db.QuestionAttempts.ExecuteDeleteAsync(cancellationToken);
        await db.ChallengeRuns.ExecuteDeleteAsync(cancellationToken);
        await db.ChallengeHealthDifficultyFactors.ExecuteDeleteAsync(cancellationToken);
        await db.ChallengeHealthPatterns.ExecuteDeleteAsync(cancellationToken);
        await db.LevelProgresses.ExecuteDeleteAsync(cancellationToken);
        await db.TopicProgresses.ExecuteDeleteAsync(cancellationToken);
        await db.LessonChallenges.ExecuteDeleteAsync(cancellationToken);
        await db.ExamSessions.ExecuteDeleteAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        logger.LogWarning("Development progress data was reset. Reference and question databases were preserved.");
    }
}
