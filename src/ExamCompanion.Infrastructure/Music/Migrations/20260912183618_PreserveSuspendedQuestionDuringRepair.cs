using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExamCompanion.Infrastructure.Music.Migrations
{
    /// <inheritdoc />
    public partial class PreserveSuspendedQuestionDuringRepair : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SuspendedQuestionId",
                table: "Sessions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SuspendedStep",
                table: "Sessions",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SuspendedQuestionId",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "SuspendedStep",
                table: "Sessions");
        }
    }
}
