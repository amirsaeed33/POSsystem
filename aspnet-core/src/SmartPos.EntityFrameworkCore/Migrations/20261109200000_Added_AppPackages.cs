using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SmartPos.EntityFrameworkCore;

#nullable disable

namespace SmartPos.Migrations
{
    [DbContext(typeof(SmartPosDbContext))]
    [Migration("20261109200000_Added_AppPackages")]
    public partial class Added_AppPackages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

IF OBJECT_ID(N'[dbo].[AppPackages]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AppPackages] (
        [Id] int NOT NULL IDENTITY(1,1),
        [Name] nvarchar(128) NOT NULL,
        [Price] nvarchar(64) NOT NULL,
        [Period] nvarchar(64) NULL,
        [Blurb] nvarchar(512) NULL,
        [Features] nvarchar(4000) NULL,
        [CtaLabel] nvarchar(64) NULL,
        [IsFeatured] bit NOT NULL CONSTRAINT [DF_AppPackages_IsFeatured] DEFAULT (0),
        [ContactViaWhatsApp] bit NOT NULL CONSTRAINT [DF_AppPackages_ContactViaWhatsApp] DEFAULT (0),
        [IsActive] bit NOT NULL CONSTRAINT [DF_AppPackages_IsActive] DEFAULT (1),
        [SortOrder] int NOT NULL CONSTRAINT [DF_AppPackages_SortOrder] DEFAULT (0),
        [CreationTime] datetime2 NOT NULL,
        [CreatorUserId] bigint NULL,
        [LastModificationTime] datetime2 NULL,
        [LastModifierUserId] bigint NULL,
        [IsDeleted] bit NOT NULL CONSTRAINT [DF_AppPackages_IsDeleted] DEFAULT (0),
        [DeleterUserId] bigint NULL,
        [DeletionTime] datetime2 NULL,
        CONSTRAINT [PK_AppPackages] PRIMARY KEY ([Id])
    );

    CREATE INDEX [IX_AppPackages_IsDeleted_SortOrder_Name]
        ON [dbo].[AppPackages] ([IsDeleted], [SortOrder], [Name]);
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[AppPackages]', N'U') IS NOT NULL
BEGIN
    DROP TABLE [dbo].[AppPackages];
END
");
        }
    }
}
