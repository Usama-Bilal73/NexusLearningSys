using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexus.Data.Migrations;

public partial class AddRepositoryAiAnalyticsFields : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Category",
            schema: "nexus",
            table: "CourseMaterials",
            type: "nvarchar(80)",
            maxLength: 80,
            nullable: false,
            defaultValue: "General");

        migrationBuilder.AddColumn<string>(
            name: "ExtractedText",
            schema: "nexus",
            table: "CourseMaterials",
            type: "nvarchar(max)",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "AiSummary",
            schema: "nexus",
            table: "CourseMaterials",
            type: "nvarchar(max)",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "SummarizedAtUtc",
            schema: "nexus",
            table: "CourseMaterials",
            type: "datetime2",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_CourseMaterials_CourseId_Category",
            schema: "nexus",
            table: "CourseMaterials",
            columns: new[] { "CourseId", "Category" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_CourseMaterials_CourseId_Category",
            schema: "nexus",
            table: "CourseMaterials");

        migrationBuilder.DropColumn(name: "AiSummary", schema: "nexus", table: "CourseMaterials");
        migrationBuilder.DropColumn(name: "Category", schema: "nexus", table: "CourseMaterials");
        migrationBuilder.DropColumn(name: "ExtractedText", schema: "nexus", table: "CourseMaterials");
        migrationBuilder.DropColumn(name: "SummarizedAtUtc", schema: "nexus", table: "CourseMaterials");
    }
}
