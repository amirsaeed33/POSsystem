using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartPos.Migrations
{
    /// <inheritdoc />
    public partial class Added_Product_BranchId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppProducts]') AND name = N'BranchId')
BEGIN
    ALTER TABLE [AppProducts] ADD [BranchId] int NOT NULL DEFAULT 0;
END;
");

            migrationBuilder.Sql(@"
DECLARE @DefaultBranchId INT;
SELECT TOP 1 @DefaultBranchId = Id FROM [AppBranches] ORDER BY Id;

IF @DefaultBranchId IS NOT NULL
BEGIN
    EXEC(N'UPDATE [AppProducts] SET [BranchId] = ' + @DefaultBranchId + ' WHERE [BranchId] = 0 OR [BranchId] NOT IN (SELECT Id FROM [AppBranches])');
END;
");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[AppProducts]') AND name = N'IX_AppProducts_BranchId')
BEGIN
    CREATE INDEX [IX_AppProducts_BranchId] ON [AppProducts] ([BranchId]);
END;

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[FK_AppProducts_AppBranches_BranchId]'))
BEGIN
    ALTER TABLE [AppProducts] ADD CONSTRAINT [FK_AppProducts_AppBranches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [AppBranches] ([Id]) ON DELETE NO ACTION;
END;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[FK_AppProducts_AppBranches_BranchId]'))
BEGIN
    ALTER TABLE [AppProducts] DROP CONSTRAINT [FK_AppProducts_AppBranches_BranchId];
END;

IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[AppProducts]') AND name = N'IX_AppProducts_BranchId')
BEGIN
    DROP INDEX [IX_AppProducts_BranchId] ON [AppProducts];
END;

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppProducts]') AND name = N'BranchId')
BEGIN
    ALTER TABLE [AppProducts] DROP COLUMN [BranchId];
END;
");
        }
    }
}
