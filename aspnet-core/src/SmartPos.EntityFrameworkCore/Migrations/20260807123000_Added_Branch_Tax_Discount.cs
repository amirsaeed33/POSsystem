using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartPos.Migrations
{
    [Migration("20260807123000_Added_Branch_Tax_Discount")]
    public partial class Added_Branch_Tax_Discount : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH('AppBranches', 'TaxPercent') IS NULL
    ALTER TABLE AppBranches ADD TaxPercent decimal(18,2) NOT NULL CONSTRAINT DF_AppBranches_TaxPercent DEFAULT(0);
IF COL_LENGTH('AppBranches', 'DiscountPercent') IS NULL
    ALTER TABLE AppBranches ADD DiscountPercent decimal(18,2) NOT NULL CONSTRAINT DF_AppBranches_DiscountPercent DEFAULT(0);
IF COL_LENGTH('AppBranches', 'DiscountAmount') IS NULL
    ALTER TABLE AppBranches ADD DiscountAmount decimal(18,2) NOT NULL CONSTRAINT DF_AppBranches_DiscountAmount DEFAULT(0);
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH('AppBranches', 'TaxPercent') IS NOT NULL
BEGIN
    DECLARE @df1 nvarchar(200);
    SELECT @df1 = d.name FROM sys.default_constraints d
    JOIN sys.columns c ON d.parent_object_id = c.object_id AND d.parent_column_id = c.column_id
    WHERE d.parent_object_id = OBJECT_ID(N'AppBranches') AND c.name = N'TaxPercent';
    IF @df1 IS NOT NULL EXEC(N'ALTER TABLE AppBranches DROP CONSTRAINT [' + @df1 + N']');
    ALTER TABLE AppBranches DROP COLUMN TaxPercent;
END
IF COL_LENGTH('AppBranches', 'DiscountPercent') IS NOT NULL
BEGIN
    DECLARE @df2 nvarchar(200);
    SELECT @df2 = d.name FROM sys.default_constraints d
    JOIN sys.columns c ON d.parent_object_id = c.object_id AND d.parent_column_id = c.column_id
    WHERE d.parent_object_id = OBJECT_ID(N'AppBranches') AND c.name = N'DiscountPercent';
    IF @df2 IS NOT NULL EXEC(N'ALTER TABLE AppBranches DROP CONSTRAINT [' + @df2 + N']');
    ALTER TABLE AppBranches DROP COLUMN DiscountPercent;
END
IF COL_LENGTH('AppBranches', 'DiscountAmount') IS NOT NULL
BEGIN
    DECLARE @df3 nvarchar(200);
    SELECT @df3 = d.name FROM sys.default_constraints d
    JOIN sys.columns c ON d.parent_object_id = c.object_id AND d.parent_column_id = c.column_id
    WHERE d.parent_object_id = OBJECT_ID(N'AppBranches') AND c.name = N'DiscountAmount';
    IF @df3 IS NOT NULL EXEC(N'ALTER TABLE AppBranches DROP CONSTRAINT [' + @df3 + N']');
    ALTER TABLE AppBranches DROP COLUMN DiscountAmount;
END
");
        }
    }
}