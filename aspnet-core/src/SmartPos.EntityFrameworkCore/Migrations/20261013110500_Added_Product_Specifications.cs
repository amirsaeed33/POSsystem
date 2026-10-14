using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SmartPos.EntityFrameworkCore;

#nullable disable

namespace SmartPos.Migrations
{
    [DbContext(typeof(SmartPosDbContext))]
    [Migration("20261013110500_Added_Product_Specifications")]
    public partial class Added_Product_Specifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppProducts]') AND name = N'Specifications')
    ALTER TABLE [AppProducts] ADD [Specifications] nvarchar(max) NULL;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Specifications",
                table: "AppProducts");
        }
    }
}
