using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexus.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAbuBakarModules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaxAttempts",
                schema: "nexus",
                table: "Quizzes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "ShuffleQuestions",
                schema: "nexus",
                table: "Quizzes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "QuizMarks",
                schema: "nexus",
                table: "Grades",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "SemesterId",
                schema: "nexus",
                table: "Courses",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Attendances",
                schema: "nexus",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CourseId = table.Column<int>(type: "int", nullable: false),
                    StudentId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MarkedByTeacherId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    MarkedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attendances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Attendances_AspNetUsers_MarkedByTeacherId",
                        column: x => x.MarkedByTeacherId,
                        principalSchema: "nexus",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Attendances_AspNetUsers_StudentId",
                        column: x => x.StudentId,
                        principalSchema: "nexus",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Attendances_Courses_CourseId",
                        column: x => x.CourseId,
                        principalSchema: "nexus",
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GradeWeights",
                schema: "nexus",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CourseId = table.Column<int>(type: "int", nullable: false),
                    AssignmentWeight = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    MidtermWeight = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    FinalWeight = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    QuizWeight = table.Column<decimal>(type: "decimal(5,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GradeWeights", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GradeWeights_Courses_CourseId",
                        column: x => x.CourseId,
                        principalSchema: "nexus",
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Semesters",
                schema: "nexus",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Semesters", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Courses_SemesterId",
                schema: "nexus",
                table: "Courses",
                column: "SemesterId");

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_CourseId",
                schema: "nexus",
                table: "Attendances",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_MarkedByTeacherId",
                schema: "nexus",
                table: "Attendances",
                column: "MarkedByTeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_StudentId",
                schema: "nexus",
                table: "Attendances",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_GradeWeights_CourseId",
                schema: "nexus",
                table: "GradeWeights",
                column: "CourseId");

            migrationBuilder.AddForeignKey(
                name: "FK_Courses_Semesters_SemesterId",
                schema: "nexus",
                table: "Courses",
                column: "SemesterId",
                principalSchema: "nexus",
                principalTable: "Semesters",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Courses_Semesters_SemesterId",
                schema: "nexus",
                table: "Courses");

            migrationBuilder.DropTable(
                name: "Attendances",
                schema: "nexus");

            migrationBuilder.DropTable(
                name: "GradeWeights",
                schema: "nexus");

            migrationBuilder.DropTable(
                name: "Semesters",
                schema: "nexus");

            migrationBuilder.DropIndex(
                name: "IX_Courses_SemesterId",
                schema: "nexus",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "MaxAttempts",
                schema: "nexus",
                table: "Quizzes");

            migrationBuilder.DropColumn(
                name: "ShuffleQuestions",
                schema: "nexus",
                table: "Quizzes");

            migrationBuilder.DropColumn(
                name: "QuizMarks",
                schema: "nexus",
                table: "Grades");

            migrationBuilder.DropColumn(
                name: "SemesterId",
                schema: "nexus",
                table: "Courses");
        }
    }
}
