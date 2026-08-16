using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ChallengePrototype.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialLevelDesignFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExamSessions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ExamDate = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LevelDesigns",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LevelDesigns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LessonChallenges",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ExamSessionId = table.Column<long>(type: "INTEGER", nullable: false),
                    LessonId = table.Column<long>(type: "INTEGER", nullable: false),
                    LevelDesignId = table.Column<long>(type: "INTEGER", nullable: false),
                    LevelDesignVersion = table.Column<int>(type: "INTEGER", nullable: false),
                    CurrentLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LessonChallenges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LessonChallenges_ExamSessions_ExamSessionId",
                        column: x => x.ExamSessionId,
                        principalTable: "ExamSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LessonChallenges_LevelDesigns_LevelDesignId",
                        column: x => x.LevelDesignId,
                        principalTable: "LevelDesigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LevelRules",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LevelDesignId = table.Column<long>(type: "INTEGER", nullable: false),
                    ChallengeLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    AverageQuestionsPerTopic = table.Column<double>(type: "REAL", nullable: false),
                    MinimumQuestionsPerTopic = table.Column<int>(type: "INTEGER", nullable: false),
                    MaximumQuestionsPerTopic = table.Column<int>(type: "INTEGER", nullable: false),
                    MaximumTotalQuestions = table.Column<int>(type: "INTEGER", nullable: false),
                    EasyQuestionWeight = table.Column<double>(type: "REAL", nullable: false),
                    MediumQuestionWeight = table.Column<double>(type: "REAL", nullable: false),
                    HardQuestionWeight = table.Column<double>(type: "REAL", nullable: false),
                    VeryHardQuestionWeight = table.Column<double>(type: "REAL", nullable: false),
                    ImportanceFocus = table.Column<double>(type: "REAL", nullable: false),
                    StartingHealth = table.Column<double>(type: "REAL", nullable: false),
                    PromotionHealth = table.Column<double>(type: "REAL", nullable: false),
                    FailureHealth = table.Column<double>(type: "REAL", nullable: false),
                    MinimumEvidencePercent = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LevelRules", x => x.Id);
                    table.CheckConstraint("CK_LevelRule_Evidence", "MinimumEvidencePercent BETWEEN 0 AND 100");
                    table.CheckConstraint("CK_LevelRule_QuestionLimits", "MinimumQuestionsPerTopic > 0 AND MaximumQuestionsPerTopic >= MinimumQuestionsPerTopic");
                    table.ForeignKey(
                        name: "FK_LevelRules_LevelDesigns_LevelDesignId",
                        column: x => x.LevelDesignId,
                        principalTable: "LevelDesigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LevelProgresses",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LessonChallengeId = table.Column<long>(type: "INTEGER", nullable: false),
                    ChallengeLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    StartingHealth = table.Column<double>(type: "REAL", nullable: false),
                    CurrentHealth = table.Column<double>(type: "REAL", nullable: false),
                    TargetQuestionCount = table.Column<int>(type: "INTEGER", nullable: false),
                    QuestionsAnswered = table.Column<int>(type: "INTEGER", nullable: false),
                    CorrectAnswers = table.Column<int>(type: "INTEGER", nullable: false),
                    IncorrectAnswers = table.Column<int>(type: "INTEGER", nullable: false),
                    EvidenceRequired = table.Column<int>(type: "INTEGER", nullable: false),
                    EvidenceCollected = table.Column<int>(type: "INTEGER", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LevelProgresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LevelProgresses_LessonChallenges_LessonChallengeId",
                        column: x => x.LessonChallengeId,
                        principalTable: "LessonChallenges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TopicProgresses",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LessonChallengeId = table.Column<long>(type: "INTEGER", nullable: false),
                    TopicId = table.Column<long>(type: "INTEGER", nullable: false),
                    ImportanceSnapshot = table.Column<int>(type: "INTEGER", nullable: false),
                    QuestionsSeen = table.Column<int>(type: "INTEGER", nullable: false),
                    CorrectAnswers = table.Column<int>(type: "INTEGER", nullable: false),
                    IncorrectAnswers = table.Column<int>(type: "INTEGER", nullable: false),
                    FirstSeenAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    LastSeenAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TopicProgresses", x => x.Id);
                    table.CheckConstraint("CK_TopicProgress_Importance", "ImportanceSnapshot BETWEEN 1 AND 5");
                    table.ForeignKey(
                        name: "FK_TopicProgresses_LessonChallenges_LessonChallengeId",
                        column: x => x.LessonChallengeId,
                        principalTable: "LessonChallenges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChallengeRuns",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LevelProgressId = table.Column<long>(type: "INTEGER", nullable: false),
                    RunNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    EndedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    StartHealth = table.Column<double>(type: "REAL", nullable: false),
                    EndHealth = table.Column<double>(type: "REAL", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    StopReason = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChallengeRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChallengeRuns_LevelProgresses_LevelProgressId",
                        column: x => x.LevelProgressId,
                        principalTable: "LevelProgresses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QuestionAttempts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChallengeRunId = table.Column<long>(type: "INTEGER", nullable: false),
                    TopicProgressId = table.Column<long>(type: "INTEGER", nullable: false),
                    QuestionId = table.Column<long>(type: "INTEGER", nullable: false),
                    ChallengeLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    ActualQuestionDifficulty = table.Column<int>(type: "INTEGER", nullable: false),
                    ImportanceSnapshot = table.Column<int>(type: "INTEGER", nullable: false),
                    ShownAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    AnsweredAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    SelectedAnswer = table.Column<string>(type: "TEXT", nullable: true),
                    IsCorrect = table.Column<bool>(type: "INTEGER", nullable: true),
                    ActiveThinkingMilliseconds = table.Column<long>(type: "INTEGER", nullable: true),
                    HealthBefore = table.Column<double>(type: "REAL", nullable: false),
                    TimeHealthDelta = table.Column<double>(type: "REAL", nullable: false),
                    AnswerHealthDelta = table.Column<double>(type: "REAL", nullable: false),
                    HealthAfter = table.Column<double>(type: "REAL", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuestionAttempts_ChallengeRuns_ChallengeRunId",
                        column: x => x.ChallengeRunId,
                        principalTable: "ChallengeRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QuestionAttempts_TopicProgresses_TopicProgressId",
                        column: x => x.TopicProgressId,
                        principalTable: "TopicProgresses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "LevelDesigns",
                columns: new[] { "Id", "CreatedAt", "IsActive", "Name", "Version" },
                values: new object[] { 1L, new DateTimeOffset(new DateTime(2026, 8, 16, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "Default Challenge Design", 1 });

            migrationBuilder.InsertData(
                table: "LevelRules",
                columns: new[] { "Id", "AverageQuestionsPerTopic", "ChallengeLevel", "EasyQuestionWeight", "FailureHealth", "HardQuestionWeight", "ImportanceFocus", "LevelDesignId", "MaximumQuestionsPerTopic", "MaximumTotalQuestions", "MediumQuestionWeight", "MinimumEvidencePercent", "MinimumQuestionsPerTopic", "PromotionHealth", "StartingHealth", "VeryHardQuestionWeight" },
                values: new object[,]
                {
                    { 1L, 1.6000000000000001, 0, 0.5, 0.0, 0.20000000000000001, 1.0, 1L, 3, 24, 0.20000000000000001, 75, 1, 100.0, 50.0, 0.10000000000000001 },
                    { 2L, 2.0, 1, 0.25, 0.0, 0.25, 1.25, 1L, 3, 28, 0.40000000000000002, 75, 1, 100.0, 50.0, 0.10000000000000001 },
                    { 3L, 2.3999999999999999, 2, 0.14999999999999999, 0.0, 0.45000000000000001, 1.5, 1L, 4, 32, 0.20000000000000001, 75, 1, 100.0, 50.0, 0.20000000000000001 },
                    { 4L, 2.7999999999999998, 3, 0.050000000000000003, 0.0, 0.29999999999999999, 1.8, 1L, 4, 32, 0.14999999999999999, 75, 1, 100.0, 50.0, 0.5 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChallengeRuns_LevelProgressId_RunNumber",
                table: "ChallengeRuns",
                columns: new[] { "LevelProgressId", "RunNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExamSessions_UserId_Status",
                table: "ExamSessions",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_LessonChallenges_ExamSessionId_LessonId",
                table: "LessonChallenges",
                columns: new[] { "ExamSessionId", "LessonId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LessonChallenges_LevelDesignId",
                table: "LessonChallenges",
                column: "LevelDesignId");

            migrationBuilder.CreateIndex(
                name: "IX_LevelDesigns_Name_Version",
                table: "LevelDesigns",
                columns: new[] { "Name", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LevelProgresses_LessonChallengeId_ChallengeLevel",
                table: "LevelProgresses",
                columns: new[] { "LessonChallengeId", "ChallengeLevel" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LevelRules_LevelDesignId_ChallengeLevel",
                table: "LevelRules",
                columns: new[] { "LevelDesignId", "ChallengeLevel" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QuestionAttempts_ChallengeRunId_QuestionId",
                table: "QuestionAttempts",
                columns: new[] { "ChallengeRunId", "QuestionId" });

            migrationBuilder.CreateIndex(
                name: "IX_QuestionAttempts_TopicProgressId",
                table: "QuestionAttempts",
                column: "TopicProgressId");

            migrationBuilder.CreateIndex(
                name: "IX_TopicProgresses_LessonChallengeId_TopicId",
                table: "TopicProgresses",
                columns: new[] { "LessonChallengeId", "TopicId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LevelRules");

            migrationBuilder.DropTable(
                name: "QuestionAttempts");

            migrationBuilder.DropTable(
                name: "ChallengeRuns");

            migrationBuilder.DropTable(
                name: "TopicProgresses");

            migrationBuilder.DropTable(
                name: "LevelProgresses");

            migrationBuilder.DropTable(
                name: "LessonChallenges");

            migrationBuilder.DropTable(
                name: "ExamSessions");

            migrationBuilder.DropTable(
                name: "LevelDesigns");
        }
    }
}
