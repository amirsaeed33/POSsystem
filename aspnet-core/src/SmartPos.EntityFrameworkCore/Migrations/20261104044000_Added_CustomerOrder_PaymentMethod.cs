using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartPos.Migrations
{
    /// <inheritdoc />
    public partial class Added_CustomerOrder_PaymentMethod : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppCustomerOrders]') AND name = N'PaymentMethod')
BEGIN
    ALTER TABLE [AppCustomerOrders] ADD [PaymentMethod] nvarchar(32) NULL;
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppCustomerOrders]') AND name = N'PaymentMethod')
BEGIN
    ALTER TABLE [AppCustomerOrders] DROP COLUMN [PaymentMethod];
END
");
        }
    }
}
