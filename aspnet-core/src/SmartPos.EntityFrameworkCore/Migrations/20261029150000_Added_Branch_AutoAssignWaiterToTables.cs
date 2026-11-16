using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SmartPos.EntityFrameworkCore;

#nullable disable

namespace SmartPos.Migrations
{
    [DbContext(typeof(SmartPosDbContext))]
    [Migration("20261029150000_Added_Branch_AutoAssignWaiterToTables")]
    public partial class Added_Branch_AutoAssignWaiterToTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppBranches]') AND name = N'AutoAssignWaiterToTables')
BEGIN
    ALTER TABLE [AppBranches] ADD [AutoAssignWaiterToTables] bit NOT NULL CONSTRAINT [DF_AppBranches_AutoAssignWaiterToTables] DEFAULT 1;
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppBranches]') AND name = N'AutoAssignWaiterToTables')
BEGIN
    DECLARE @df sysname;
    SELECT @df = dc.name
    FROM sys.default_constraints dc
    INNER JOIN sys.columns c ON c.default_object_id = dc.object_id
    WHERE dc.parent_object_id = OBJECT_ID(N'[AppBranches]') AND c.name = N'AutoAssignWaiterToTables';
    IF @df IS NOT NULL EXEC(N'ALTER TABLE [AppBranches] DROP CONSTRAINT [' + @df + N']');
    ALTER TABLE [AppBranches] DROP COLUMN [AutoAssignWaiterToTables];
END
");
        }
    }
}

