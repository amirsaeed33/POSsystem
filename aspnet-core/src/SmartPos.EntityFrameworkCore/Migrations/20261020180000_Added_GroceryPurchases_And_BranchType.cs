using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SmartPos.EntityFrameworkCore;

#nullable disable

namespace SmartPos.Migrations
{
    [DbContext(typeof(SmartPosDbContext))]
    [Migration("20261020180000_Added_GroceryPurchases_And_BranchType")]
    public partial class Added_GroceryPurchases_And_BranchType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppBranches]') AND name = N'BranchType')
BEGIN
    ALTER TABLE [AppBranches] ADD [BranchType] nvarchar(64) NULL;
END

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'AppGroceryPurchases')
BEGIN
    CREATE TABLE [dbo].[AppGroceryPurchases] (
        [Id] int IDENTITY(1,1) NOT NULL,
        [CreationTime] datetime2 NOT NULL,
        [CreatorUserId] bigint NULL,
        [LastModificationTime] datetime2 NULL,
        [LastModifierUserId] bigint NULL,
        [IsDeleted] bit NOT NULL CONSTRAINT [DF_AppGroceryPurchases_IsDeleted] DEFAULT (0),
        [DeleterUserId] bigint NULL,
        [DeletionTime] datetime2 NULL,
        [TenantId] int NULL,
        [BranchId] int NOT NULL,
        [ProductName] nvarchar(256) NOT NULL,
        [PurchasePrice] decimal(18,2) NOT NULL,
        [Unit] nvarchar(64) NOT NULL,
        [UnitId] int NULL,
        [PurchaseDate] datetime2 NOT NULL,
        CONSTRAINT [PK_AppGroceryPurchases] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_AppGroceryPurchases_AppBranches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [dbo].[AppBranches] ([Id]),
        CONSTRAINT [FK_AppGroceryPurchases_AppUnits_UnitId] FOREIGN KEY ([UnitId]) REFERENCES [dbo].[AppUnits] ([Id])
    );

    CREATE NONCLUSTERED INDEX [IX_AppGroceryPurchases_BranchId] ON [dbo].[AppGroceryPurchases] ([BranchId] ASC);
    CREATE NONCLUSTERED INDEX [IX_AppGroceryPurchases_BranchId_PurchaseDate] ON [dbo].[AppGroceryPurchases] ([BranchId] ASC, [PurchaseDate] ASC);
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = N'AppGroceryPurchases')
BEGIN
    DROP TABLE [dbo].[AppGroceryPurchases];
END

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppBranches]') AND name = N'BranchType')
BEGIN
    ALTER TABLE [AppBranches] DROP COLUMN [BranchType];
END
");
        }
    }
}
