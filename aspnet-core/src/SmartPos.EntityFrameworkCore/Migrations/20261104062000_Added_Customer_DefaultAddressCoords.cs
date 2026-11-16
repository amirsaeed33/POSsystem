using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SmartPos.EntityFrameworkCore;

#nullable disable

namespace SmartPos.Migrations
{
    [DbContext(typeof(SmartPosDbContext))]
    [Migration("20261104062000_Added_Customer_DefaultAddressCoords")]
    public partial class Added_Customer_DefaultAddressCoords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppCustomers]') AND name = N'Latitude')
BEGIN
    ALTER TABLE [AppCustomers] ADD [Latitude] float NULL;
END
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppCustomers]') AND name = N'Longitude')
BEGIN
    ALTER TABLE [AppCustomers] ADD [Longitude] float NULL;
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppCustomers]') AND name = N'Longitude')
BEGIN
    ALTER TABLE [AppCustomers] DROP COLUMN [Longitude];
END
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppCustomers]') AND name = N'Latitude')
BEGIN
    ALTER TABLE [AppCustomers] DROP COLUMN [Latitude];
END
");
        }
    }
}
