using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartPos.Migrations
{
    /// <inheritdoc />
    public partial class Added_OnlineOrderHeroSlides : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppOnlineOrderHeroSlides",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: true),
                    BranchId = table.Column<int>(type: "int", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Badge = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    BadgeIcon = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    TitleMain = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    TitleHighlight = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    TitleEnd = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    OfferPill = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Rating = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    PrimaryBtnText = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    PrimaryTarget = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    SecondaryBtnText = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    SecondaryTarget = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    ImagePath = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    BgImagePath = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorUserId = table.Column<long>(type: "bigint", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierUserId = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeleterUserId = table.Column<long>(type: "bigint", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppOnlineOrderHeroSlides", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppOnlineOrderHeroSlides_AppBranches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "AppBranches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppOnlineOrderHeroSlides_BranchId_IsActive",
                table: "AppOnlineOrderHeroSlides",
                columns: new[] { "BranchId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_AppOnlineOrderHeroSlides_BranchId_SortOrder",
                table: "AppOnlineOrderHeroSlides",
                columns: new[] { "BranchId", "SortOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppOnlineOrderHeroSlides");
        }
    }
}
