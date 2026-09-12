using ExamCompanion.Application.Music;
using ExamCompanion.Domain.Music;
using ExamCompanion.Infrastructure.Music;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace ExamCompanion.Application.Tests;

public sealed class GameEngineTests
{
    [Theory]
    [InlineData(.2, 1)] [InlineData(.339, 1)] [InlineData(.34, 2)] [InlineData(.5, 2)] [InlineData(.67, 3)] [InlineData(.9, 3)]
    public void ImportanceControlsCycles(double importance, int cycles) => Assert.Equal(cycles, GameRulesOptions.Cycles(importance));

    [Fact] public async Task EducationalMistakesAddReinforcementButStopAtFour()
    {
        await using var h = await Harness.Create(); var state = await h.Start();
        state = (await h.Answer(state, false)).State;
        Assert.Equal(3, state.EducationalTarget);
        for (var i = 0; i < 3; i++) state = (await h.Answer(state, false)).State;
        Assert.Equal(4, state.EducationalAnswered); Assert.Equal(4, state.EducationalTarget);
        Assert.Equal(GameStepType.Challenge, state.Step); Assert.True(state.Topics[0].HasScratch);
    }
    [Fact] public async Task ChallengeSuccessClosesCycleAndUnlocksOnlyNextTopic()
    {
        await using var h = await Harness.Create(); var state = await h.Start();
        state = (await h.Answer(state, true)).State;
        Assert.Equal(TopicProgressStatus.Locked, state.Topics[1].Status);
        Assert.Equal(0, state.Topics[0].CompletedCycles);
        state = (await h.Answer(state, true)).State;
        var result = await h.Answer(state, true);
        Assert.Equal(AnswerResultType.TopicCompleted, result.Result);
        Assert.Equal(TopicProgressStatus.CompletedClean, result.State.Topics[0].Status);
        Assert.Equal(TopicProgressStatus.Active, result.State.Topics[1].Status);
    }
    [Fact] public async Task SecondChallengeFailureClosesCycleWithScratch()
    {
        await using var h = await Harness.Create(); var s = await h.Start();
        s = (await h.Answer(s, true)).State; s = (await h.Answer(s, true)).State;
        s = (await h.Answer(s, false)).State; Assert.Equal(GameStepType.Reinforcement, s.Step);
        s = (await h.Answer(s, true)).State; Assert.Equal(GameStepType.Challenge, s.Step);
        s = (await h.Answer(s, false)).State;
        Assert.Equal(TopicProgressStatus.CompletedWithScratch, s.Topics[0].Status);
        Assert.Equal(s.Topics[1].Id, s.CurrentTopicId);
    }
    [Fact] public async Task RepairClearsScratchAndPreservesNextTopicProgress()
    {
        await using var h = await Harness.Create(); var s = await h.Start();
        for (int i = 0; i < 6; i++) s = (await h.Answer(s, false)).State;
        s = (await h.Answer(s, false)).State;
        var topic = s.Topics[0]; Assert.True(topic.HasScratch);
        s = await h.Engine.StartRepairAsync(s.SessionId, topic.Id);
        Assert.Equal(GameStepType.RepairEducational, s.Step);
        s = (await h.Answer(s, true)).State;
        s = (await h.Answer(s, true)).State;
        Assert.True(s.Topics[0].IsRepaired); Assert.False(s.Topics[0].HasScratch);
        Assert.Equal(s.Topics[1].Id, s.CurrentTopicId); Assert.Equal(0, s.EducationalAnswered);
    }
    [Fact] public async Task FailedRepairKeepsScratchAndAllowsRetry()
    {
        await using var h = await Harness.Create(1); var s = await h.Start();
        while (s.Question != null) s = (await h.Answer(s, false)).State;
        s = await h.Engine.StartRepairAsync(s.SessionId, s.Topics[0].Id);
        s = (await h.Answer(s, false)).State;
        var result = await h.Answer(s, true);
        Assert.Equal(AnswerResultType.RepairFailed, result.Result); Assert.True(result.State.Topics[0].HasScratch);
        Assert.True((await h.Engine.StartRepairAsync(s.SessionId, s.Topics[0].Id)).IsRepair);
    }
    [Fact] public async Task RepairCannotSkipPendingChallengeReinforcement()
    {
        await using var h = await Harness.Create(); var s = await h.Start();
        for (var i = 0; i < 7; i++) s = (await h.Answer(s, false)).State;
        s = (await h.Answer(s, true)).State; s = (await h.Answer(s, true)).State;
        s = (await h.Answer(s, false)).State;
        Assert.Equal(GameStepType.Reinforcement, s.Step);
        var pending = s.Question!.Id;
        s = await h.Engine.StartRepairAsync(s.SessionId, s.Topics[0].Id);
        s = (await h.Answer(s, true)).State; s = (await h.Answer(s, true)).State;
        Assert.Equal(GameStepType.Reinforcement, s.Step);
        Assert.Equal(pending, s.Question!.Id);
        Assert.Equal(1, s.ChallengeAttempts);
    }
    [Fact] public async Task LastTopicCompletesPersistentSessionAndCanBeReloaded()
    {
        await using var h = await Harness.Create(); var s = await h.Start(); int count = 0;
        while (s.Question != null && count++ < 40) s = (await h.Answer(s, true)).State;
        Assert.Equal(18, count); Assert.Equal(GameSessionStatus.Completed, s.Status);
        var loaded = await h.Engine.GetSessionAsync(s.SessionId);
        Assert.Null(loaded.Question); Assert.All(loaded.Topics, t => Assert.Equal(1, t.Progress));
        await using var db = h.Factory.CreateDbContext(); Assert.Equal(18, await db.Attempts.CountAsync());
    }
    [Fact] public async Task RejectsDuplicateInvalidOptionAndLockedRepair()
    {
        await using var h = await Harness.Create(); var s = await h.Start();
        await Assert.ThrowsAsync<GameInputException>(() => h.Engine.SubmitAnswerAsync(s.SessionId, s.Question!.Id, s.TurnId, "Z"));
        await Assert.ThrowsAsync<GameInputException>(() => h.Engine.StartRepairAsync(s.SessionId, s.Topics[1].Id));
        var next = await h.Answer(s, true);
        await Assert.ThrowsAsync<GameInputException>(() => h.Engine.SubmitAnswerAsync(s.SessionId, s.Question!.Id, s.TurnId, "A"));
        Assert.NotEqual(s.Question!.Id, next.State.Question!.Id);
    }
    [Fact] public async Task InterruptedSessionResumesExactQuestionAndTurn()
    {
        await using var h = await Harness.Create(); var s = await h.Start(); s = (await h.Answer(s, false)).State;
        var loaded = await h.Engine.GetSessionAsync(s.SessionId);
        Assert.Equal(s.Question!.Id, loaded.Question!.Id); Assert.Equal(s.TurnId, loaded.TurnId);
        Assert.Equal(s.EducationalTarget, loaded.EducationalTarget);
    }
    [Fact] public async Task ParallelContextsRejectStaleSave()
    {
        await using var h = await Harness.Create(); var s = await h.Start();
        await using var a = new MusicGameStore(h.Factory); await using var b = new MusicGameStore(h.Factory);
        var first = (await a.GetSessionAsync(s.SessionId))!; var second = (await b.GetSessionAsync(s.SessionId))!;
        first.Revision = Guid.NewGuid(); await a.SaveAsync(); second.Revision = Guid.NewGuid();
        await Assert.ThrowsAsync<GameInputException>(() => b.SaveAsync());
    }
}

internal sealed class Harness : IAsyncDisposable
{
    private readonly SqliteConnection connection;
    public TestFactory Factory { get; }
    private readonly MusicGameStore store;
    public IGameEngine Engine { get; }
    private Guid lessonId;
    private Harness(SqliteConnection connection)
    {
        this.connection = connection; Factory = new(new DbContextOptionsBuilder<MusicDbContext>().UseSqlite(connection).Options);
        store = new MusicGameStore(Factory);
        Engine = new MusicGameEngine(store, new QuestionSelector(), Options.Create(new GameRulesOptions()), NullLogger<MusicGameEngine>.Instance);
    }
    public static async Task<Harness> Create(int topicCount = 3)
    {
        var c = new SqliteConnection("Data Source=:memory:"); await c.OpenAsync(); var h = new Harness(c);
        await using var db = h.Factory.CreateDbContext(); await db.Database.MigrateAsync();
        var lesson = new Lesson { Name = "Biology", Slug = "bio" };
        for (int i = 0; i < topicCount; i++)
        {
            var topic = new Topic { Name = "Topic " + i, DisplayOrder = i, Importance = new[] { .2, .5, .9 }[i] };
            foreach (var type in Enum.GetValues<QuestionType>())
                for (int q = 0; q < 8; q++) topic.Questions.Add(new Question { SourceId = type + q.ToString(), Type = type, Text = "Question " + q,
                    OptionA = "Yes", OptionB = "No", OptionC = type == QuestionType.Challenge ? "C" : null,
                    OptionD = type == QuestionType.Challenge ? "D" : null, CorrectOption = "A", Difficulty = q % 3 });
            lesson.Topics.Add(topic);
        }
        db.Lessons.Add(lesson); await db.SaveChangesAsync(); h.lessonId = lesson.Id; return h;
    }
    public Task<GameStateDto> Start() => Engine.StartSessionAsync(lessonId);
    public Task<AnswerResultDto> Answer(GameStateDto s, bool correct) => Engine.SubmitAnswerAsync(s.SessionId, s.Question!.Id, s.TurnId, correct ? "A" : "B");
    public async ValueTask DisposeAsync() { await store.DisposeAsync(); await connection.DisposeAsync(); }
}
internal sealed class TestFactory(DbContextOptions<MusicDbContext> options) : IDbContextFactory<MusicDbContext>
{
    public MusicDbContext CreateDbContext() => new(options);
}
