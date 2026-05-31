using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexus.Data.Migrations;

public partial class AddAcademicModels : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Departments",
            schema: "nexus",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Departments", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Courses",
            schema: "nexus",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                Semester = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                TeacherId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                DepartmentId = table.Column<int>(type: "int", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Courses", x => x.Id);
                table.ForeignKey("FK_Courses_AspNetUsers_TeacherId", x => x.TeacherId, principalSchema: "nexus", principalTable: "AspNetUsers", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_Courses_Departments_DepartmentId", x => x.DepartmentId, principalSchema: "nexus", principalTable: "Departments", principalColumn: "Id", onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateTable(
            name: "Assignments",
            schema: "nexus",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Title = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                Deadline = table.Column<DateTime>(type: "datetime2", nullable: false),
                CourseId = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Assignments", x => x.Id);
                table.ForeignKey("FK_Assignments_Courses_CourseId", x => x.CourseId, principalSchema: "nexus", principalTable: "Courses", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Enrollments",
            schema: "nexus",
            columns: table => new
            {
                StudentId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                CourseId = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Enrollments", x => new { x.StudentId, x.CourseId });
                table.ForeignKey("FK_Enrollments_AspNetUsers_StudentId", x => x.StudentId, principalSchema: "nexus", principalTable: "AspNetUsers", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_Enrollments_Courses_CourseId", x => x.CourseId, principalSchema: "nexus", principalTable: "Courses", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Grades",
            schema: "nexus",
            columns: table => new
            {
                StudentId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                CourseId = table.Column<int>(type: "int", nullable: false),
                AssignmentMarks = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                MidtermMarks = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                FinalMarks = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                TotalMarks = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Grades", x => new { x.StudentId, x.CourseId });
                table.ForeignKey("FK_Grades_AspNetUsers_StudentId", x => x.StudentId, principalSchema: "nexus", principalTable: "AspNetUsers", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_Grades_Courses_CourseId", x => x.CourseId, principalSchema: "nexus", principalTable: "Courses", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Submissions",
            schema: "nexus",
            columns: table => new
            {
                StudentId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                AssignmentId = table.Column<int>(type: "int", nullable: false),
                FilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Submissions", x => new { x.StudentId, x.AssignmentId });
                table.ForeignKey("FK_Submissions_AspNetUsers_StudentId", x => x.StudentId, principalSchema: "nexus", principalTable: "AspNetUsers", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_Submissions_Assignments_AssignmentId", x => x.AssignmentId, principalSchema: "nexus", principalTable: "Assignments", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex("IX_Assignments_CourseId_Deadline", schema: "nexus", table: "Assignments", columns: new[] { "CourseId", "Deadline" });
        migrationBuilder.CreateIndex("IX_Courses_DepartmentId", schema: "nexus", table: "Courses", column: "DepartmentId");
        migrationBuilder.CreateIndex("IX_Courses_Name_Semester", schema: "nexus", table: "Courses", columns: new[] { "Name", "Semester" }, unique: true);
        migrationBuilder.CreateIndex("IX_Courses_TeacherId", schema: "nexus", table: "Courses", column: "TeacherId");
        migrationBuilder.CreateIndex("IX_Departments_Name", schema: "nexus", table: "Departments", column: "Name", unique: true);
        migrationBuilder.CreateIndex("IX_Enrollments_CourseId", schema: "nexus", table: "Enrollments", column: "CourseId");
        migrationBuilder.CreateIndex("IX_Grades_CourseId", schema: "nexus", table: "Grades", column: "CourseId");
        migrationBuilder.CreateIndex("IX_Submissions_AssignmentId", schema: "nexus", table: "Submissions", column: "AssignmentId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "Enrollments", schema: "nexus");
        migrationBuilder.DropTable(name: "Grades", schema: "nexus");
        migrationBuilder.DropTable(name: "Submissions", schema: "nexus");
        migrationBuilder.DropTable(name: "Assignments", schema: "nexus");
        migrationBuilder.DropTable(name: "Courses", schema: "nexus");
        migrationBuilder.DropTable(name: "Departments", schema: "nexus");
    }
}
