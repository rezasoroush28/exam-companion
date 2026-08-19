using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExamCompanion.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRecoveryMiniGame : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RecoverySuggestionSets",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LessonChallengeId = table.Column<long>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecoverySuggestionSets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecoverySuggestionSets_LessonChallenges_LessonChallengeId",
                        column: x => x.LessonChallengeId,
                        principalTable: "LessonChallenges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RecoverySuggestionItems",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SetId = table.Column<long>(type: "INTEGER", nullable: false),
                    TopicProgressId = table.Column<long>(type: "INTEGER", nullable: false),
                    TopicId = table.Column<long>(type: "INTEGER", nullable: false),
                    Rank = table.Column<int>(type: "INTEGER", nullable: false),
                    ImportanceSnapshot = table.Column<int>(type: "INTEGER", nullable: false),
                    QuestionsSeenSnapshot = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalBonusSnapshot = table.Column<int>(type: "INTEGER", nullable: false),
                    AverageBonusSnapshot = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecoverySuggestionItems", x => x.Id);
                    table.CheckConstraint("CK_RecoverySuggestionItem_Rank", "Rank BETWEEN 1 AND 3");
                    table.ForeignKey(
                        name: "FK_RecoverySuggestionItems_RecoverySuggestionSets_SetId",
                        column: x => x.SetId,
                        principalTable: "RecoverySuggestionSets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RecoverySuggestionItems_TopicProgresses_TopicProgressId",
                        column: x => x.TopicProgressId,
                        principalTable: "TopicProgresses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StudyClaims",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LessonChallengeId = table.Column<long>(type: "INTEGER", nullable: false),
                    SetId = table.Column<long>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ClaimedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudyClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudyClaims_LessonChallenges_LessonChallengeId",
                        column: x => x.LessonChallengeId,
                        principalTable: "LessonChallenges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StudyClaims_RecoverySuggestionSets_SetId",
                        column: x => x.SetId,
                        principalTable: "RecoverySuggestionSets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RecoveryMiniGameSessions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LessonChallengeId = table.Column<long>(type: "INTEGER", nullable: false),
                    StudyClaimId = table.Column<long>(type: "INTEGER", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CurrentTopicIndex = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecoveryMiniGameSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecoveryMiniGameSessions_LessonChallenges_LessonChallengeId",
                        column: x => x.LessonChallengeId,
                        principalTable: "LessonChallenges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RecoveryMiniGameSessions_StudyClaims_StudyClaimId",
                        column: x => x.StudyClaimId,
                        principalTable: "StudyClaims",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StudyClaimTopics",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ClaimId = table.Column<long>(type: "INTEGER", nullable: false),
                    SuggestionItemId = table.Column<long>(type: "INTEGER", nullable: false),
                    TopicProgressId = table.Column<long>(type: "INTEGER", nullable: false),
                    TopicId = table.Column<long>(type: "INTEGER", nullable: false),
                    Importance = table.Column<int>(type: "INTEGER", nullable: false),
                    Sequence = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudyClaimTopics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudyClaimTopics_RecoverySuggestionItems_SuggestionItemId",
                        column: x => x.SuggestionItemId,
                        principalTable: "RecoverySuggestionItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudyClaimTopics_StudyClaims_ClaimId",
                        column: x => x.ClaimId,
                        principalTable: "StudyClaims",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StudyClaimTopics_TopicProgresses_TopicProgressId",
                        column: x => x.TopicProgressId,
                        principalTable: "TopicProgresses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RecoveryTopicSessions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MiniGameSessionId = table.Column<long>(type: "INTEGER", nullable: false),
                    TopicProgressId = table.Column<long>(type: "INTEGER", nullable: false),
                    TopicId = table.Column<long>(type: "INTEGER", nullable: false),
                    Importance = table.Column<int>(type: "INTEGER", nullable: false),
                    Sequence = table.Column<int>(type: "INTEGER", nullable: false),
                    RequiredCorrect = table.Column<int>(type: "INTEGER", nullable: false),
                    Correct = table.Column<int>(type: "INTEGER", nullable: false),
                    Incorrect = table.Column<int>(type: "INTEGER", nullable: false),
                    Asked = table.Column<int>(type: "INTEGER", nullable: false),
                    Stability = table.Column<double>(type: "REAL", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    StabilizedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecoveryTopicSessions", x => x.Id);
                    table.CheckConstraint("CK_RecoveryTopicSession_Stability", "Stability BETWEEN 0 AND 1 AND RequiredCorrect > 0");
                    table.ForeignKey(
                        name: "FK_RecoveryTopicSessions_RecoveryMiniGameSessions_MiniGameSessionId",
                        column: x => x.MiniGameSessionId,
                        principalTable: "RecoveryMiniGameSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RecoveryTopicSessions_TopicProgresses_TopicProgressId",
                        column: x => x.TopicProgressId,
                        principalTable: "TopicProgresses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RecoveryQuestionAttempts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SessionId = table.Column<long>(type: "INTEGER", nullable: false),
                    TopicSessionId = table.Column<long>(type: "INTEGER", nullable: false),
                    QuestionId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    TopicId = table.Column<long>(type: "INTEGER", nullable: false),
                    ActualDifficulty = table.Column<int>(type: "INTEGER", nullable: false),
                    ShownAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    AnsweredAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    SelectedAnswer = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    IsCorrect = table.Column<bool>(type: "INTEGER", nullable: false),
                    ActiveThinkingMs = table.Column<long>(type: "INTEGER", nullable: false),
                    HintLevelUsed = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecoveryQuestionAttempts", x => x.Id);
                    table.CheckConstraint("CK_RecoveryQuestionAttempt_Hint", "HintLevelUsed BETWEEN 0 AND 3");
                    table.ForeignKey(
                        name: "FK_RecoveryQuestionAttempts_RecoveryMiniGameSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "RecoveryMiniGameSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RecoveryQuestionAttempts_RecoveryTopicSessions_TopicSessionId",
                        column: x => x.TopicSessionId,
                        principalTable: "RecoveryTopicSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RecoveryMiniGameSessions_LessonChallengeId",
                table: "RecoveryMiniGameSessions",
                column: "LessonChallengeId");

            migrationBuilder.CreateIndex(
                name: "IX_RecoveryMiniGameSessions_StudyClaimId",
                table: "RecoveryMiniGameSessions",
                column: "StudyClaimId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RecoveryQuestionAttempts_SessionId_QuestionId",
                table: "RecoveryQuestionAttempts",
                columns: new[] { "SessionId", "QuestionId" });

            migrationBuilder.CreateIndex(
                name: "IX_RecoveryQuestionAttempts_TopicSessionId",
                table: "RecoveryQuestionAttempts",
                column: "TopicSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_RecoverySuggestionItems_SetId_Rank",
                table: "RecoverySuggestionItems",
                columns: new[] { "SetId", "Rank" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RecoverySuggestionItems_SetId_TopicId",
                table: "RecoverySuggestionItems",
                columns: new[] { "SetId", "TopicId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RecoverySuggestionItems_TopicProgressId",
                table: "RecoverySuggestionItems",
                column: "TopicProgressId");

            migrationBuilder.CreateIndex(
                name: "IX_RecoverySuggestionSets_LessonChallengeId_Status",
                table: "RecoverySuggestionSets",
                columns: new[] { "LessonChallengeId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_RecoveryTopicSessions_MiniGameSessionId_Sequence",
                table: "RecoveryTopicSessions",
                columns: new[] { "MiniGameSessionId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RecoveryTopicSessions_TopicProgressId",
                table: "RecoveryTopicSessions",
                column: "TopicProgressId");

            migrationBuilder.CreateIndex(
                name: "IX_StudyClaims_LessonChallengeId",
                table: "StudyClaims",
                column: "LessonChallengeId");

            migrationBuilder.CreateIndex(
                name: "IX_StudyClaims_SetId",
                table: "StudyClaims",
                column: "SetId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudyClaimTopics_ClaimId_Sequence",
                table: "StudyClaimTopics",
                columns: new[] { "ClaimId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudyClaimTopics_ClaimId_TopicId",
                table: "StudyClaimTopics",
                columns: new[] { "ClaimId", "TopicId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudyClaimTopics_SuggestionItemId",
                table: "StudyClaimTopics",
                column: "SuggestionItemId");

            migrationBuilder.CreateIndex(
                name: "IX_StudyClaimTopics_TopicProgressId",
                table: "StudyClaimTopics",
                column: "TopicProgressId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RecoveryQuestionAttempts");

            migrationBuilder.DropTable(
                name: "StudyClaimTopics");

            migrationBuilder.DropTable(
                name: "RecoveryTopicSessions");

            migrationBuilder.DropTable(
                name: "RecoverySuggestionItems");

            migrationBuilder.DropTable(
                name: "RecoveryMiniGameSessions");

            migrationBuilder.DropTable(
                name: "StudyClaims");

            migrationBuilder.DropTable(
                name: "RecoverySuggestionSets");
        }
    }
}
