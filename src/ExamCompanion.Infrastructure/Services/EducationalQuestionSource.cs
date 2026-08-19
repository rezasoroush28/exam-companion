using ChallengePrototype.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.Sqlite;

namespace ChallengePrototype.Services;

public sealed class EducationalQuestionSource(IWebHostEnvironment environment) : IEducationalQuestionSource
{
    private string DatabasePath => Path.Combine(environment.ContentRootPath, "data", "TopicEducationalQuestions.sqlite");

    public async Task<IReadOnlyDictionary<long, string>> GetTopicTitlesAsync(IEnumerable<long> topicIds,
        CancellationToken cancellationToken = default)
    {
        var ids = topicIds.Distinct().ToArray();
        if (ids.Length == 0) return new Dictionary<long, string>();
        await using var connection = await OpenAsync(cancellationToken);
        var command = connection.CreateCommand();
        command.CommandText = $"SELECT topic_id, topic_name FROM educational_topics WHERE topic_id IN ({string.Join(',', ids.Select((_, i) => $"$p{i}"))})";
        for (var i = 0; i < ids.Length; i++) command.Parameters.AddWithValue($"$p{i}", ids[i]);
        var result = new Dictionary<long, string>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) result[reader.GetInt64(0)] = reader.GetString(1);
        return result;
    }

    public async Task<IReadOnlyList<EducationalQuestion>> GetQuestionsAsync(long topicId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);
        var command = connection.CreateCommand();
        command.CommandText = """
            SELECT q.question_id, q.topic_id, t.topic_name, q.question_text,
                   q.educational_explanation, q.difficulty, c.option, c.choice_text, c.is_correct
            FROM educational_questions q
            JOIN educational_topics t ON t.topic_id=q.topic_id
            JOIN educational_question_choices c ON c.question_id=q.question_id
            WHERE q.topic_id=$topicId
            ORDER BY q.position, c.position
            """;
        command.Parameters.AddWithValue("$topicId", topicId);
        var rows = new List<QuestionRow>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            rows.Add(new QuestionRow(reader.GetString(0), reader.GetInt64(1), reader.GetString(2), reader.GetString(3),
                reader.IsDBNull(4) ? null : reader.GetString(4), ParseDifficulty(reader.GetString(5)),
                reader.GetString(6), reader.GetString(7), reader.GetInt64(8) == 1));
        return rows.GroupBy(x => new { x.Id, x.TopicId, x.TopicTitle, x.Text, x.Explanation, x.Difficulty })
            .Select(group => new EducationalQuestion(group.Key.Id, group.Key.TopicId, group.Key.TopicTitle,
                group.Key.Text, group.Key.Explanation, group.Key.Difficulty,
                group.Select(x => new EducationalAnswer(x.Option, x.Answer, x.Correct)).ToArray()))
            .Where(x => x.Answers.Count == 2 && x.Answers.Count(a => a.IsCorrect) == 1)
            .ToArray();
    }

    private async Task<SqliteConnection> OpenAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(DatabasePath)) throw new FileNotFoundException("Educational question database not found.", DatabasePath);
        var connection = new SqliteConnection($"Data Source={DatabasePath};Mode=ReadOnly");
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    private static QuestionDifficulty ParseDifficulty(string value) => value.Trim().ToLowerInvariant() switch
    {
        "easy" => QuestionDifficulty.Easy,
        "medium" => QuestionDifficulty.Medium,
        "hard" => QuestionDifficulty.Hard,
        "very_hard" or "veryhard" or "very hard" => QuestionDifficulty.VeryHard,
        _ => QuestionDifficulty.Medium
    };

    private sealed record QuestionRow(string Id, long TopicId, string TopicTitle, string Text,
        string? Explanation, QuestionDifficulty Difficulty, string Option, string Answer, bool Correct);
}
