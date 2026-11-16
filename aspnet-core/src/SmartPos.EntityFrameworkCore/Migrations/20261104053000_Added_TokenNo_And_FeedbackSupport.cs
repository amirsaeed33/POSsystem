using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SmartPos.EntityFrameworkCore;

#nullable disable

namespace SmartPos.Migrations
{
    [DbContext(typeof(SmartPosDbContext))]
    [Migration("20261104053000_Added_TokenNo_And_FeedbackSupport")]
    public partial class Added_TokenNo_And_FeedbackSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppRestaurantOrders]') AND name = N'TokenNo')
BEGIN
    ALTER TABLE [AppRestaurantOrders] ADD [TokenNo] int NULL;
END

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppSales]') AND name = N'TokenNo')
BEGIN
    ALTER TABLE [AppSales] ADD [TokenNo] int NULL;
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppRestaurantOrders]') AND name = N'TokenNo')
BEGIN
    ALTER TABLE [AppRestaurantOrders] DROP COLUMN [TokenNo];
END

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppSales]') AND name = N'TokenNo')
BEGIN
    ALTER TABLE [AppSales] DROP COLUMN [TokenNo];
END
");
        }
    }
}
