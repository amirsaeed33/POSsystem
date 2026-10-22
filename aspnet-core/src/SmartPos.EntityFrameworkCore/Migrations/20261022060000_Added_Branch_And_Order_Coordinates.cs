using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SmartPos.EntityFrameworkCore;

#nullable disable

namespace SmartPos.Migrations
{
    [DbContext(typeof(SmartPosDbContext))]
    [Migration("20261022060000_Added_Branch_And_Order_Coordinates")]
    public partial class Added_Branch_And_Order_Coordinates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppBranches]') AND name = N'Latitude')
BEGIN
    ALTER TABLE [AppBranches] ADD [Latitude] float NULL;
END

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppBranches]') AND name = N'Longitude')
BEGIN
    ALTER TABLE [AppBranches] ADD [Longitude] float NULL;
END

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppCustomerOrders]') AND name = N'Latitude')
BEGIN
    ALTER TABLE [AppCustomerOrders] ADD [Latitude] float NULL;
END

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppCustomerOrders]') AND name = N'Longitude')
BEGIN
    ALTER TABLE [AppCustomerOrders] ADD [Longitude] float NULL;
END

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppCustomerOrders]') AND name = N'DistanceKm')
BEGIN
    ALTER TABLE [AppCustomerOrders] ADD [DistanceKm] float NULL;
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppCustomerOrders]') AND name = N'DistanceKm')
    ALTER TABLE [AppCustomerOrders] DROP COLUMN [DistanceKm];

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppCustomerOrders]') AND name = N'Longitude')
    ALTER TABLE [AppCustomerOrders] DROP COLUMN [Longitude];

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppCustomerOrders]') AND name = N'Latitude')
    ALTER TABLE [AppCustomerOrders] DROP COLUMN [Latitude];

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppBranches]') AND name = N'Longitude')
    ALTER TABLE [AppBranches] DROP COLUMN [Longitude];

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppBranches]') AND name = N'Latitude')
    ALTER TABLE [AppBranches] DROP COLUMN [Latitude];
");
        }
    }
}
