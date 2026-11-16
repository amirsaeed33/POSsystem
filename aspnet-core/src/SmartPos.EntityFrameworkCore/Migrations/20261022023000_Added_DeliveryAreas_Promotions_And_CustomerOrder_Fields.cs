using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SmartPos.EntityFrameworkCore;

#nullable disable

namespace SmartPos.Migrations
{
    [DbContext(typeof(SmartPosDbContext))]
    [Migration("20261022023000_Added_DeliveryAreas_Promotions_And_CustomerOrder_Fields")]
    public partial class Added_DeliveryAreas_Promotions_And_CustomerOrder_Fields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'AppDeliveryAreas')
BEGIN
    CREATE TABLE [AppDeliveryAreas] (
        [Id] int IDENTITY(1,1) NOT NULL,
        [TenantId] int NULL,
        [BranchId] int NOT NULL,
        [Name] nvarchar(128) NOT NULL,
        [DeliveryFee] decimal(18,2) NOT NULL CONSTRAINT [DF_AppDeliveryAreas_DeliveryFee] DEFAULT 0,
        [MinimumOrderAmount] decimal(18,2) NOT NULL CONSTRAINT [DF_AppDeliveryAreas_MinimumOrderAmount] DEFAULT 0,
        [EstimatedDeliveryTime] nvarchar(64) NULL,
        [CoverageDetails] nvarchar(256) NULL,
        [IsActive] bit NOT NULL CONSTRAINT [DF_AppDeliveryAreas_IsActive] DEFAULT 1,
        [CreationTime] datetime2 NOT NULL CONSTRAINT [DF_AppDeliveryAreas_CreationTime] DEFAULT GETDATE(),
        [CreatorUserId] bigint NULL,
        [LastModificationTime] datetime2 NULL,
        [LastModifierUserId] bigint NULL,
        [IsDeleted] bit NOT NULL CONSTRAINT [DF_AppDeliveryAreas_IsDeleted] DEFAULT 0,
        [DeleterUserId] bigint NULL,
        [DeletionTime] datetime2 NULL,
        CONSTRAINT [PK_AppDeliveryAreas] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AppDeliveryAreas_AppBranches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [AppBranches] ([Id])
    );
    CREATE INDEX [IX_AppDeliveryAreas_BranchId] ON [AppDeliveryAreas] ([BranchId]);
    CREATE INDEX [IX_AppDeliveryAreas_IsActive] ON [AppDeliveryAreas] ([IsActive]);
END;

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'AppDeliveryPromotions')
BEGIN
    CREATE TABLE [AppDeliveryPromotions] (
        [Id] int IDENTITY(1,1) NOT NULL,
        [TenantId] int NULL,
        [BranchId] int NULL,
        [Name] nvarchar(128) NOT NULL,
        [MinimumOrderAmount] decimal(18,2) NULL,
        [ProductId] int NULL,
        [StartDate] datetime2 NULL,
        [EndDate] datetime2 NULL,
        [IsActive] bit NOT NULL CONSTRAINT [DF_AppDeliveryPromotions_IsActive] DEFAULT 1,
        [Notes] nvarchar(256) NULL,
        [CreationTime] datetime2 NOT NULL CONSTRAINT [DF_AppDeliveryPromotions_CreationTime] DEFAULT GETDATE(),
        [CreatorUserId] bigint NULL,
        [LastModificationTime] datetime2 NULL,
        [LastModifierUserId] bigint NULL,
        [IsDeleted] bit NOT NULL CONSTRAINT [DF_AppDeliveryPromotions_IsDeleted] DEFAULT 0,
        [DeleterUserId] bigint NULL,
        [DeletionTime] datetime2 NULL,
        CONSTRAINT [PK_AppDeliveryPromotions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AppDeliveryPromotions_AppBranches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [AppBranches] ([Id]),
        CONSTRAINT [FK_AppDeliveryPromotions_AppProducts_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [AppProducts] ([Id])
    );
    CREATE INDEX [IX_AppDeliveryPromotions_BranchId] ON [AppDeliveryPromotions] ([BranchId]);
    CREATE INDEX [IX_AppDeliveryPromotions_ProductId] ON [AppDeliveryPromotions] ([ProductId]);
    CREATE INDEX [IX_AppDeliveryPromotions_IsActive] ON [AppDeliveryPromotions] ([IsActive]);
END;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppProducts]') AND name = N'IsFreeDelivery')
    ALTER TABLE [AppProducts] ADD [IsFreeDelivery] bit NOT NULL CONSTRAINT [DF_AppProducts_IsFreeDelivery] DEFAULT 0;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppCustomerOrders]') AND name = N'SubTotal')
    ALTER TABLE [AppCustomerOrders] ADD [SubTotal] decimal(18,2) NOT NULL CONSTRAINT [DF_AppCustomerOrders_SubTotal] DEFAULT 0;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppCustomerOrders]') AND name = N'DeliveryFee')
    ALTER TABLE [AppCustomerOrders] ADD [DeliveryFee] decimal(18,2) NOT NULL CONSTRAINT [DF_AppCustomerOrders_DeliveryFee] DEFAULT 0;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppCustomerOrders]') AND name = N'DeliveryAreaId')
    ALTER TABLE [AppCustomerOrders] ADD [DeliveryAreaId] int NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppCustomerOrders]') AND name = N'DeliveryAreaName')
    ALTER TABLE [AppCustomerOrders] ADD [DeliveryAreaName] nvarchar(128) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppCustomerOrders]') AND name = N'IsFreeDelivery')
    ALTER TABLE [AppCustomerOrders] ADD [IsFreeDelivery] bit NOT NULL CONSTRAINT [DF_AppCustomerOrders_IsFreeDelivery] DEFAULT 0;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppCustomerOrders]') AND name = N'FreeDeliveryReason')
    ALTER TABLE [AppCustomerOrders] ADD [FreeDeliveryReason] nvarchar(256) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppCustomerOrders]') AND name = N'DeliveryPromotionId')
    ALTER TABLE [AppCustomerOrders] ADD [DeliveryPromotionId] int NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppCustomerOrders]') AND name = N'OriginalDeliveryFee')
    ALTER TABLE [AppCustomerOrders] ADD [OriginalDeliveryFee] decimal(18,2) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppCustomerOrders]') AND name = N'DeliveryFeeOverrideReason')
    ALTER TABLE [AppCustomerOrders] ADD [DeliveryFeeOverrideReason] nvarchar(256) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppCustomerOrders]') AND name = N'DeliveryFeeOverriddenByUserId')
    ALTER TABLE [AppCustomerOrders] ADD [DeliveryFeeOverriddenByUserId] bigint NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppCustomerOrders]') AND name = N'DeliveryFeeOverrideTime')
    ALTER TABLE [AppCustomerOrders] ADD [DeliveryFeeOverrideTime] datetime2 NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppSales]') AND name = N'DeliveryCharges')
    ALTER TABLE [AppSales] ADD [DeliveryCharges] decimal(18,2) NOT NULL CONSTRAINT [DF_AppSales_DeliveryCharges] DEFAULT 0;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppSales]') AND name = N'DeliveryAreaName')
    ALTER TABLE [AppSales] ADD [DeliveryAreaName] nvarchar(128) NULL;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
