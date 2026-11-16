using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SmartPos.EntityFrameworkCore;

#nullable disable

namespace SmartPos.Migrations
{
    [DbContext(typeof(SmartPosDbContext))]
    [Migration("20261028210000_Added_Restaurant_CarService_Fields")]
    public partial class Added_Restaurant_CarService_Fields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppRestaurantOrders]') AND name = N'CarName')
BEGIN
    ALTER TABLE [AppRestaurantOrders] ADD [CarName] nvarchar(128) NULL;
END

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppRestaurantOrders]') AND name = N'CarNumber')
BEGIN
    ALTER TABLE [AppRestaurantOrders] ADD [CarNumber] nvarchar(64) NULL;
END

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppRestaurantOrders]') AND name = N'CarLocation')
BEGIN
    ALTER TABLE [AppRestaurantOrders] ADD [CarLocation] nvarchar(128) NULL;
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppRestaurantOrders]') AND name = N'CarName')
    ALTER TABLE [AppRestaurantOrders] DROP COLUMN [CarName];

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppRestaurantOrders]') AND name = N'CarNumber')
    ALTER TABLE [AppRestaurantOrders] DROP COLUMN [CarNumber];

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppRestaurantOrders]') AND name = N'CarLocation')
    ALTER TABLE [AppRestaurantOrders] DROP COLUMN [CarLocation];
");
        }
    }
}

