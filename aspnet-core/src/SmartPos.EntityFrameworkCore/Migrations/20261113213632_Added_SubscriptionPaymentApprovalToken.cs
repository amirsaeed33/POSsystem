using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartPos.Migrations
{
    /// <inheritdoc />
    public partial class Added_SubscriptionPaymentApprovalToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApprovalToken",
                table: "AppSubscriptionPayments",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppSubscriptionPayments_ApprovalToken",
                table: "AppSubscriptionPayments",
                column: "ApprovalToken",
                unique: true,
                filter: "[ApprovalToken] IS NOT NULL AND [IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AppSubscriptionPayments_ApprovalToken",
                table: "AppSubscriptionPayments");

            migrationBuilder.DropColumn(
                name: "ApprovalToken",
                table: "AppSubscriptionPayments");
        }
    }
}
