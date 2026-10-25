using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SmartPos.EntityFrameworkCore;

#nullable disable

namespace SmartPos.Migrations
{
    [DbContext(typeof(SmartPosDbContext))]
    [Migration("20261025130000_Added_Restaurant_Waiter_And_Rider_Assignment")]
    public partial class Added_Restaurant_Waiter_And_Rider_Assignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppRestaurantTables]') AND name = N'WaiterId')
BEGIN
    ALTER TABLE [AppRestaurantTables] ADD [WaiterId] int NULL;
END

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppRestaurantTables]') AND name = N'WaiterName')
BEGIN
    ALTER TABLE [AppRestaurantTables] ADD [WaiterName] nvarchar(128) NULL;
END

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppCustomerOrders]') AND name = N'RiderId')
BEGIN
    ALTER TABLE [AppCustomerOrders] ADD [RiderId] int NULL;
END

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppCustomerOrders]') AND name = N'RiderName')
BEGIN
    ALTER TABLE [AppCustomerOrders] ADD [RiderName] nvarchar(128) NULL;
END

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppCustomerOrders]') AND name = N'RiderPhone')
BEGIN
    ALTER TABLE [AppCustomerOrders] ADD [RiderPhone] nvarchar(32) NULL;
END

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppCustomerOrders]') AND name = N'RiderAssignedTime')
BEGIN
    ALTER TABLE [AppCustomerOrders] ADD [RiderAssignedTime] datetime2 NULL;
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppRestaurantTables]') AND name = N'WaiterId')
    ALTER TABLE [AppRestaurantTables] DROP COLUMN [WaiterId];

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppRestaurantTables]') AND name = N'WaiterName')
    ALTER TABLE [AppRestaurantTables] DROP COLUMN [WaiterName];

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppCustomerOrders]') AND name = N'RiderId')
    ALTER TABLE [AppCustomerOrders] DROP COLUMN [RiderId];

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppCustomerOrders]') AND name = N'RiderName')
    ALTER TABLE [AppCustomerOrders] DROP COLUMN [RiderName];

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppCustomerOrders]') AND name = N'RiderPhone')
    ALTER TABLE [AppCustomerOrders] DROP COLUMN [RiderPhone];

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppCustomerOrders]') AND name = N'RiderAssignedTime')
    ALTER TABLE [AppCustomerOrders] DROP COLUMN [RiderAssignedTime];
");
        }
    }
}
