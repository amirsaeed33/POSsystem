using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SmartPos.EntityFrameworkCore;

#nullable disable

namespace SmartPos.Migrations
{
    [DbContext(typeof(SmartPosDbContext))]
    [Migration("20261028130000_Added_Restaurant_Receipt_Print_Tracking")]
    public partial class Added_Restaurant_Receipt_Print_Tracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppRestaurantOrders]') AND name = N'LastPrintedBy')
BEGIN
    ALTER TABLE [AppRestaurantOrders] ADD [LastPrintedBy] nvarchar(128) NULL;
END

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppRestaurantOrders]') AND name = N'LastPrintedTime')
BEGIN
    ALTER TABLE [AppRestaurantOrders] ADD [LastPrintedTime] datetime2 NULL;
END

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'AppRestaurantPrintLogs')
BEGIN
    CREATE TABLE [AppRestaurantPrintLogs] (
        [Id] int NOT NULL IDENTITY,
        [TenantId] int NULL,
        [BranchId] int NOT NULL,
        [OrderId] int NULL,
        [OrderNo] nvarchar(64) NULL,
        [ReceiptType] nvarchar(64) NULL,
        [PrintedByUserId] bigint NOT NULL,
        [PrintedByName] nvarchar(128) NULL,
        [PrintedByRole] nvarchar(64) NULL,
        [PrintTime] datetime2 NOT NULL,
        [CreationTime] datetime2 NOT NULL,
        [CreatorUserId] bigint NULL,
        CONSTRAINT [PK_AppRestaurantPrintLogs] PRIMARY KEY ([Id])
    );
    CREATE INDEX [IX_AppRestaurantPrintLogs_BranchId_OrderId] ON [AppRestaurantPrintLogs] ([BranchId], [OrderId]);
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = N'AppRestaurantPrintLogs')
    DROP TABLE [AppRestaurantPrintLogs];

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppRestaurantOrders]') AND name = N'LastPrintedBy')
    ALTER TABLE [AppRestaurantOrders] DROP COLUMN [LastPrintedBy];

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppRestaurantOrders]') AND name = N'LastPrintedTime')
    ALTER TABLE [AppRestaurantOrders] DROP COLUMN [LastPrintedTime];
");
        }
    }
}
