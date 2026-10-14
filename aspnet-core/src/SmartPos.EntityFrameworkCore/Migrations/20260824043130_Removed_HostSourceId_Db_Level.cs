using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartPos.Migrations
{
    /// <inheritdoc />
    public partial class Removed_HostSourceId_Db_Level : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppCategories]') AND name = N'HostSourceId')
BEGIN
    DECLARE @v1 sysname;
    SELECT @v1 = [d].[name] FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AppCategories]') AND [c].[name] = N'HostSourceId');
    IF @v1 IS NOT NULL EXEC(N'ALTER TABLE [AppCategories] DROP CONSTRAINT [' + @v1 + '];');
    ALTER TABLE [AppCategories] DROP COLUMN [HostSourceId];
END");

            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppBrands]') AND name = N'HostSourceId')
BEGIN
    DECLARE @v2 sysname;
    SELECT @v2 = [d].[name] FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AppBrands]') AND [c].[name] = N'HostSourceId');
    IF @v2 IS NOT NULL EXEC(N'ALTER TABLE [AppBrands] DROP CONSTRAINT [' + @v2 + '];');
    ALTER TABLE [AppBrands] DROP COLUMN [HostSourceId];
END");

            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppUnits]') AND name = N'HostSourceId')
BEGIN
    DECLARE @v3 sysname;
    SELECT @v3 = [d].[name] FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AppUnits]') AND [c].[name] = N'HostSourceId');
    IF @v3 IS NOT NULL EXEC(N'ALTER TABLE [AppUnits] DROP CONSTRAINT [' + @v3 + '];');
    ALTER TABLE [AppUnits] DROP COLUMN [HostSourceId];
END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "HostSourceId",
                table: "AppCategories",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HostSourceId",
                table: "AppBrands",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HostSourceId",
                table: "AppUnits",
                type: "int",
                nullable: true);
        }
    }
}
