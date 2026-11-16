using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SmartPos.EntityFrameworkCore;

#nullable disable

namespace SmartPos.Migrations
{
    [DbContext(typeof(SmartPosDbContext))]
    [Migration("20261106100000_Added_AppDevelopers")]
    public partial class Added_AppDevelopers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

IF OBJECT_ID(N'[dbo].[AppDevelopers]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AppDevelopers] (
        [Id] int NOT NULL IDENTITY(1,1),
        [Name] nvarchar(128) NOT NULL,
        [Email] nvarchar(256) NULL,
        [PhoneNumber] nvarchar(32) NULL,
        [Description] nvarchar(2000) NULL,
        [ProfileImagePath] nvarchar(512) NULL,
        [BackgroundImagePath] nvarchar(512) NULL,
        [Education] nvarchar(2000) NULL,
        [Tags] nvarchar(512) NULL,
        [IsActive] bit NOT NULL CONSTRAINT [DF_AppDevelopers_IsActive] DEFAULT (1),
        [SortOrder] int NOT NULL CONSTRAINT [DF_AppDevelopers_SortOrder] DEFAULT (0),
        [CreationTime] datetime2 NOT NULL,
        [CreatorUserId] bigint NULL,
        [LastModificationTime] datetime2 NULL,
        [LastModifierUserId] bigint NULL,
        [IsDeleted] bit NOT NULL CONSTRAINT [DF_AppDevelopers_IsDeleted] DEFAULT (0),
        [DeleterUserId] bigint NULL,
        [DeletionTime] datetime2 NULL,
        CONSTRAINT [PK_AppDevelopers] PRIMARY KEY ([Id])
    );

    CREATE INDEX [IX_AppDevelopers_IsDeleted_SortOrder_Name]
        ON [dbo].[AppDevelopers] ([IsDeleted], [SortOrder], [Name]);
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[AppDevelopers]', N'U') IS NOT NULL
BEGIN
    DROP TABLE [dbo].[AppDevelopers];
END
");
        }
    }
}
