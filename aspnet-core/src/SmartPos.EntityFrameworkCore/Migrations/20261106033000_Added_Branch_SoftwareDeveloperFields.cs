using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SmartPos.EntityFrameworkCore;

#nullable disable

namespace SmartPos.Migrations
{
    [DbContext(typeof(SmartPosDbContext))]
    [Migration("20261106033000_Added_Branch_SoftwareDeveloperFields")]
    public partial class Added_Branch_SoftwareDeveloperFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppBranches]') AND name = N'SoftwareDeveloperName')
BEGIN
    ALTER TABLE [AppBranches] ADD [SoftwareDeveloperName] nvarchar(256) NULL;
END

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppBranches]') AND name = N'SoftwareDeveloperPhone')
BEGIN
    ALTER TABLE [AppBranches] ADD [SoftwareDeveloperPhone] nvarchar(64) NULL;
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppBranches]') AND name = N'SoftwareDeveloperPhone')
BEGIN
    ALTER TABLE [AppBranches] DROP COLUMN [SoftwareDeveloperPhone];
END

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppBranches]') AND name = N'SoftwareDeveloperName')
BEGIN
    ALTER TABLE [AppBranches] DROP COLUMN [SoftwareDeveloperName];
END
");
        }
    }
}

