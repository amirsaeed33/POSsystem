using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SmartPos.EntityFrameworkCore;

#nullable disable

namespace SmartPos.Migrations
{
    [DbContext(typeof(SmartPosDbContext))]
    [Migration("20261106153000_Added_Product_IsVisibleOnline")]
    public partial class Added_Product_IsVisibleOnline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppProducts]') AND name = N'IsVisibleOnline')
BEGIN
    ALTER TABLE [AppProducts] ADD [IsVisibleOnline] bit NOT NULL
        CONSTRAINT [DF_AppProducts_IsVisibleOnline] DEFAULT (1);
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppProducts]') AND name = N'IsVisibleOnline')
BEGIN
    DECLARE @df sysname;
    SELECT @df = dc.name
    FROM sys.default_constraints dc
    INNER JOIN sys.columns c ON c.default_object_id = dc.object_id
    WHERE dc.parent_object_id = OBJECT_ID(N'[AppProducts]') AND c.name = N'IsVisibleOnline';
    IF @df IS NOT NULL EXEC(N'ALTER TABLE [AppProducts] DROP CONSTRAINT [' + @df + N']');
    ALTER TABLE [AppProducts] DROP COLUMN [IsVisibleOnline];
END
");
        }
    }
}
