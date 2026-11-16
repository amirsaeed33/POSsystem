using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SmartPos.EntityFrameworkCore;

#nullable disable

namespace SmartPos.Migrations
{
    [DbContext(typeof(SmartPosDbContext))]
    [Migration("20261105023000_Added_RestaurantOrder_KitchenStatus")]
    public partial class Added_RestaurantOrder_KitchenStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppRestaurantOrders]') AND name = N'KitchenStatus')
BEGIN
    ALTER TABLE [AppRestaurantOrders] ADD [KitchenStatus] tinyint NOT NULL
        CONSTRAINT [DF_AppRestaurantOrders_KitchenStatus] DEFAULT (0);
END
");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppRestaurantOrders]') AND name = N'KitchenReadyTime')
BEGIN
    ALTER TABLE [AppRestaurantOrders] ADD [KitchenReadyTime] datetime2 NULL;
END
");

            // Separate batch so SQL Server sees KitchenStatus after ALTER.
            migrationBuilder.Sql(@"
UPDATE o
SET o.KitchenStatus = 1
FROM [AppRestaurantOrders] o
WHERE o.KitchenStatus = 0
  AND o.IsDeleted = 0
  AND EXISTS (
      SELECT 1
      FROM [AppRestaurantOrderLines] l
      WHERE l.OrderId = o.Id
        AND l.IsDeleted = 0
        AND l.IsSentToKitchen = 1
  );
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppRestaurantOrders]') AND name = N'KitchenReadyTime')
BEGIN
    ALTER TABLE [AppRestaurantOrders] DROP COLUMN [KitchenReadyTime];
END
");

            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppRestaurantOrders]') AND name = N'KitchenStatus')
BEGIN
    DECLARE @df sysname;
    SELECT @df = dc.name
    FROM sys.default_constraints dc
    INNER JOIN sys.columns c ON c.default_object_id = dc.object_id
    WHERE dc.parent_object_id = OBJECT_ID(N'[AppRestaurantOrders]') AND c.name = N'KitchenStatus';
    IF @df IS NOT NULL EXEC(N'ALTER TABLE [AppRestaurantOrders] DROP CONSTRAINT [' + @df + N']');
    ALTER TABLE [AppRestaurantOrders] DROP COLUMN [KitchenStatus];
END
");
        }
    }
}
