using ChallengePrototype.Components;
using ChallengePrototype.Services;
using ExamCompanion.Application.Music;
using ExamCompanion.Domain.Music;
using ExamCompanion.Infrastructure.Music;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddOptions<GameRulesOptions>().Bind(builder.Configuration.GetSection("MusicGameRules"))
    .Validate(r => r.BaseEducationalQuestionCount >= 1 && r.MaxEducationalQuestionCount >= r.BaseEducationalQuestionCount
        && r.MaxChallengeAttempts >= 1 && r.WrongAnswersForScratch >= 1 && r.ReinforcementsForScratch >= 1,
        "Game rules must have valid positive thresholds.").ValidateOnStart();
builder.Services.AddDbContextFactory<MusicDbContext>(o => o.UseSqlite(
    builder.Configuration.GetConnectionString("MusicGame") ?? "Data Source=data/exam-companion.db"));
builder.Services.AddScoped<IGameStore, MusicGameStore>();
builder.Services.AddScoped<IGameEngine, MusicGameEngine>();
builder.Services.AddSingleton<IQuestionSelector, QuestionSelector>();
builder.Services.AddSingleton<IQuestionService, QuestionService>();
builder.Services.AddSingleton<IEducationalQuestionSource, EducationalQuestionSource>();
builder.Services.AddScoped<MusicSeed>();
var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<MusicDbContext>().Database.MigrateAsync();
    await scope.ServiceProvider.GetRequiredService<MusicSeed>().SeedAsync();
}
if (!app.Environment.IsDevelopment()) app.UseExceptionHandler("/Error", createScopeForErrors: true);
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.Run();
