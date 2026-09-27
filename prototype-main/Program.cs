using System.Text.Json.Serialization;
using ExamCompanion.Application.Music;
using ExamCompanion.Domain.Music;
using ExamCompanion.Infrastructure.Music;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddOptions<GameRulesOptions>().Bind(builder.Configuration.GetSection("MusicGameRules"))
    .PostConfigure(r => r.ShowDeveloperAnswers = builder.Environment.IsDevelopment());
var database = Path.Combine(builder.Environment.ContentRootPath, "data", "guitar-game.db");
Directory.CreateDirectory(Path.GetDirectoryName(database)!);
builder.Services.AddDbContextFactory<MusicDbContext>(o => o.UseSqlite($"Data Source={database}"));
builder.Services.AddScoped<IGameStore, MusicGameStore>();
builder.Services.AddScoped<IGameEngine, MusicGameEngine>();
builder.Services.AddSingleton<IQuestionSelector, QuestionSelector>();
var app = builder.Build();
await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MusicDbContext>();
    await db.Database.MigrateAsync();
    if (!await db.Lessons.AnyAsync())
    {
        var sourcePath = Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath, "..", "data", "exam-companion.db"));
        if (!File.Exists(sourcePath)) throw new InvalidOperationException("The main game catalog database is required for first startup.");
        await using var source = new MusicDbContext(new DbContextOptionsBuilder<MusicDbContext>()
            .UseSqlite($"Data Source={sourcePath};Mode=ReadOnly").Options);
        var lessons = await source.Lessons.AsNoTracking().Include(l => l.Topics).ThenInclude(t => t.Questions).ToListAsync();
        db.Lessons.AddRange(lessons);
        db.Exams.AddRange(await source.Exams.AsNoTracking().ToListAsync());
        await db.SaveChangesAsync();
    }
}
app.Use(async (context, next) =>
{
    // Same-origin JSON mutations; no cross-origin cookie or form writes.
    if (context.Request.Method == "POST" && (!context.Request.HasJsonContentType()
        || context.Request.Headers["Sec-Fetch-Site"] == "cross-site"))
    { context.Response.StatusCode = 415; return; }
    try { await next(); }
    catch (GameInputException e) { context.Response.StatusCode = 409; await context.Response.WriteAsJsonAsync(new { error = e.Message }); }
    catch (DbUpdateConcurrencyException) { context.Response.StatusCode = 409; await context.Response.WriteAsJsonAsync(new { error = "بازی در پنجره دیگری تغییر کرده است. صفحه را تازه کنید." }); }
    catch (Exception e)
    {
        app.Logger.LogError(e, "Game request failed");
        context.Response.StatusCode = 500;
        await context.Response.WriteAsJsonAsync(new { error = "ارتباط با بازی برقرار نشد. دوباره تلاش کنید." });
    }
});
app.UseDefaultFiles(); app.UseStaticFiles();
app.MapGet("/api/lesson", (IGameEngine engine, CancellationToken ct) => engine.GetLessonAsync(ct));
app.MapPost("/api/sessions", (StartRequest request, IGameEngine engine, CancellationToken ct) => engine.StartSessionAsync(request.LessonId, ct));
app.MapGet("/api/sessions/{id:guid}", (Guid id, IGameEngine engine, CancellationToken ct) => engine.GetSessionAsync(id, ct));
app.MapDelete("/api/sessions/{id:guid}", async (Guid id, IGameStore store, CancellationToken ct) =>
{
    await store.DeleteSessionAsync(id, ct);
    return Results.NoContent();
});
app.MapPost("/api/sessions/{id:guid}/answers", (Guid id, AnswerRequest request, IGameEngine engine, CancellationToken ct) =>
    engine.SubmitAnswerAsync(id, request.QuestionId, request.TurnId, request.Option, ct));
app.MapPost("/api/sessions/{id:guid}/repairs", (Guid id, RepairRequest request, IGameEngine engine, CancellationToken ct) => engine.StartRepairAsync(id, request.TopicId, ct));
app.Run();
record StartRequest(Guid LessonId);
record AnswerRequest(Guid QuestionId, Guid TurnId, string Option);
record RepairRequest(Guid TopicId);
