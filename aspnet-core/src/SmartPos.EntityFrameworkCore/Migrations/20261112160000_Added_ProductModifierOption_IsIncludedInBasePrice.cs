using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SmartPos.EntityFrameworkCore;

#nullable disable

namespace SmartPos.Migrations
{
    [DbContext(typeof(SmartPosDbContext))]
    [Migration("20261112160000_Added_ProductModifierOption_IsIncludedInBasePrice")]
    public partial class Added_ProductModifierOption_IsIncludedInBasePrice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Separate batches: SQL Server compiles the whole batch before ALTER runs,
            // so UPDATE must not reference the new column in the same batch as ADD.
            migrationBuilder.Sql(@"
IF COL_LENGTH(N'dbo.AppProductModifierOptions', N'IsIncludedInBasePrice') IS NULL
BEGIN
    ALTER TABLE [dbo].[AppProductModifierOptions]
    ADD [IsIncludedInBasePrice] bit NOT NULL
        CONSTRAINT [DF_AppProductModifierOptions_IsIncludedInBasePrice] DEFAULT (0);
END
");

            migrationBuilder.Sql(@"
-- Backfill: previous empty→sale flow stored sale price without a flag.
IF COL_LENGTH(N'dbo.AppProductModifierOptions', N'IsIncludedInBasePrice') IS NOT NULL
BEGIN
    UPDATE o
    SET o.[IsIncludedInBasePrice] = 1
    FROM [dbo].[AppProductModifierOptions] o
    INNER JOIN [dbo].[AppProductModifierGroups] g ON g.[Id] = o.[ModifierGroupId]
    INNER JOIN [dbo].[AppProducts] p ON p.[Id] = g.[ProductId]
    WHERE o.[IsDeleted] = 0
      AND o.[IsIncludedInBasePrice] = 0
      AND o.[Price] > 0
      AND p.[Price] > 0
      AND ABS(o.[Price] - p.[Price]) < 0.01;
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH(N'dbo.AppProductModifierOptions', N'IsIncludedInBasePrice') IS NOT NULL
BEGIN
    DECLARE @df sysname;
    SELECT @df = dc.name
    FROM sys.default_constraints dc
    INNER JOIN sys.columns c ON c.default_object_id = dc.object_id
    INNER JOIN sys.tables t ON t.object_id = c.object_id
    WHERE t.name = N'AppProductModifierOptions'
      AND SCHEMA_NAME(t.schema_id) = N'dbo'
      AND c.name = N'IsIncludedInBasePrice';
    IF @df IS NOT NULL
        EXEC(N'ALTER TABLE [dbo].[AppProductModifierOptions] DROP CONSTRAINT [' + @df + N']');
    ALTER TABLE [dbo].[AppProductModifierOptions] DROP COLUMN [IsIncludedInBasePrice];
END
");
        }
    }
}
