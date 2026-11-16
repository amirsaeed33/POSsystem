using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SmartPos.EntityFrameworkCore;

#nullable disable

namespace SmartPos.Migrations
{
    [DbContext(typeof(SmartPosDbContext))]
    [Migration("20261021110000_Added_Restaurant_Orders_And_Modifiers")]
    public partial class Added_Restaurant_Orders_And_Modifiers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'AppRestaurantOrders')
BEGIN
    CREATE TABLE [AppRestaurantOrders] (
        [Id] int IDENTITY(1,1) NOT NULL,
        [CreationTime] datetime2 NOT NULL,
        [CreatorUserId] bigint NULL,
        [LastModificationTime] datetime2 NULL,
        [LastModifierUserId] bigint NULL,
        [IsDeleted] bit NOT NULL,
        [DeleterUserId] bigint NULL,
        [DeletionTime] datetime2 NULL,
        [TenantId] int NULL,
        [BranchId] int NOT NULL,
        [OrderNo] nvarchar(64) NULL,
        [TableId] int NULL,
        [TableNo] nvarchar(64) NULL,
        [OrderType] tinyint NOT NULL CONSTRAINT [DF_AppRestaurantOrders_OrderType] DEFAULT 1,
        [Status] tinyint NOT NULL CONSTRAINT [DF_AppRestaurantOrders_Status] DEFAULT 0,
        [CustomerId] int NULL,
        [WaiterName] nvarchar(128) NULL,
        [Notes] nvarchar(1024) NULL,
        [SubTotal] decimal(18,2) NOT NULL CONSTRAINT [DF_AppRestaurantOrders_SubTotal] DEFAULT 0,
        [DiscountPercent] decimal(18,2) NOT NULL CONSTRAINT [DF_AppRestaurantOrders_DiscountPercent] DEFAULT 0,
        [DiscountAmount] decimal(18,2) NOT NULL CONSTRAINT [DF_AppRestaurantOrders_DiscountAmount] DEFAULT 0,
        [TaxPercent] decimal(18,2) NOT NULL CONSTRAINT [DF_AppRestaurantOrders_TaxPercent] DEFAULT 0,
        [TaxAmount] decimal(18,2) NOT NULL CONSTRAINT [DF_AppRestaurantOrders_TaxAmount] DEFAULT 0,
        [ServiceChargePercent] decimal(18,2) NOT NULL CONSTRAINT [DF_AppRestaurantOrders_ServiceChargePercent] DEFAULT 0,
        [ServiceChargeAmount] decimal(18,2) NOT NULL CONSTRAINT [DF_AppRestaurantOrders_ServiceChargeAmount] DEFAULT 0,
        [GrandTotal] decimal(18,2) NOT NULL CONSTRAINT [DF_AppRestaurantOrders_GrandTotal] DEFAULT 0,
        [SaleId] int NULL,
        [BilledTime] datetime2 NULL,
        [SettledTime] datetime2 NULL,
        CONSTRAINT [PK_AppRestaurantOrders] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AppRestaurantOrders_AppBranches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [AppBranches] ([Id]),
        CONSTRAINT [FK_AppRestaurantOrders_AppRestaurantTables_TableId] FOREIGN KEY ([TableId]) REFERENCES [AppRestaurantTables] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_AppRestaurantOrders_AppCustomers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [AppCustomers] ([Id]),
        CONSTRAINT [FK_AppRestaurantOrders_AppSales_SaleId] FOREIGN KEY ([SaleId]) REFERENCES [AppSales] ([Id])
    );
    CREATE INDEX [IX_AppRestaurantOrders_BranchId_Status] ON [AppRestaurantOrders] ([BranchId], [Status]);
    CREATE INDEX [IX_AppRestaurantOrders_TableId] ON [AppRestaurantOrders] ([TableId]);
END

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'AppRestaurantOrderLines')
BEGIN
    CREATE TABLE [AppRestaurantOrderLines] (
        [Id] int IDENTITY(1,1) NOT NULL,
        [CreationTime] datetime2 NOT NULL,
        [CreatorUserId] bigint NULL,
        [LastModificationTime] datetime2 NULL,
        [LastModifierUserId] bigint NULL,
        [IsDeleted] bit NOT NULL,
        [DeleterUserId] bigint NULL,
        [DeletionTime] datetime2 NULL,
        [TenantId] int NULL,
        [OrderId] int NOT NULL,
        [ProductId] int NOT NULL,
        [ProductName] nvarchar(256) NULL,
        [Quantity] decimal(18,2) NOT NULL CONSTRAINT [DF_AppRestaurantOrderLines_Quantity] DEFAULT 1,
        [UnitPrice] decimal(18,2) NOT NULL CONSTRAINT [DF_AppRestaurantOrderLines_UnitPrice] DEFAULT 0,
        [LineTotal] decimal(18,2) NOT NULL CONSTRAINT [DF_AppRestaurantOrderLines_LineTotal] DEFAULT 0,
        [KitchenNotes] nvarchar(512) NULL,
        [SelectedModifiers] nvarchar(512) NULL,
        [IsSentToKitchen] bit NOT NULL CONSTRAINT [DF_AppRestaurantOrderLines_IsSentToKitchen] DEFAULT 0,
        [SentToKitchenTime] datetime2 NULL,
        CONSTRAINT [PK_AppRestaurantOrderLines] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AppRestaurantOrderLines_AppRestaurantOrders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [AppRestaurantOrders] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AppRestaurantOrderLines_AppProducts_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [AppProducts] ([Id])
    );
    CREATE INDEX [IX_AppRestaurantOrderLines_OrderId] ON [AppRestaurantOrderLines] ([OrderId]);
END

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'AppProductModifierGroups')
BEGIN
    CREATE TABLE [AppProductModifierGroups] (
        [Id] int IDENTITY(1,1) NOT NULL,
        [CreationTime] datetime2 NOT NULL,
        [CreatorUserId] bigint NULL,
        [LastModificationTime] datetime2 NULL,
        [LastModifierUserId] bigint NULL,
        [IsDeleted] bit NOT NULL,
        [DeleterUserId] bigint NULL,
        [DeletionTime] datetime2 NULL,
        [TenantId] int NULL,
        [ProductId] int NOT NULL,
        [Name] nvarchar(128) NOT NULL,
        [MinSelection] int NOT NULL CONSTRAINT [DF_AppProductModifierGroups_MinSelection] DEFAULT 0,
        [MaxSelection] int NOT NULL CONSTRAINT [DF_AppProductModifierGroups_MaxSelection] DEFAULT 1,
        [IsRequired] bit NOT NULL CONSTRAINT [DF_AppProductModifierGroups_IsRequired] DEFAULT 0,
        CONSTRAINT [PK_AppProductModifierGroups] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AppProductModifierGroups_AppProducts_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [AppProducts] ([Id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_AppProductModifierGroups_ProductId] ON [AppProductModifierGroups] ([ProductId]);
END

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'AppProductModifierOptions')
BEGIN
    CREATE TABLE [AppProductModifierOptions] (
        [Id] int IDENTITY(1,1) NOT NULL,
        [CreationTime] datetime2 NOT NULL,
        [CreatorUserId] bigint NULL,
        [LastModificationTime] datetime2 NULL,
        [LastModifierUserId] bigint NULL,
        [IsDeleted] bit NOT NULL,
        [DeleterUserId] bigint NULL,
        [DeletionTime] datetime2 NULL,
        [TenantId] int NULL,
        [ModifierGroupId] int NOT NULL,
        [Name] nvarchar(128) NOT NULL,
        [Price] decimal(18,2) NOT NULL CONSTRAINT [DF_AppProductModifierOptions_Price] DEFAULT 0,
        [IsDefault] bit NOT NULL CONSTRAINT [DF_AppProductModifierOptions_IsDefault] DEFAULT 0,
        CONSTRAINT [PK_AppProductModifierOptions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AppProductModifierOptions_AppProductModifierGroups_ModifierGroupId] FOREIGN KEY ([ModifierGroupId]) REFERENCES [AppProductModifierGroups] ([Id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_AppProductModifierOptions_ModifierGroupId] ON [AppProductModifierOptions] ([ModifierGroupId]);
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = N'AppProductModifierOptions') DROP TABLE [AppProductModifierOptions];
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = N'AppProductModifierGroups') DROP TABLE [AppProductModifierGroups];
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = N'AppRestaurantOrderLines') DROP TABLE [AppRestaurantOrderLines];
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = N'AppRestaurantOrders') DROP TABLE [AppRestaurantOrders];
");
        }
    }
}
