using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SmartPos.EntityFrameworkCore;

#nullable disable

namespace SmartPos.Migrations
{
    [DbContext(typeof(SmartPosDbContext))]
    [Migration("20261103130000_Added_CustomerOrder_DeliveryRating")]
    public partial class Added_CustomerOrder_DeliveryRating : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppCustomerOrders]') AND name = N'DeliveryRating')
BEGIN
    ALTER TABLE [AppCustomerOrders] ADD [DeliveryRating] int NULL;
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppCustomerOrders]') AND name = N'DeliveryRating')
BEGIN
    ALTER TABLE [AppCustomerOrders] DROP COLUMN [DeliveryRating];
END
");
        }
    }
}
