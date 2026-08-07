using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartPos.Migrations
{
    [Migration("20260807112000_Added_Staff_LoginPassword")]
    public partial class Added_Staff_LoginPassword : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH('AppStaff', 'LoginPassword') IS NULL
    ALTER TABLE AppStaff ADD LoginPassword nvarchar(100) NULL;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH('AppStaff', 'LoginPassword') IS NOT NULL
    ALTER TABLE AppStaff DROP COLUMN LoginPassword;
");
        }
    }
}
