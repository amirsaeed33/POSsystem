using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SmartPos.EntityFrameworkCore;

#nullable disable

namespace SmartPos.Migrations
{
    [DbContext(typeof(SmartPosDbContext))]
    [Migration("20261029140000_Added_LedgerEntry_BranchId")]
    public partial class Added_LedgerEntry_BranchId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
SET QUOTED_IDENTIFIER ON;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppLedgerEntries]') AND name = N'BranchId')
BEGIN
    ALTER TABLE [AppLedgerEntries] ADD [BranchId] int NULL;
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AppLedgerEntries_BranchId' AND object_id = OBJECT_ID(N'[AppLedgerEntries]'))
BEGIN
    CREATE INDEX [IX_AppLedgerEntries_BranchId] ON [AppLedgerEntries] ([BranchId]);
END

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AppLedgerEntries_AppBranches_BranchId')
BEGIN
    ALTER TABLE [AppLedgerEntries] WITH CHECK
    ADD CONSTRAINT [FK_AppLedgerEntries_AppBranches_BranchId]
    FOREIGN KEY ([BranchId]) REFERENCES [AppBranches] ([Id]);
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AppLedgerEntries_AppBranches_BranchId')
BEGIN
    ALTER TABLE [AppLedgerEntries] DROP CONSTRAINT [FK_AppLedgerEntries_AppBranches_BranchId];
END
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AppLedgerEntries_BranchId' AND object_id = OBJECT_ID(N'[AppLedgerEntries]'))
BEGIN
    DROP INDEX [IX_AppLedgerEntries_BranchId] ON [AppLedgerEntries];
END
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppLedgerEntries]') AND name = N'BranchId')
BEGIN
    ALTER TABLE [AppLedgerEntries] DROP COLUMN [BranchId];
END
");
        }
    }
}