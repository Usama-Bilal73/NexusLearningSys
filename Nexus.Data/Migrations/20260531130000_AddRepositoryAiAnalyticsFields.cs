using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Nexus.Data.Persistence;

#nullable disable

namespace Nexus.Data.Migrations;

[Migration("20260531130000_AddRepositoryAiAnalyticsFields")]
public partial class AddRepositoryAiAnalyticsFields : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            @"IF COL_LENGTH(N'[nexus].[CourseMaterials]', N'Category') IS NULL
BEGIN
    ALTER TABLE [nexus].[CourseMaterials]
    ADD [Category] nvarchar(80) NOT NULL
        CONSTRAINT [DF_CourseMaterials_Category] DEFAULT N'General';
END

IF COL_LENGTH(N'[nexus].[CourseMaterials]', N'ExtractedText') IS NULL
BEGIN
    ALTER TABLE [nexus].[CourseMaterials] ADD [ExtractedText] nvarchar(max) NULL;
END

IF COL_LENGTH(N'[nexus].[CourseMaterials]', N'AiSummary') IS NULL
BEGIN
    ALTER TABLE [nexus].[CourseMaterials] ADD [AiSummary] nvarchar(max) NULL;
END

IF COL_LENGTH(N'[nexus].[CourseMaterials]', N'SummarizedAtUtc') IS NULL
BEGIN
    ALTER TABLE [nexus].[CourseMaterials] ADD [SummarizedAtUtc] datetime2 NULL;
END

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_CourseMaterials_CourseId_Category'
      AND [object_id] = OBJECT_ID(N'[nexus].[CourseMaterials]'))
BEGIN
    CREATE INDEX [IX_CourseMaterials_CourseId_Category]
    ON [nexus].[CourseMaterials] ([CourseId], [Category]);
END");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            @"IF EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_CourseMaterials_CourseId_Category'
      AND [object_id] = OBJECT_ID(N'[nexus].[CourseMaterials]'))
BEGIN
    DROP INDEX [IX_CourseMaterials_CourseId_Category] ON [nexus].[CourseMaterials];
END

IF COL_LENGTH(N'[nexus].[CourseMaterials]', N'SummarizedAtUtc') IS NOT NULL
BEGIN
    ALTER TABLE [nexus].[CourseMaterials] DROP COLUMN [SummarizedAtUtc];
END

IF COL_LENGTH(N'[nexus].[CourseMaterials]', N'ExtractedText') IS NOT NULL
BEGIN
    ALTER TABLE [nexus].[CourseMaterials] DROP COLUMN [ExtractedText];
END

IF COL_LENGTH(N'[nexus].[CourseMaterials]', N'AiSummary') IS NOT NULL
BEGIN
    ALTER TABLE [nexus].[CourseMaterials] DROP COLUMN [AiSummary];
END

IF COL_LENGTH(N'[nexus].[CourseMaterials]', N'Category') IS NOT NULL
BEGIN
    DECLARE @constraintName sysname;

    SELECT @constraintName = [dc].[name]
    FROM sys.default_constraints AS [dc]
    INNER JOIN sys.columns AS [c]
        ON [c].[default_object_id] = [dc].[object_id]
    WHERE [dc].[parent_object_id] = OBJECT_ID(N'[nexus].[CourseMaterials]')
      AND [c].[name] = N'Category';

    IF @constraintName IS NOT NULL
    BEGIN
        EXEC(N'ALTER TABLE [nexus].[CourseMaterials] DROP CONSTRAINT [' + @constraintName + N']');
    END

    ALTER TABLE [nexus].[CourseMaterials] DROP COLUMN [Category];
END");
    }
}
