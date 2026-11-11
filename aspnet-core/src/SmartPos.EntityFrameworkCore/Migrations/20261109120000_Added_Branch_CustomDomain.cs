using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SmartPos.EntityFrameworkCore;

#nullable disable

namespace SmartPos.Migrations
{
    [DbContext(typeof(SmartPosDbContext))]
    [Migration("20261109120000_Added_Branch_CustomDomain")]
    public partial class Added_Branch_CustomDomain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppBranches]') AND name = N'CustomDomain')
BEGIN
    ALTER TABLE [AppBranches] ADD [CustomDomain] nvarchar(256) NULL;
END

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppBranches]') AND name = N'StorefrontTitle')
BEGIN
    ALTER TABLE [AppBranches] ADD [StorefrontTitle] nvarchar(256) NULL;
END
");

            migrationBuilder.Sql(@"
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_AppBranches_CustomDomain'
      AND object_id = OBJECT_ID(N'[AppBranches]')
)
BEGIN
    EXEC(N'CREATE UNIQUE NONCLUSTERED INDEX [IX_AppBranches_CustomDomain]
    ON [AppBranches]([CustomDomain])
    WHERE [CustomDomain] IS NOT NULL AND [CustomDomain] <> N'''';');
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

IF EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_AppBranches_CustomDomain'
      AND object_id = OBJECT_ID(N'[AppBranches]')
)
BEGIN
    DROP INDEX [IX_AppBranches_CustomDomain] ON [AppBranches];
END
");

            migrationBuilder.Sql(@"
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppBranches]') AND name = N'StorefrontTitle')
BEGIN
    ALTER TABLE [AppBranches] DROP COLUMN [StorefrontTitle];
END

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppBranches]') AND name = N'CustomDomain')
BEGIN
    ALTER TABLE [AppBranches] DROP COLUMN [CustomDomain];
END
");
        }
    }
}
