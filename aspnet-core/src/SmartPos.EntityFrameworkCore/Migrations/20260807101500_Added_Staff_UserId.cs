using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartPos.Migrations
{
    /// <inheritdoc />
    [Migration("20260807101500_Added_Staff_UserId")]
    public partial class Added_Staff_UserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH('AppStaff', 'UserId') IS NULL
    ALTER TABLE AppStaff ADD UserId bigint NULL;
");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AppStaff_UserId' AND object_id = OBJECT_ID(N'AppStaff'))
    CREATE INDEX IX_AppStaff_UserId ON AppStaff (UserId);
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AppStaff_UserId' AND object_id = OBJECT_ID(N'AppStaff'))
    DROP INDEX IX_AppStaff_UserId ON AppStaff;

IF COL_LENGTH('AppStaff', 'UserId') IS NOT NULL
    ALTER TABLE AppStaff DROP COLUMN UserId;
");
        }
    }
}
