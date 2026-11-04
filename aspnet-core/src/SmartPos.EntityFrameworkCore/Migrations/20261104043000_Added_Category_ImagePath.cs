using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SmartPos.EntityFrameworkCore;

#nullable disable

namespace SmartPos.Migrations
{
    [DbContext(typeof(SmartPosDbContext))]
    [Migration("20261104043000_Added_Category_ImagePath")]
    public partial class Added_Category_ImagePath : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppCategories]') AND name = N'ImagePath')
BEGIN
    ALTER TABLE [AppCategories] ADD [ImagePath] nvarchar(512) NULL;
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppCategories]') AND name = N'ImagePath')
BEGIN
    ALTER TABLE [AppCategories] DROP COLUMN [ImagePath];
END
");
        }
    }
}
