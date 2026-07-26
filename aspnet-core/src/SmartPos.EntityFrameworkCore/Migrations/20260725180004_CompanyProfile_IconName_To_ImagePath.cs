using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartPos.Migrations
{
    /// <inheritdoc />
    public partial class CompanyProfile_IconName_To_ImagePath : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IconName",
                table: "AppCompanyProfiles");

            migrationBuilder.AddColumn<string>(
                name: "ImagePath",
                table: "AppCompanyProfiles",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImagePath",
                table: "AppCompanyProfiles");

            migrationBuilder.AddColumn<string>(
                name: "IconName",
                table: "AppCompanyProfiles",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);
        }
    }
}
