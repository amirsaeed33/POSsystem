using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SmartPos.EntityFrameworkCore;

#nullable disable

namespace SmartPos.Migrations
{
    [DbContext(typeof(SmartPosDbContext))]
    [Migration("20261109210000_Added_Branch_CustomerFeedback")]
    public partial class Added_Branch_CustomerFeedback : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

IF COL_LENGTH('dbo.AppBranches', 'CustomerFeedbackName') IS NULL
    ALTER TABLE [dbo].[AppBranches] ADD [CustomerFeedbackName] nvarchar(128) NULL;

IF COL_LENGTH('dbo.AppBranches', 'CustomerFeedbackRole') IS NULL
    ALTER TABLE [dbo].[AppBranches] ADD [CustomerFeedbackRole] nvarchar(128) NULL;

IF COL_LENGTH('dbo.AppBranches', 'CustomerFeedbackCity') IS NULL
    ALTER TABLE [dbo].[AppBranches] ADD [CustomerFeedbackCity] nvarchar(128) NULL;

IF COL_LENGTH('dbo.AppBranches', 'CustomerFeedbackComment') IS NULL
    ALTER TABLE [dbo].[AppBranches] ADD [CustomerFeedbackComment] nvarchar(1000) NULL;

IF COL_LENGTH('dbo.AppBranches', 'CustomerFeedbackRating') IS NULL
    ALTER TABLE [dbo].[AppBranches] ADD [CustomerFeedbackRating] int NOT NULL CONSTRAINT [DF_AppBranches_CustomerFeedbackRating] DEFAULT (0);

IF COL_LENGTH('dbo.AppBranches', 'ShowCustomerFeedback') IS NULL
    ALTER TABLE [dbo].[AppBranches] ADD [ShowCustomerFeedback] bit NOT NULL CONSTRAINT [DF_AppBranches_ShowCustomerFeedback] DEFAULT (0);
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH('dbo.AppBranches', 'ShowCustomerFeedback') IS NOT NULL
    ALTER TABLE [dbo].[AppBranches] DROP CONSTRAINT [DF_AppBranches_ShowCustomerFeedback];
IF COL_LENGTH('dbo.AppBranches', 'ShowCustomerFeedback') IS NOT NULL
    ALTER TABLE [dbo].[AppBranches] DROP COLUMN [ShowCustomerFeedback];

IF COL_LENGTH('dbo.AppBranches', 'CustomerFeedbackRating') IS NOT NULL
    ALTER TABLE [dbo].[AppBranches] DROP CONSTRAINT [DF_AppBranches_CustomerFeedbackRating];
IF COL_LENGTH('dbo.AppBranches', 'CustomerFeedbackRating') IS NOT NULL
    ALTER TABLE [dbo].[AppBranches] DROP COLUMN [CustomerFeedbackRating];

IF COL_LENGTH('dbo.AppBranches', 'CustomerFeedbackComment') IS NOT NULL
    ALTER TABLE [dbo].[AppBranches] DROP COLUMN [CustomerFeedbackComment];

IF COL_LENGTH('dbo.AppBranches', 'CustomerFeedbackCity') IS NOT NULL
    ALTER TABLE [dbo].[AppBranches] DROP COLUMN [CustomerFeedbackCity];

IF COL_LENGTH('dbo.AppBranches', 'CustomerFeedbackRole') IS NOT NULL
    ALTER TABLE [dbo].[AppBranches] DROP COLUMN [CustomerFeedbackRole];

IF COL_LENGTH('dbo.AppBranches', 'CustomerFeedbackName') IS NOT NULL
    ALTER TABLE [dbo].[AppBranches] DROP COLUMN [CustomerFeedbackName];
");
        }
    }
}
