using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SmartPos.EntityFrameworkCore;

#nullable disable

namespace SmartPos.Migrations
{
    [DbContext(typeof(SmartPosDbContext))]
    [Migration("20261031100000_Added_Product_CardTag")]
    public partial class Added_Product_CardTag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppProducts]') AND name = N'CardTag')
BEGIN
    ALTER TABLE [AppProducts] ADD [CardTag] nvarchar(64) NULL;
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppProducts]') AND name = N'CardTag')
BEGIN
    ALTER TABLE [AppProducts] DROP COLUMN [CardTag];
END
");
        }
    }
}
