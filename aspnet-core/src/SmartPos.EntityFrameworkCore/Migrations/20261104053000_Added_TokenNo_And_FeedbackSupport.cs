using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartPos.Migrations
{
    /// <inheritdoc />
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
