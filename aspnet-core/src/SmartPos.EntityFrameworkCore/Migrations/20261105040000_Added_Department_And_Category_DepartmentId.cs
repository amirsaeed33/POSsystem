using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SmartPos.EntityFrameworkCore;

#nullable disable

namespace SmartPos.Migrations
{
    [DbContext(typeof(SmartPosDbContext))]
    [Migration("20261105040000_Added_Department_And_Category_DepartmentId")]
    public partial class Added_Department_And_Category_DepartmentId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'AppDepartments')
BEGIN
    CREATE TABLE [AppDepartments] (
        [Id] int IDENTITY(1,1) NOT NULL,
        [TenantId] int NULL,
        [BranchId] int NOT NULL,
        [Name] nvarchar(128) NOT NULL,
        [Description] nvarchar(512) NULL,
        [IsActive] bit NOT NULL CONSTRAINT [DF_AppDepartments_IsActive] DEFAULT (1),
        [CreationTime] datetime2 NOT NULL,
        [CreatorUserId] bigint NULL,
        [LastModificationTime] datetime2 NULL,
        [LastModifierUserId] bigint NULL,
        [IsDeleted] bit NOT NULL CONSTRAINT [DF_AppDepartments_IsDeleted] DEFAULT (0),
        [DeleterUserId] bigint NULL,
        [DeletionTime] datetime2 NULL,
        CONSTRAINT [PK_AppDepartments] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_AppDepartments_AppBranches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [AppBranches] ([Id])
    );

    CREATE NONCLUSTERED INDEX [IX_AppDepartments_BranchId] ON [AppDepartments] ([BranchId] ASC);
    CREATE UNIQUE NONCLUSTERED INDEX [IX_AppDepartments_TenantId_BranchId_Name] ON [AppDepartments] ([TenantId] ASC, [BranchId] ASC, [Name] ASC) WHERE ([IsDeleted] = 0);
END

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppCategories]') AND name = N'DepartmentId')
BEGIN
    ALTER TABLE [AppCategories] ADD [DepartmentId] int NULL;
    
    ALTER TABLE [AppCategories] ADD CONSTRAINT [FK_AppCategories_AppDepartments_DepartmentId]
        FOREIGN KEY ([DepartmentId]) REFERENCES [AppDepartments] ([Id]);

    CREATE NONCLUSTERED INDEX [IX_AppCategories_DepartmentId] ON [AppCategories] ([DepartmentId] ASC);
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AppCategories_AppDepartments_DepartmentId')
BEGIN
    ALTER TABLE [AppCategories] DROP CONSTRAINT [FK_AppCategories_AppDepartments_DepartmentId];
END

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AppCategories_DepartmentId' AND object_id = OBJECT_ID(N'[AppCategories]'))
BEGIN
    DROP INDEX [IX_AppCategories_DepartmentId] ON [AppCategories];
END

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppCategories]') AND name = N'DepartmentId')
BEGIN
    ALTER TABLE [AppCategories] DROP COLUMN [DepartmentId];
END

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = N'AppDepartments')
BEGIN
    DROP TABLE [AppDepartments];
END
");
        }
    }
}

