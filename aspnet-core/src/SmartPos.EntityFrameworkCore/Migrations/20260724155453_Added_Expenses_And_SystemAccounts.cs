using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartPos.Migrations
{
    /// <inheritdoc />
    public partial class Added_Expenses_And_SystemAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppExpenses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: true),
                    ExpenseDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ReferenceNo = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    PaymentAccountId = table.Column<int>(type: "int", nullable: false),
                    ExpenseAccountId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_AppExpenses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppExpenses_AppAccounts_ExpenseAccountId",
                        column: x => x.ExpenseAccountId,
                        principalTable: "AppAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AppExpenses_AppAccounts_PaymentAccountId",
                        column: x => x.PaymentAccountId,
                        principalTable: "AppAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppExpenses_ExpenseAccountId",
                table: "AppExpenses",
                column: "ExpenseAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_AppExpenses_PaymentAccountId",
                table: "AppExpenses",
                column: "PaymentAccountId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppExpenses");
        }
    }
}
