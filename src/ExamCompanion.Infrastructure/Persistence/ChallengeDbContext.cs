using ChallengePrototype.Models;
using Microsoft.EntityFrameworkCore;

namespace ChallengePrototype.Data;

public sealed class ChallengeDbContext(DbContextOptions<ChallengeDbContext> options) : DbContext(options)
{
    public DbSet<ExamSession> ExamSessions => Set<ExamSession>();
    public DbSet<ExamLessonChallenge> LessonChallenges => Set<ExamLessonChallenge>();
    public DbSet<TopicProgress> TopicProgresses => Set<TopicProgress>();
    public DbSet<ChallengeLevelProgress> LevelProgresses => Set<ChallengeLevelProgress>();
    public DbSet<LevelDesign> LevelDesigns => Set<LevelDesign>();
    public DbSet<LevelRule> LevelRules => Set<LevelRule>();
    public DbSet<ChallengeRun> ChallengeRuns => Set<ChallengeRun>();
    public DbSet<QuestionAttempt> QuestionAttempts => Set<QuestionAttempt>();
    public DbSet<ChallengeHealthPattern> ChallengeHealthPatterns => Set<ChallengeHealthPattern>();
    public DbSet<ChallengeHealthDifficultyFactor> ChallengeHealthDifficultyFactors => Set<ChallengeHealthDifficultyFactor>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ExamSession>(entity =>
        {
            entity.Property(x => x.UserId).HasMaxLength(128).IsRequired();
            entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
            entity.HasIndex(x => new { x.UserId, x.Status });
        });
        modelBuilder.Entity<ExamLessonChallenge>(entity =>
        {
            entity.HasIndex(x => new { x.ExamSessionId, x.LessonId }).IsUnique();
            entity.HasOne(x => x.LevelDesign).WithMany().HasForeignKey(x => x.LevelDesignId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<TopicProgress>(entity =>
        {
            entity.HasIndex(x => new { x.LessonChallengeId, x.TopicId }).IsUnique();
            entity.ToTable(table => table.HasCheckConstraint("CK_TopicProgress_Importance", "ImportanceSnapshot BETWEEN 1 AND 5"));
        });
        modelBuilder.Entity<ChallengeLevelProgress>(entity =>
        {
            entity.HasIndex(x => new { x.LessonChallengeId, x.ChallengeLevel }).IsUnique();
        });
        modelBuilder.Entity<ChallengeHealthPattern>(entity =>
        {
            entity.HasIndex(x => x.ChallengeLevelProgressId).IsUnique();
            entity.HasOne(x => x.LevelProgress).WithOne(x => x.HealthPattern)
                .HasForeignKey<ChallengeHealthPattern>(x => x.ChallengeLevelProgressId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.ToTable(table =>
            {
                table.HasCheckConstraint("CK_ChallengeHealthPattern_Version", "PatternVersion > 0");
                table.HasCheckConstraint("CK_ChallengeHealthPattern_Topics", "TopicCount > 0");
                table.HasCheckConstraint("CK_ChallengeHealthPattern_Questions", "TargetQuestionCount > 0");
                table.HasCheckConstraint("CK_ChallengeHealthPattern_Health", "PromotionHealth > StartingHealth AND StartingHealth > FailureHealth");
            });
        });
        modelBuilder.Entity<ChallengeHealthDifficultyFactor>(entity =>
        {
            entity.HasIndex(x => new { x.ChallengeHealthPatternId, x.QuestionDifficulty }).IsUnique();
            entity.HasOne(x => x.HealthPattern).WithMany(x => x.DifficultyFactors)
                .HasForeignKey(x => x.ChallengeHealthPatternId).OnDelete(DeleteBehavior.Cascade);
            entity.ToTable(table => table.HasCheckConstraint("CK_ChallengeHealthDifficultyFactor_Timing",
                "GracePeriodMilliseconds >= 0 AND PressureWindowSeconds > 0"));
        });
        modelBuilder.Entity<LevelDesign>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(160).IsRequired();
            entity.HasIndex(x => new { x.Name, x.Version }).IsUnique();
        });
        modelBuilder.Entity<LevelRule>(entity =>
        {
            entity.HasIndex(x => new { x.LevelDesignId, x.ChallengeLevel }).IsUnique();
            entity.ToTable(table =>
            {
                table.HasCheckConstraint("CK_LevelRule_QuestionLimits", "MinimumQuestionsPerTopic > 0 AND MaximumQuestionsPerTopic >= MinimumQuestionsPerTopic");
                table.HasCheckConstraint("CK_LevelRule_Evidence", "MinimumEvidencePercent BETWEEN 0 AND 100");
            });
        });
        modelBuilder.Entity<ChallengeRun>().HasIndex(x => new { x.LevelProgressId, x.RunNumber }).IsUnique();
        modelBuilder.Entity<QuestionAttempt>(entity =>
        {
            entity.HasIndex(x => new { x.ChallengeRunId, x.QuestionId });
            entity.HasOne(x => x.TopicProgress).WithMany().HasForeignKey(x => x.TopicProgressId).OnDelete(DeleteBehavior.Restrict);
        });

        var createdAt = new DateTimeOffset(2026, 8, 16, 0, 0, 0, TimeSpan.Zero);
        modelBuilder.Entity<LevelDesign>().HasData(new LevelDesign { Id = 1, Name = "Default Challenge Design", Version = 1, IsActive = true, CreatedAt = createdAt });
        modelBuilder.Entity<LevelRule>().HasData(
            Rule(1, ChallengeLevel.Easy, 1.6, 1, 3, 24, .50, .20, .20, .10, 1, 50, 100, 0, 75),
            Rule(2, ChallengeLevel.Medium, 2, 1, 3, 28, .25, .40, .25, .10, 1.25, 50, 100, 0, 75),
            Rule(3, ChallengeLevel.Hard, 2.4, 1, 4, 32, .15, .20, .45, .20, 1.5, 50, 100, 0, 75),
            Rule(4, ChallengeLevel.VeryHard, 2.8, 1, 4, 32, .05, .15, .30, .50, 1.8, 50, 100, 0, 75));
    }

    private static LevelRule Rule(long id, ChallengeLevel level, double average, int minimum, int maximum, int total,
        double easy, double medium, double hard, double veryHard, double focus, double start, double promotion, double failure, int evidence) =>
        new() { Id = id, LevelDesignId = 1, ChallengeLevel = level, AverageQuestionsPerTopic = average,
            MinimumQuestionsPerTopic = minimum, MaximumQuestionsPerTopic = maximum, MaximumTotalQuestions = total,
            EasyQuestionWeight = easy, MediumQuestionWeight = medium, HardQuestionWeight = hard, VeryHardQuestionWeight = veryHard,
            ImportanceFocus = focus, StartingHealth = start, PromotionHealth = promotion, FailureHealth = failure, MinimumEvidencePercent = evidence };
}
