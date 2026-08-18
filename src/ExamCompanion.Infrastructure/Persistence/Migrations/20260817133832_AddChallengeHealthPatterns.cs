using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExamCompanion.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddChallengeHealthPatterns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChallengeHealthPatterns",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChallengeLevelProgressId = table.Column<long>(type: "INTEGER", nullable: false),
                    PatternVersion = table.Column<int>(type: "INTEGER", nullable: false),
                    ChallengeLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    TopicCount = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalImportance = table.Column<int>(type: "INTEGER", nullable: false),
                    AverageImportance = table.Column<double>(type: "REAL", nullable: false),
                    TargetQuestionCount = table.Column<int>(type: "INTEGER", nullable: false),
                    StartingHealth = table.Column<double>(type: "REAL", nullable: false),
                    PromotionHealth = table.Column<double>(type: "REAL", nullable: false),
                    FailureHealth = table.Column<double>(type: "REAL", nullable: false),
                    PromotionDistance = table.Column<double>(type: "REAL", nullable: false),
                    HealthUnit = table.Column<double>(type: "REAL", nullable: false),
                    TargetAccuracy = table.Column<double>(type: "REAL", nullable: false),
                    WrongSeverity = table.Column<double>(type: "REAL", nullable: false),
                    ExpectedTimeBudgetFraction = table.Column<double>(type: "REAL", nullable: false),
                    ImportanceSensitivity = table.Column<double>(type: "REAL", nullable: false),
                    BaseCorrectGain = table.Column<double>(type: "REAL", nullable: false),
                    BaseWrongDamage = table.Column<double>(type: "REAL", nullable: false),
                    BaseTimeBudgetPerQuestion = table.Column<double>(type: "REAL", nullable: false),
                    GeneratedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChallengeHealthPatterns", x => x.Id);
                    table.CheckConstraint("CK_ChallengeHealthPattern_Health", "PromotionHealth > StartingHealth AND StartingHealth > FailureHealth");
                    table.CheckConstraint("CK_ChallengeHealthPattern_Questions", "TargetQuestionCount > 0");
                    table.CheckConstraint("CK_ChallengeHealthPattern_Topics", "TopicCount > 0");
                    table.CheckConstraint("CK_ChallengeHealthPattern_Version", "PatternVersion > 0");
                    table.ForeignKey(
                        name: "FK_ChallengeHealthPatterns_LevelProgresses_ChallengeLevelProgressId",
                        column: x => x.ChallengeLevelProgressId,
                        principalTable: "LevelProgresses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChallengeHealthDifficultyFactors",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChallengeHealthPatternId = table.Column<long>(type: "INTEGER", nullable: false),
                    QuestionDifficulty = table.Column<int>(type: "INTEGER", nullable: false),
                    CorrectMultiplier = table.Column<double>(type: "REAL", nullable: false),
                    WrongMultiplier = table.Column<double>(type: "REAL", nullable: false),
                    TimeMultiplier = table.Column<double>(type: "REAL", nullable: false),
                    GracePeriodMilliseconds = table.Column<int>(type: "INTEGER", nullable: false),
                    PressureWindowSeconds = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChallengeHealthDifficultyFactors", x => x.Id);
                    table.CheckConstraint("CK_ChallengeHealthDifficultyFactor_Timing", "GracePeriodMilliseconds >= 0 AND PressureWindowSeconds > 0");
                    table.ForeignKey(
                        name: "FK_ChallengeHealthDifficultyFactors_ChallengeHealthPatterns_ChallengeHealthPatternId",
                        column: x => x.ChallengeHealthPatternId,
                        principalTable: "ChallengeHealthPatterns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChallengeHealthDifficultyFactors_ChallengeHealthPatternId_QuestionDifficulty",
                table: "ChallengeHealthDifficultyFactors",
                columns: new[] { "ChallengeHealthPatternId", "QuestionDifficulty" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChallengeHealthPatterns_ChallengeLevelProgressId",
                table: "ChallengeHealthPatterns",
                column: "ChallengeLevelProgressId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChallengeHealthDifficultyFactors");

            migrationBuilder.DropTable(
                name: "ChallengeHealthPatterns");
        }
    }
}
