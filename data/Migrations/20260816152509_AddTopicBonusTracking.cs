using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChallengePrototype.data.Migrations
{
    /// <inheritdoc />
    public partial class AddTopicBonusTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TotalBonusEarned",
                table: "TopicProgresses",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BonusEarned",
                table: "QuestionAttempts",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("""
                UPDATE QuestionAttempts
                SET BonusEarned = CASE WHEN IsCorrect = 1 THEN CAST(ROUND(
                    10.0 *
                    CASE (SELECT ImportanceSnapshot FROM TopicProgresses WHERE Id = QuestionAttempts.TopicProgressId)
                        WHEN 1 THEN 1.0 WHEN 2 THEN 1.25 WHEN 3 THEN 1.5 WHEN 4 THEN 1.8 ELSE 2.2 END *
                    CASE ActualQuestionDifficulty WHEN 0 THEN 1.0 WHEN 1 THEN 1.4 WHEN 2 THEN 2.0 ELSE 3.0 END
                ) AS INTEGER) ELSE 0 END;

                UPDATE TopicProgresses
                SET TotalBonusEarned = COALESCE((
                    SELECT SUM(qa.BonusEarned)
                    FROM QuestionAttempts qa
                    JOIN ChallengeRuns cr ON cr.Id = qa.ChallengeRunId
                    WHERE qa.TopicProgressId = TopicProgresses.Id
                      AND COALESCE(cr.StopReason, '') <> 'Verification'
                ), 0);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TotalBonusEarned",
                table: "TopicProgresses");

            migrationBuilder.DropColumn(
                name: "BonusEarned",
                table: "QuestionAttempts");
        }
    }
}
