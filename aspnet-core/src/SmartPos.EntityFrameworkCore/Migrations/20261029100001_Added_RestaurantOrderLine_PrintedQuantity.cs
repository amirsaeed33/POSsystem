using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SmartPos.EntityFrameworkCore;

#nullable disable

namespace SmartPos.Migrations
{
    [DbContext(typeof(SmartPosDbContext))]
    [Migration("20261029100001_Added_RestaurantOrderLine_PrintedQuantity")]
    public partial class Added_RestaurantOrderLine_PrintedQuantity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppRestaurantOrderLines]') AND name = N'PrintedQuantity')
BEGIN
    ALTER TABLE [AppRestaurantOrderLines] ADD [PrintedQuantity] decimal(18,2) NOT NULL
        CONSTRAINT [DF_AppRestaurantOrderLines_PrintedQuantity] DEFAULT (0);
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppRestaurantOrderLines]') AND name = N'PrintedQuantity')
BEGIN
    DECLARE @df sysname;
    SELECT @df = dc.name
    FROM sys.default_constraints dc
    INNER JOIN sys.columns c ON c.default_object_id = dc.object_id
    WHERE dc.parent_object_id = OBJECT_ID(N'[AppRestaurantOrderLines]') AND c.name = N'PrintedQuantity';
    IF @df IS NOT NULL EXEC(N'ALTER TABLE [AppRestaurantOrderLines] DROP CONSTRAINT [' + @df + N']');
    ALTER TABLE [AppRestaurantOrderLines] DROP COLUMN [PrintedQuantity];
END
");
        }
    }
}
