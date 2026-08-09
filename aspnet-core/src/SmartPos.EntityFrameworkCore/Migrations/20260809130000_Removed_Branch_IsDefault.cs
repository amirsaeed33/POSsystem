using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartPos.Migrations
{
    [Migration("20260809130000_Removed_Branch_IsDefault")]
    public partial class Removed_Branch_IsDefault : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH('AppBranches', 'IsDefault') IS NOT NULL
    ALTER TABLE AppBranches DROP COLUMN IsDefault;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH('AppBranches', 'IsDefault') IS NULL
    ALTER TABLE AppBranches ADD IsDefault bit NOT NULL
        CONSTRAINT DF_AppBranches_IsDefault DEFAULT(0);
");
        }
    }
}
