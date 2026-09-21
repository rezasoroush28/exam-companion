using ExamCompanion.Domain.Music;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExamCompanion.Infrastructure.Music;

public sealed class MusicDbContext(DbContextOptions<MusicDbContext> options) : DbContext(options)
{
    public DbSet<Exam> Exams => Set<Exam>();
    public DbSet<Lesson> Lessons => Set<Lesson>();
    public DbSet<Topic> Topics => Set<Topic>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<GameSession> Sessions => Set<GameSession>();
    public DbSet<TopicProgress> TopicProgress => Set<TopicProgress>();
    public DbSet<QuestionAttempt> Attempts => Set<QuestionAttempt>();
    protected override void OnModelCreating(ModelBuilder b)
    {
        b.ApplyConfiguration(new ExamConfiguration()); b.ApplyConfiguration(new LessonConfiguration());
        b.ApplyConfiguration(new TopicConfiguration()); b.ApplyConfiguration(new QuestionConfiguration());
        b.ApplyConfiguration(new SessionConfiguration()); b.ApplyConfiguration(new ProgressConfiguration());
        b.ApplyConfiguration(new AttemptConfiguration());
        foreach (var entity in b.Model.GetEntityTypes())
            if (entity.FindProperty("Id") is { } id) id.ValueGenerated = Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.Never;
    }
}
public sealed class ExamConfiguration : IEntityTypeConfiguration<Exam>
{
    public void Configure(EntityTypeBuilder<Exam> b) { b.HasKey(x => x.Id); b.Property(x => x.Name).IsRequired().HasMaxLength(200); }
}
public sealed class LessonConfiguration : IEntityTypeConfiguration<Lesson>
{
    public void Configure(EntityTypeBuilder<Lesson> b)
    {
        b.HasKey(x => x.Id); b.Property(x => x.Name).IsRequired(); b.HasIndex(x => x.Slug).IsUnique();
        b.HasMany(x => x.Topics).WithOne(x => x.Lesson).HasForeignKey(x => x.LessonId).OnDelete(DeleteBehavior.Cascade);
    }
}
public sealed class TopicConfiguration : IEntityTypeConfiguration<Topic>
{
    public void Configure(EntityTypeBuilder<Topic> b)
    {
        b.HasKey(x => x.Id); b.Property(x => x.Name).IsRequired(); b.HasIndex(x => new { x.LessonId, x.DisplayOrder }).IsUnique();
        b.HasMany(x => x.Questions).WithOne(x => x.Topic).HasForeignKey(x => x.TopicId).OnDelete(DeleteBehavior.Cascade);
        b.ToTable("Topics", t => t.HasCheckConstraint("CK_Topic_Importance", "Importance >= 0 AND Importance <= 1"));
    }
}
public sealed class QuestionConfiguration : IEntityTypeConfiguration<Question>
{
    public void Configure(EntityTypeBuilder<Question> b)
    {
        b.HasKey(x => x.Id); b.Property(x => x.Text).IsRequired(); b.Property(x => x.OptionA).IsRequired();
        b.Property(x => x.OptionB).IsRequired(); b.Property(x => x.CorrectOption).HasMaxLength(1).IsRequired();
        b.HasIndex(x => new { x.TopicId, x.Type, x.SourceId }).IsUnique();
    }
}
public sealed class SessionConfiguration : IEntityTypeConfiguration<GameSession>
{
    public void Configure(EntityTypeBuilder<GameSession> b)
    {
        b.HasKey(x => x.Id); b.Property(x => x.Revision).IsConcurrencyToken(); b.HasIndex(x => x.StartedAt);
        b.HasOne(x => x.Lesson).WithMany().HasForeignKey(x => x.LessonId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(x => x.Topics).WithOne().HasForeignKey(x => x.GameSessionId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Attempts).WithOne().HasForeignKey(x => x.GameSessionId).OnDelete(DeleteBehavior.Cascade);
    }
}
public sealed class ProgressConfiguration : IEntityTypeConfiguration<TopicProgress>
{
    public void Configure(EntityTypeBuilder<TopicProgress> b)
    {
        b.HasKey(x => x.Id); b.HasIndex(x => new { x.GameSessionId, x.TopicId }).IsUnique();
        b.HasOne<Topic>().WithMany().HasForeignKey(x => x.TopicId).OnDelete(DeleteBehavior.Restrict);
    }
}
public sealed class AttemptConfiguration : IEntityTypeConfiguration<QuestionAttempt>
{
    public void Configure(EntityTypeBuilder<QuestionAttempt> b)
    {
        b.HasKey(x => x.Id); b.HasIndex(x => new { x.GameSessionId, x.TurnId }).IsUnique();
        b.HasOne<Question>().WithMany().HasForeignKey(x => x.QuestionId).OnDelete(DeleteBehavior.Restrict);
    }
}
