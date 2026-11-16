using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SmartPos.EntityFrameworkCore;

#nullable disable

namespace SmartPos.Migrations
{
    [DbContext(typeof(SmartPosDbContext))]
    [Migration("20261103120000_Added_DeliveryArea_Coordinates")]
    public partial class Added_DeliveryArea_Coordinates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppDeliveryAreas]') AND name = N'Latitude')
BEGIN
    ALTER TABLE [AppDeliveryAreas] ADD [Latitude] float NULL;
END

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppDeliveryAreas]') AND name = N'Longitude')
BEGIN
    ALTER TABLE [AppDeliveryAreas] ADD [Longitude] float NULL;
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppDeliveryAreas]') AND name = N'Longitude')
    ALTER TABLE [AppDeliveryAreas] DROP COLUMN [Longitude];

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppDeliveryAreas]') AND name = N'Latitude')
    ALTER TABLE [AppDeliveryAreas] DROP COLUMN [Latitude];
");
        }
    }
}
