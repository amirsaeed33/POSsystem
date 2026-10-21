using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SmartPos.EntityFrameworkCore;

#nullable disable

namespace SmartPos.Migrations
{
    [DbContext(typeof(SmartPosDbContext))]
    [Migration("20261021100000_Added_Restaurant_Tables_And_Branch_BusinessType")]
    public partial class Added_Restaurant_Tables_And_Branch_BusinessType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppBranches]') AND name = N'BusinessType')
BEGIN
    ALTER TABLE [AppBranches] ADD [BusinessType] tinyint NOT NULL CONSTRAINT [DF_AppBranches_BusinessType] DEFAULT 1;
END

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppBranches]') AND name = N'ServiceChargePercent')
BEGIN
    ALTER TABLE [AppBranches] ADD [ServiceChargePercent] decimal(18,2) NOT NULL CONSTRAINT [DF_AppBranches_ServiceChargePercent] DEFAULT 0;
END

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'AppRestaurantTables')
BEGIN
    CREATE TABLE [AppRestaurantTables] (
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
        [TableNo] nvarchar(64) NOT NULL,
        [Area] nvarchar(64) NULL,
        [Capacity] int NOT NULL CONSTRAINT [DF_AppRestaurantTables_Capacity] DEFAULT 4,
        [Status] tinyint NOT NULL CONSTRAINT [DF_AppRestaurantTables_Status] DEFAULT 0,
        [CurrentOrderId] int NULL,
        [IsActive] bit NOT NULL CONSTRAINT [DF_AppRestaurantTables_IsActive] DEFAULT 1,
        CONSTRAINT [PK_AppRestaurantTables] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AppRestaurantTables_AppBranches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [AppBranches] ([Id])
    );
    CREATE INDEX [IX_AppRestaurantTables_BranchId_TableNo] ON [AppRestaurantTables] ([BranchId], [TableNo]);
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = N'AppRestaurantTables')
    DROP TABLE [AppRestaurantTables];

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppBranches]') AND name = N'ServiceChargePercent')
    ALTER TABLE [AppBranches] DROP COLUMN [ServiceChargePercent];

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppBranches]') AND name = N'BusinessType')
    ALTER TABLE [AppBranches] DROP COLUMN [BusinessType];
");
        }
    }
}
