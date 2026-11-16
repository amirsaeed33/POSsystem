using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SmartPos.EntityFrameworkCore;

#nullable disable

namespace SmartPos.Migrations
{
    [DbContext(typeof(SmartPosDbContext))]
    [Migration("20261029120000_Added_Branch_SocialMediaUrls")]
    public partial class Added_Branch_SocialMediaUrls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppBranches]') AND name = N'FacebookUrl')
BEGIN
    ALTER TABLE [AppBranches] ADD [FacebookUrl] nvarchar(512) NULL;
END
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppBranches]') AND name = N'InstagramUrl')
BEGIN
    ALTER TABLE [AppBranches] ADD [InstagramUrl] nvarchar(512) NULL;
END
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppBranches]') AND name = N'YoutubeUrl')
BEGIN
    ALTER TABLE [AppBranches] ADD [YoutubeUrl] nvarchar(512) NULL;
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppBranches]') AND name = N'FacebookUrl')
BEGIN
    ALTER TABLE [AppBranches] DROP COLUMN [FacebookUrl];
END
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppBranches]') AND name = N'InstagramUrl')
BEGIN
    ALTER TABLE [AppBranches] DROP COLUMN [InstagramUrl];
END
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppBranches]') AND name = N'YoutubeUrl')
BEGIN
    ALTER TABLE [AppBranches] DROP COLUMN [YoutubeUrl];
END
");
        }
    }
}
