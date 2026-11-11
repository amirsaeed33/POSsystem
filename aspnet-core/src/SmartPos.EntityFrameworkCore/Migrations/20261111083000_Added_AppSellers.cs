using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SmartPos.EntityFrameworkCore;

#nullable disable

namespace SmartPos.Migrations
{
    [DbContext(typeof(SmartPosDbContext))]
    [Migration("20261111083000_Added_AppSellers")]
    public partial class Added_AppSellers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

IF OBJECT_ID(N'[dbo].[AppSellers]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AppSellers] (
        [Id] int NOT NULL IDENTITY(1,1),
        [FullName] nvarchar(128) NOT NULL,
        [Email] nvarchar(256) NULL,
        [PhoneNumber] nvarchar(32) NOT NULL,
        [City] nvarchar(128) NULL,
        [CNIC] nvarchar(32) NULL,
        [CompanyName] nvarchar(128) NULL,
        [ExperienceNotes] nvarchar(2000) NULL,
        [Message] nvarchar(2000) NULL,
        [Status] tinyint NOT NULL CONSTRAINT [DF_AppSellers_Status] DEFAULT (0),
        [IsPubliclyListed] bit NOT NULL CONSTRAINT [DF_AppSellers_IsPubliclyListed] DEFAULT (0),
        [CommissionPerSalePkr] decimal(18,2) NOT NULL CONSTRAINT [DF_AppSellers_CommissionPerSalePkr] DEFAULT (3000),
        [ReviewedTime] datetime2 NULL,
        [ReviewNotes] nvarchar(1000) NULL,
        [RejectionReason] nvarchar(1000) NULL,
        [CreationTime] datetime2 NOT NULL,
        [CreatorUserId] bigint NULL,
        [LastModificationTime] datetime2 NULL,
        [LastModifierUserId] bigint NULL,
        [IsDeleted] bit NOT NULL CONSTRAINT [DF_AppSellers_IsDeleted] DEFAULT (0),
        [DeleterUserId] bigint NULL,
        [DeletionTime] datetime2 NULL,
        CONSTRAINT [PK_AppSellers] PRIMARY KEY ([Id])
    );

    CREATE INDEX [IX_AppSellers_IsDeleted_Status_CreationTime]
        ON [dbo].[AppSellers] ([IsDeleted], [Status], [CreationTime] DESC);

    CREATE INDEX [IX_AppSellers_IsDeleted_IsPubliclyListed_Status]
        ON [dbo].[AppSellers] ([IsDeleted], [IsPubliclyListed], [Status]);
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[AppSellers]', N'U') IS NOT NULL
BEGIN
    DROP TABLE [dbo].[AppSellers];
END
");
        }
    }
}
