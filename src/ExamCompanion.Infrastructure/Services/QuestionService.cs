using System.Text.Json;
using ChallengePrototype.Models;
using Microsoft.Data.Sqlite;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace ChallengePrototype.Services;

public sealed class QuestionService(IWebHostEnvironment environment, ILogger<QuestionService> logger) : IQuestionService
{
    private string QuestionDb => Path.Combine(environment.ContentRootPath, "data", "QuestionBankOneQuestionPerTopic.sqlite");

    public async Task<ChallengeSetup> CreateFixedSetupAsync(CancellationToken cancellationToken)
    {
        await using var connection = new SqliteConnection($"Data Source={QuestionDb};Mode=ReadOnly");
        await connection.OpenAsync(cancellationToken);

        var lessonCommand = connection.CreateCommand();
        lessonCommand.CommandText = "SELECT lesson_title FROM mvp_challenge_config WHERE config_id=1";
        var lessonTitle = (string?)await lessonCommand.ExecuteScalarAsync(cancellationToken)
            ?? throw new InvalidOperationException("تنظیم ثابت چالش MVP در پایگاه داده وجود ندارد.");
        var lessonIdCommand = connection.CreateCommand();
        lessonIdCommand.CommandText = "SELECT lesson_id FROM mvp_challenge_config WHERE config_id=1";
        var lessonId = Convert.ToInt64(await lessonIdCommand.ExecuteScalarAsync(cancellationToken));

        var topics = new List<GameTopic>();
        var topicCommand = connection.CreateCommand();
        topicCommand.CommandText = "SELECT topic_id,topic_title,importance FROM mvp_challenge_topics ORDER BY position";
        await using (var reader = await topicCommand.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
                topics.Add(new GameTopic(reader.GetInt64(0), reader.GetString(1), reader.GetDouble(2)));
        }
        if (topics.Count != 5) throw new InvalidOperationException($"چالش MVP باید دقیقاً پنج موضوع داشته باشد؛ تعداد فعلی: {topics.Count}.");

        var topicById = topics.ToDictionary(x => x.Id);
        var questions = new List<ChallengeQuestion>();
        var questionCommand = connection.CreateCommand();
        questionCommand.CommandText = """
            SELECT question_id,topic_id,title_html,answers_json,answer_key,level
            FROM mvp_challenge_questions
            ORDER BY topic_id,question_id
            """;
        await using (var reader = await questionCommand.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                if (!topicById.TryGetValue(reader.GetInt64(1), out var topic)) continue;
                var answers = JsonSerializer.Deserialize<List<AnswerJson>>(reader.GetString(3), JsonOptions) ?? [];
                if (answers.Count != 4) continue;
                var databaseLevel = reader.IsDBNull(5) ? null : reader.GetString(5);
                var difficulty = NormalizeLevel(databaseLevel) ?? GameDifficulty.Medium;
                questions.Add(new ChallengeQuestion(reader.GetInt64(0), topic, reader.GetString(2),
                    answers.Select(x => new AnswerOption(x.Option, x.Answer)).ToArray(),
                    reader.GetString(4), difficulty, databaseLevel, false));
            }
        }
        if (questions.Count == 0) throw new InvalidOperationException("برای پنج موضوع ثابت چالش، سؤالی ذخیره نشده است.");
        var missing = topics.Where(topic => questions.All(question => question.Topic.Id != topic.Id)).ToArray();
        if (missing.Length > 0)
            throw new InvalidOperationException($"موضوع‌های بدون سؤال: {string.Join("، ", missing.Select(x => x.Title))}");

        logger.LogInformation("Loaded fixed MVP lesson {Lesson}, {TopicCount} topics and {QuestionCount} questions.",
            lessonTitle, topics.Count, questions.Count);
        return new ChallengeSetup(lessonId, lessonTitle, topics, questions);
    }

    private static GameDifficulty? NormalizeLevel(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        "easy" => GameDifficulty.Easy,
        "medium" => GameDifficulty.Medium,
        "hard" => GameDifficulty.Hard,
        "very hard" or "veryhard" => GameDifficulty.VeryHard,
        _ => null
    };

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private sealed record AnswerJson(string Answer, string Option);
}
