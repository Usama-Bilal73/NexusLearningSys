using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexus.Data.Migrations
{
    /// <inheritdoc />
    public partial class PendingChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Submissions",
                schema: "nexus",
                table: "Submissions");

            migrationBuilder.AddColumn<int>(
                name: "Id",
                schema: "nexus",
                table: "Submissions",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<string>(
                name: "Feedback",
                schema: "nexus",
                table: "Submissions",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                schema: "nexus",
                table: "Submissions",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Submissions",
                schema: "nexus",
                table: "Submissions",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "CourseMaterials",
                schema: "nexus",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    MaterialType = table.Column<int>(type: "int", nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    UploadedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UploadedByTeacherId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CourseId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseMaterials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseMaterials_AspNetUsers_UploadedByTeacherId",
                        column: x => x.UploadedByTeacherId,
                        principalSchema: "nexus",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CourseMaterials_Courses_CourseId",
                        column: x => x.CourseId,
                        principalSchema: "nexus",
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Quizzes",
                schema: "nexus",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DurationMinutes = table.Column<int>(type: "int", nullable: false),
                    OpensAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClosesAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    CourseId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Quizzes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Quizzes_Courses_CourseId",
                        column: x => x.CourseId,
                        principalSchema: "nexus",
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Questions",
                schema: "nexus",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Text = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    OptionA = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    OptionB = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    OptionC = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    OptionD = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    CorrectOption = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: false),
                    Points = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    QuizId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Questions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Questions_Quizzes_QuizId",
                        column: x => x.QuizId,
                        principalSchema: "nexus",
                        principalTable: "Quizzes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QuizAttempts",
                schema: "nexus",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QuizId = table.Column<int>(type: "int", nullable: false),
                    StudentId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SubmittedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsAutoSubmitted = table.Column<bool>(type: "bit", nullable: false),
                    Score = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuizAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuizAttempts_AspNetUsers_StudentId",
                        column: x => x.StudentId,
                        principalSchema: "nexus",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QuizAttempts_Quizzes_QuizId",
                        column: x => x.QuizId,
                        principalSchema: "nexus",
                        principalTable: "Quizzes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Answers",
                schema: "nexus",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QuizAttemptId = table.Column<int>(type: "int", nullable: false),
                    QuestionId = table.Column<int>(type: "int", nullable: false),
                    SelectedOption = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: true),
                    IsCorrect = table.Column<bool>(type: "bit", nullable: false),
                    PointsEarned = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Answers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Answers_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalSchema: "nexus",
                        principalTable: "Questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Answers_QuizAttempts_QuizAttemptId",
                        column: x => x.QuizAttemptId,
                        principalSchema: "nexus",
                        principalTable: "QuizAttempts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_StudentId_AssignmentId_SubmittedAt",
                schema: "nexus",
                table: "Submissions",
                columns: new[] { "StudentId", "AssignmentId", "SubmittedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Answers_QuestionId",
                schema: "nexus",
                table: "Answers",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_Answers_QuizAttemptId_QuestionId",
                schema: "nexus",
                table: "Answers",
                columns: new[] { "QuizAttemptId", "QuestionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseMaterials_CourseId_MaterialType",
                schema: "nexus",
                table: "CourseMaterials",
                columns: new[] { "CourseId", "MaterialType" });

            migrationBuilder.CreateIndex(
                name: "IX_CourseMaterials_UploadedByTeacherId",
                schema: "nexus",
                table: "CourseMaterials",
                column: "UploadedByTeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_Questions_QuizId",
                schema: "nexus",
                table: "Questions",
                column: "QuizId");

            migrationBuilder.CreateIndex(
                name: "IX_QuizAttempts_QuizId_StudentId",
                schema: "nexus",
                table: "QuizAttempts",
                columns: new[] { "QuizId", "StudentId" });

            migrationBuilder.CreateIndex(
                name: "IX_QuizAttempts_StudentId",
                schema: "nexus",
                table: "QuizAttempts",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_Quizzes_CourseId_IsPublished",
                schema: "nexus",
                table: "Quizzes",
                columns: new[] { "CourseId", "IsPublished" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Answers",
                schema: "nexus");

            migrationBuilder.DropTable(
                name: "CourseMaterials",
                schema: "nexus");

            migrationBuilder.DropTable(
                name: "Questions",
                schema: "nexus");

            migrationBuilder.DropTable(
                name: "QuizAttempts",
                schema: "nexus");

            migrationBuilder.DropTable(
                name: "Quizzes",
                schema: "nexus");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Submissions",
                schema: "nexus",
                table: "Submissions");

            migrationBuilder.DropIndex(
                name: "IX_Submissions_StudentId_AssignmentId_SubmittedAt",
                schema: "nexus",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "Id",
                schema: "nexus",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "Feedback",
                schema: "nexus",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "Status",
                schema: "nexus",
                table: "Submissions");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Submissions",
                schema: "nexus",
                table: "Submissions",
                columns: new[] { "StudentId", "AssignmentId" });
        }
    }
}
