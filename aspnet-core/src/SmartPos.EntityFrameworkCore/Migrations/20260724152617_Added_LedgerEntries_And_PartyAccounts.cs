using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartPos.Migrations
{
    /// <inheritdoc />
    public partial class Added_LedgerEntries_And_PartyAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AccountId",
                table: "AppSuppliers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AccountId",
                table: "AppCustomers",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AppLedgerEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: true),
                    AccountId = table.Column<int>(type: "int", nullable: false),
                    TransactionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    VoucherType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    VoucherId = table.Column<int>(type: "int", nullable: true),
                    Debit = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Credit = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppLedgerEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppLedgerEntries_AppAccounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "AppAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppSuppliers_AccountId",
                table: "AppSuppliers",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_AppCustomers_AccountId",
                table: "AppCustomers",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_AppLedgerEntries_AccountId",
                table: "AppLedgerEntries",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_AppLedgerEntries_TransactionDate",
                table: "AppLedgerEntries",
                column: "TransactionDate");

            migrationBuilder.AddForeignKey(
                name: "FK_AppCustomers_AppAccounts_AccountId",
                table: "AppCustomers",
                column: "AccountId",
                principalTable: "AppAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AppSuppliers_AppAccounts_AccountId",
                table: "AppSuppliers",
                column: "AccountId",
                principalTable: "AppAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AppCustomers_AppAccounts_AccountId",
                table: "AppCustomers");

            migrationBuilder.DropForeignKey(
                name: "FK_AppSuppliers_AppAccounts_AccountId",
                table: "AppSuppliers");

            migrationBuilder.DropTable(
                name: "AppLedgerEntries");

            migrationBuilder.DropIndex(
                name: "IX_AppSuppliers_AccountId",
                table: "AppSuppliers");

            migrationBuilder.DropIndex(
                name: "IX_AppCustomers_AccountId",
                table: "AppCustomers");

            migrationBuilder.DropColumn(
                name: "AccountId",
                table: "AppSuppliers");

            migrationBuilder.DropColumn(
                name: "AccountId",
                table: "AppCustomers");
        }
    }
}
