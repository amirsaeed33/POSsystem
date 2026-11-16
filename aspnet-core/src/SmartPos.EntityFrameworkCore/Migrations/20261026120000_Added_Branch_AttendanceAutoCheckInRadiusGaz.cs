using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SmartPos.EntityFrameworkCore;

#nullable disable

namespace SmartPos.Migrations
{
    [DbContext(typeof(SmartPosDbContext))]
    [Migration("20261026120000_Added_Branch_AttendanceAutoCheckInRadiusGaz")]
    public partial class Added_Branch_AttendanceAutoCheckInRadiusGaz : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppBranches]') AND name = N'AttendanceAutoCheckInRadiusGaz')
BEGIN
    ALTER TABLE [AppBranches] ADD [AttendanceAutoCheckInRadiusGaz] int NOT NULL CONSTRAINT [DF_AppBranches_AttendanceAutoCheckInRadiusGaz] DEFAULT 50;
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppBranches]') AND name = N'AttendanceAutoCheckInRadiusGaz')
BEGIN
    DECLARE @df sysname;
    SELECT @df = d.name
    FROM sys.default_constraints d
    INNER JOIN sys.columns c ON c.default_object_id = d.object_id
    WHERE d.parent_object_id = OBJECT_ID(N'[AppBranches]') AND c.name = N'AttendanceAutoCheckInRadiusGaz';
    IF @df IS NOT NULL EXEC(N'ALTER TABLE [AppBranches] DROP CONSTRAINT [' + @df + N']');
    ALTER TABLE [AppBranches] DROP COLUMN [AttendanceAutoCheckInRadiusGaz];
END
");
        }
    }
}
