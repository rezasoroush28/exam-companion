using ExamCompanion.Application.Music;
using ExamCompanion.Domain.Music;
using Microsoft.EntityFrameworkCore;

namespace ExamCompanion.Infrastructure.Music;

public sealed class MusicGameStore(IDbContextFactory<MusicDbContext> factory) : IGameStore, IAsyncDisposable
{
    private MusicDbContext? db;
    private async Task<MusicDbContext> Fresh(CancellationToken ct)
    {
        if (db != null) await db.DisposeAsync();
        return db = await factory.CreateDbContextAsync(ct);
    }
    public async Task<Lesson?> GetLessonAsync(Guid? id, CancellationToken ct = default)
    {
        var context = await Fresh(ct);
        return await context.Lessons.Include(l => l.Topics).ThenInclude(t => t.Questions).AsSplitQuery()
            .FirstOrDefaultAsync(l => id == null || l.Id == id, ct);
    }
    public async Task<GameSession?> GetSessionAsync(Guid id, CancellationToken ct = default)
    {
        var context = await Fresh(ct);
        return await context.Sessions.Include(s => s.Lesson).ThenInclude(l => l.Topics).ThenInclude(t => t.Questions)
            .Include(s => s.Topics).Include(s => s.Attempts).AsSplitQuery().SingleOrDefaultAsync(s => s.Id == id, ct);
    }
    public async Task<Guid?> LatestSessionAsync(Guid lessonId, CancellationToken ct = default)
    {
        await using var context = await factory.CreateDbContextAsync(ct);
        return await context.Sessions.Where(s => s.LessonId == lessonId).OrderByDescending(s => s.StartedAt)
            .Select(s => (Guid?)s.Id).FirstOrDefaultAsync(ct);
    }
    public async Task AddAsync(GameSession session, CancellationToken ct = default)
    {
        db!.Sessions.Add(session); await SaveAsync(ct);
    }
    public async Task SaveAsync(CancellationToken ct = default)
    {
        try { await db!.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { throw new GameInputException("این آلبوم در صفحه دیگری تغییر کرد. دوباره ادامه بده."); }
    }
    public async ValueTask DisposeAsync() { if (db != null) await db.DisposeAsync(); }
}
