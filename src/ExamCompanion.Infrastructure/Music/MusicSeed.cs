using System.Net;
using System.Text.RegularExpressions;
using ChallengePrototype.Services;
using ExamCompanion.Domain.Music;
using Microsoft.EntityFrameworkCore;

namespace ExamCompanion.Infrastructure.Music;

public sealed partial class MusicSeed(MusicDbContext db, IQuestionService bank, IEducationalQuestionSource educational)
{
    public async Task SeedAsync(CancellationToken ct = default)
    {
        if (await db.Lessons.AnyAsync(ct)) return;
        var setup = await bank.CreateFixedSetupAsync(ct);
        var lesson = new Lesson { SourceId = setup.LessonId, Name = setup.LessonTitle, Slug = "biology-1" };
        string[] colors = ["#c6f278", "#89d9ef", "#bc9cf4", "#ffa89b", "#ffe090"];
        string[] keys = ["C", "Dm", "Em", "F", "G"];
        foreach (var source in setup.Topics)
        {
            var i = lesson.Topics.Count;
            var topic = new Topic { SourceId = source.Id, Name = source.Title, Importance = source.Importance,
                DisplayOrder = i, MusicalColor = colors[i % 5], MusicalKey = keys[i % 5], Description = "یک قطعه از دنیای زنده" };
            foreach (var q in await educational.GetQuestionsAsync(source.Id, ct))
            {
                var choices = q.Answers.ToArray();
                topic.Questions.Add(new Question { SourceId = q.Id, Type = QuestionType.Educational, Text = Plain(q.Text),
                    OptionA = Plain(choices[0].Text), OptionB = Plain(choices[1].Text),
                    CorrectOption = choices[0].IsCorrect ? "A" : "B", Difficulty = (int)q.Difficulty, Explanation = q.Explanation });
            }
            foreach (var q in setup.Questions.Where(q => q.Topic.Id == source.Id))
            {
                var choices = q.Options.ToArray();
                var index = Array.FindIndex(choices, a => a.Option == q.CorrectOption);
                if (choices.Length != 4 || index < 0) continue;
                topic.Questions.Add(new Question { SourceId = q.Id.ToString(), Type = QuestionType.Challenge, Text = Plain(q.TitleHtml),
                    OptionA = Plain(choices[0].Html), OptionB = Plain(choices[1].Html), OptionC = Plain(choices[2].Html),
                    OptionD = Plain(choices[3].Html), CorrectOption = ((char)('A' + index)).ToString(), Difficulty = (int)q.RequestedDifficulty });
            }
            if (topic.Questions.Count(q => q.Type == QuestionType.Educational) < 8 || topic.Questions.Count(q => q.Type == QuestionType.Challenge) < 5)
                throw new InvalidOperationException($"Insufficient source questions for topic {source.Id}.");
            lesson.Topics.Add(topic);
        }
        db.Exams.Add(new Exam { Name = "آلبوم زیست‌شناسی", ExamDate = new DateTime(2027, 6, 1) });
        db.Lessons.Add(lesson);
        await db.SaveChangesAsync(ct);
    }
    private static string Plain(string text) => Spaces().Replace(WebUtility.HtmlDecode(Tags().Replace(text.Replace("\\n", " "), " ")), " ").Trim();
    [GeneratedRegex("<[^>]+>")] private static partial Regex Tags();
    [GeneratedRegex(@"\s+")] private static partial Regex Spaces();
}
