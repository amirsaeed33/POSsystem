using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartPos.Migrations
{
    /// <inheritdoc />
    public partial class Removed_Product_StockQuantity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppProducts]') AND name = N'StockQuantity')
BEGIN
    DECLARE @v1 sysname;
    SELECT @v1 = [d].[name] FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AppProducts]') AND [c].[name] = N'StockQuantity');
    IF @v1 IS NOT NULL EXEC(N'ALTER TABLE [AppProducts] DROP CONSTRAINT [' + @v1 + '];');
    ALTER TABLE [AppProducts] DROP COLUMN [StockQuantity];
END");

            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppBranches]') AND name = N'IsDefault')
BEGIN
    DECLARE @v2 sysname;
    SELECT @v2 = [d].[name] FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AppBranches]') AND [c].[name] = N'IsDefault');
    IF @v2 IS NOT NULL EXEC(N'ALTER TABLE [AppBranches] DROP CONSTRAINT [' + @v2 + '];');
    ALTER TABLE [AppBranches] DROP COLUMN [IsDefault];
END");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppUnits]') AND name = N'BranchId')
    ALTER TABLE [AppUnits] ADD [BranchId] int NOT NULL DEFAULT 0;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppUnits]') AND name = N'IsActive')
    ALTER TABLE [AppUnits] ADD [IsActive] bit NOT NULL DEFAULT 0;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppUnits]') AND name = N'Symbol')
    ALTER TABLE [AppUnits] ADD [Symbol] nvarchar(32) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppStaff]') AND name = N'LoginPassword')
    ALTER TABLE [AppStaff] ADD [LoginPassword] nvarchar(100) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppStaff]') AND name = N'UserId')
    ALTER TABLE [AppStaff] ADD [UserId] bigint NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppCategories]') AND name = N'BranchId')
    ALTER TABLE [AppCategories] ADD [BranchId] int NOT NULL DEFAULT 0;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppCategories]') AND name = N'IsActive')
    ALTER TABLE [AppCategories] ADD [IsActive] bit NOT NULL DEFAULT 0;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppBrands]') AND name = N'BranchId')
    ALTER TABLE [AppBrands] ADD [BranchId] int NOT NULL DEFAULT 0;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppBrands]') AND name = N'IsActive')
    ALTER TABLE [AppBrands] ADD [IsActive] bit NOT NULL DEFAULT 0;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppBranches]') AND name = N'ActivationTokenExpiresAt')
    ALTER TABLE [AppBranches] ADD [ActivationTokenExpiresAt] datetime2 NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppBranches]') AND name = N'ActivationTokenHash')
    ALTER TABLE [AppBranches] ADD [ActivationTokenHash] nvarchar(128) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppBranches]') AND name = N'DiscountAmount')
    ALTER TABLE [AppBranches] ADD [DiscountAmount] decimal(18,2) NOT NULL DEFAULT 0;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppBranches]') AND name = N'DiscountPercent')
    ALTER TABLE [AppBranches] ADD [DiscountPercent] decimal(18,2) NOT NULL DEFAULT 0;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppBranches]') AND name = N'TaxPercent')
    ALTER TABLE [AppBranches] ADD [TaxPercent] decimal(18,2) NOT NULL DEFAULT 0;
");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'AppHostCatalogItems')
BEGIN
    CREATE TABLE [AppHostCatalogItems] (
        [Id] int NOT NULL IDENTITY,
        [Type] nvarchar(32) NOT NULL,
        [CompanyTypeId] int NULL,
        [Name] nvarchar(128) NOT NULL,
        [Symbol] nvarchar(32) NULL,
        [IsActive] bit NOT NULL,
        [CreationTime] datetime2 NOT NULL,
        [CreatorUserId] bigint NULL,
        [LastModificationTime] datetime2 NULL,
        [LastModifierUserId] bigint NULL,
        [IsDeleted] bit NOT NULL,
        [DeleterUserId] bigint NULL,
        [DeletionTime] datetime2 NULL,
        CONSTRAINT [PK_AppHostCatalogItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AppHostCatalogItems_AppHostCatalogItems_CompanyTypeId] FOREIGN KEY ([CompanyTypeId]) REFERENCES [AppHostCatalogItems] ([Id]) ON DELETE NO ACTION
    );
END

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'AppBranchSeedRequests')
BEGIN
    CREATE TABLE [AppBranchSeedRequests] (
        [Id] int NOT NULL IDENTITY,
        [TenantId] int NOT NULL,
        [BranchId] int NOT NULL,
        [RequestedByUserId] bigint NOT NULL,
        [CompanyTypeId] int NOT NULL,
        [Status] nvarchar(32) NOT NULL,
        [ApprovedByUserId] bigint NULL,
        [ApprovedDate] datetime2 NULL,
        [CreationTime] datetime2 NOT NULL,
        [CreatorUserId] bigint NULL,
        [LastModificationTime] datetime2 NULL,
        [LastModifierUserId] bigint NULL,
        [IsDeleted] bit NOT NULL,
        [DeleterUserId] bigint NULL,
        [DeletionTime] datetime2 NULL,
        CONSTRAINT [PK_AppBranchSeedRequests] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AppBranchSeedRequests_AppBranches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [AppBranches] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_AppBranchSeedRequests_AppHostCatalogItems_CompanyTypeId] FOREIGN KEY ([CompanyTypeId]) REFERENCES [AppHostCatalogItems] ([Id]) ON DELETE NO ACTION
    );
END

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'AppBranchSeedRequestItems')
BEGIN
    CREATE TABLE [AppBranchSeedRequestItems] (
        [Id] int NOT NULL IDENTITY,
        [BranchSeedRequestId] int NOT NULL,
        [HostItemId] int NOT NULL,
        [CreationTime] datetime2 NOT NULL,
        [CreatorUserId] bigint NULL,
        CONSTRAINT [PK_AppBranchSeedRequestItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AppBranchSeedRequestItems_AppBranchSeedRequests_BranchSeedRequestId] FOREIGN KEY ([BranchSeedRequestId]) REFERENCES [AppBranchSeedRequests] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AppBranchSeedRequestItems_AppHostCatalogItems_HostItemId] FOREIGN KEY ([HostItemId]) REFERENCES [AppHostCatalogItems] ([Id]) ON DELETE NO ACTION
    );
END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AppBrands_AppBranches_BranchId",
                table: "AppBrands");

            migrationBuilder.DropForeignKey(
                name: "FK_AppCategories_AppBranches_BranchId",
                table: "AppCategories");

            migrationBuilder.DropForeignKey(
                name: "FK_AppUnits_AppBranches_BranchId",
                table: "AppUnits");

            migrationBuilder.DropTable(
                name: "AppBranchSeedRequestItems");

            migrationBuilder.DropTable(
                name: "AppBranchSeedRequests");

            migrationBuilder.DropTable(
                name: "AppHostCatalogItems");

            migrationBuilder.DropIndex(
                name: "IX_AppUnits_BranchId",
                table: "AppUnits");

            migrationBuilder.DropIndex(
                name: "IX_AppUnits_TenantId_BranchId_Name",
                table: "AppUnits");

            migrationBuilder.DropIndex(
                name: "IX_AppStaff_UserId",
                table: "AppStaff");

            migrationBuilder.DropIndex(
                name: "IX_AppCategories_BranchId",
                table: "AppCategories");

            migrationBuilder.DropIndex(
                name: "IX_AppCategories_TenantId_BranchId_Name",
                table: "AppCategories");

            migrationBuilder.DropIndex(
                name: "IX_AppBrands_BranchId",
                table: "AppBrands");

            migrationBuilder.DropIndex(
                name: "IX_AppBrands_TenantId_BranchId_Name",
                table: "AppBrands");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "AppUnits");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "AppUnits");

            migrationBuilder.DropColumn(
                name: "Symbol",
                table: "AppUnits");

            migrationBuilder.DropColumn(
                name: "LoginPassword",
                table: "AppStaff");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "AppStaff");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "AppCategories");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "AppCategories");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "AppBrands");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "AppBrands");

            migrationBuilder.DropColumn(
                name: "ActivationTokenExpiresAt",
                table: "AppBranches");

            migrationBuilder.DropColumn(
                name: "ActivationTokenHash",
                table: "AppBranches");

            migrationBuilder.DropColumn(
                name: "DiscountAmount",
                table: "AppBranches");

            migrationBuilder.DropColumn(
                name: "DiscountPercent",
                table: "AppBranches");

            migrationBuilder.DropColumn(
                name: "TaxPercent",
                table: "AppBranches");

            migrationBuilder.AddColumn<decimal>(
                name: "StockQuantity",
                table: "AppProducts",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                table: "AppBranches",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
