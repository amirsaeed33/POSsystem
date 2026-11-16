using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SmartPos.EntityFrameworkCore;

#nullable disable

namespace SmartPos.Migrations
{
    [DbContext(typeof(SmartPosDbContext))]
    [Migration("20261025180000_Added_Deals_And_DealDetails")]
    public partial class Added_Deals_And_DealDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'AppDeals')
BEGIN
    CREATE TABLE [AppDeals] (
        [Id] int NOT NULL IDENTITY(1,1),
        [TenantId] int NULL,
        [BranchId] int NOT NULL,
        [Name] nvarchar(256) NOT NULL,
        [Description] nvarchar(1024) NULL,
        [DealPrice] decimal(18,2) NOT NULL,
        [ExpiryDate] datetime2 NULL,
        [CategoryType] nvarchar(64) NULL,
        [CategoryId] int NULL,
        [IsActive] bit NOT NULL DEFAULT 1,
        [ProductId] int NULL,
        [CreationTime] datetime2 NOT NULL,
        [CreatorUserId] bigint NULL,
        [LastModificationTime] datetime2 NULL,
        [LastModifierUserId] bigint NULL,
        [IsDeleted] bit NOT NULL DEFAULT 0,
        [DeleterUserId] bigint NULL,
        [DeletionTime] datetime2 NULL,
        CONSTRAINT [PK_AppDeals] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_AppDeals_AppBranches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [AppBranches] ([Id]),
        CONSTRAINT [FK_AppDeals_AppCategories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [AppCategories] ([Id])
    );
    CREATE INDEX [IX_AppDeals_BranchId] ON [AppDeals] ([BranchId]);
    CREATE INDEX [IX_AppDeals_TenantId_BranchId] ON [AppDeals] ([TenantId], [BranchId]);
END

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'AppDealDetails')
BEGIN
    CREATE TABLE [AppDealDetails] (
        [Id] int NOT NULL IDENTITY(1,1),
        [TenantId] int NULL,
        [DealId] int NOT NULL,
        [ProductId] int NOT NULL,
        [Quantity] decimal(18,2) NOT NULL DEFAULT 1,
        [UnitPrice] decimal(18,2) NOT NULL DEFAULT 0,
        [CreationTime] datetime2 NOT NULL,
        [CreatorUserId] bigint NULL,
        [LastModificationTime] datetime2 NULL,
        [LastModifierUserId] bigint NULL,
        [IsDeleted] bit NOT NULL DEFAULT 0,
        [DeleterUserId] bigint NULL,
        [DeletionTime] datetime2 NULL,
        CONSTRAINT [PK_AppDealDetails] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_AppDealDetails_AppDeals_DealId] FOREIGN KEY ([DealId]) REFERENCES [AppDeals] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AppDealDetails_AppProducts_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [AppProducts] ([Id])
    );
    CREATE INDEX [IX_AppDealDetails_DealId] ON [AppDealDetails] ([DealId]);
    CREATE INDEX [IX_AppDealDetails_ProductId] ON [AppDealDetails] ([ProductId]);
END

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppProducts]') AND name = N'DealId')
BEGIN
    ALTER TABLE [AppProducts] ADD [DealId] int NULL;
END

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppProducts]') AND name = N'IsDeal')
BEGIN
    ALTER TABLE [AppProducts] ADD [IsDeal] bit NOT NULL DEFAULT 0;
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppProducts]') AND name = N'DealId')
    ALTER TABLE [AppProducts] DROP COLUMN [DealId];

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppProducts]') AND name = N'IsDeal')
    ALTER TABLE [AppProducts] DROP COLUMN [IsDeal];

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = N'AppDealDetails')
    DROP TABLE [AppDealDetails];

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = N'AppDeals')
    DROP TABLE [AppDeals];
");
        }
    }
}
