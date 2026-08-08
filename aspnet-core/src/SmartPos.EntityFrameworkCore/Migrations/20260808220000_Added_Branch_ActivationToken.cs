using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartPos.Migrations
{
    [Migration("20260808220000_Added_Branch_ActivationToken")]
    public partial class Added_Branch_ActivationToken : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH('AppBranches', 'ActivationTokenHash') IS NULL
    ALTER TABLE AppBranches ADD ActivationTokenHash nvarchar(128) NULL;
IF COL_LENGTH('AppBranches', 'ActivationTokenExpiresAt') IS NULL
    ALTER TABLE AppBranches ADD ActivationTokenExpiresAt datetime2 NULL;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH('AppBranches', 'ActivationTokenHash') IS NOT NULL
    ALTER TABLE AppBranches DROP COLUMN ActivationTokenHash;
IF COL_LENGTH('AppBranches', 'ActivationTokenExpiresAt') IS NOT NULL
    ALTER TABLE AppBranches DROP COLUMN ActivationTokenExpiresAt;
");
        }
    }
}
