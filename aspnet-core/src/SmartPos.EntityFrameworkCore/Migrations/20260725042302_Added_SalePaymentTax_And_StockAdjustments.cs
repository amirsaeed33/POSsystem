using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartPos.Migrations
{
    /// <inheritdoc />
    public partial class Added_SalePaymentTax_And_StockAdjustments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CardAmount",
                table: "AppSales",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "CashAmount",
                table: "AppSales",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "CreditAmount",
                table: "AppSales",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountAmount",
                table: "AppSales",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountPercent",
                table: "AppSales",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "PaymentType",
                table: "AppSales",
                type: "int",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.AddColumn<decimal>(
                name: "SubTotal",
                table: "AppSales",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxAmount",
                table: "AppSales",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxPercent",
                table: "AppSales",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "AppStockAdjustments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: true),
                    AdjustmentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Reason = table.Column<int>(type: "int", nullable: false),
                    ReferenceNo = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
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
                    table.PrimaryKey("PK_AppStockAdjustments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppStockAdjustmentLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: true),
                    StockAdjustmentId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    QuantityChange = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
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
                    table.PrimaryKey("PK_AppStockAdjustmentLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppStockAdjustmentLines_AppProducts_ProductId",
                        column: x => x.ProductId,
                        principalTable: "AppProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AppStockAdjustmentLines_AppStockAdjustments_StockAdjustmentId",
                        column: x => x.StockAdjustmentId,
                        principalTable: "AppStockAdjustments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppStockAdjustmentLines_ProductId",
                table: "AppStockAdjustmentLines",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_AppStockAdjustmentLines_StockAdjustmentId",
                table: "AppStockAdjustmentLines",
                column: "StockAdjustmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AppStockAdjustments_ReferenceNo",
                table: "AppStockAdjustments",
                column: "ReferenceNo");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppStockAdjustmentLines");

            migrationBuilder.DropTable(
                name: "AppStockAdjustments");

            migrationBuilder.DropColumn(
                name: "CardAmount",
                table: "AppSales");

            migrationBuilder.DropColumn(
                name: "CashAmount",
                table: "AppSales");

            migrationBuilder.DropColumn(
                name: "CreditAmount",
                table: "AppSales");

            migrationBuilder.DropColumn(
                name: "DiscountAmount",
                table: "AppSales");

            migrationBuilder.DropColumn(
                name: "DiscountPercent",
                table: "AppSales");

            migrationBuilder.DropColumn(
                name: "PaymentType",
                table: "AppSales");

            migrationBuilder.DropColumn(
                name: "SubTotal",
                table: "AppSales");

            migrationBuilder.DropColumn(
                name: "TaxAmount",
                table: "AppSales");

            migrationBuilder.DropColumn(
                name: "TaxPercent",
                table: "AppSales");
        }
    }
}
