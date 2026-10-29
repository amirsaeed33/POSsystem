using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SmartPos.EntityFrameworkCore;

#nullable disable

namespace SmartPos.Migrations
{
    [DbContext(typeof(SmartPosDbContext))]
    [Migration("20261029100000_Added_RestaurantTable_IsCarBay")]
    public partial class Added_RestaurantTable_IsCarBay : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppRestaurantTables]') AND name = N'IsCarBay')
BEGIN
    ALTER TABLE [AppRestaurantTables] ADD [IsCarBay] bit NOT NULL CONSTRAINT [DF_AppRestaurantTables_IsCarBay] DEFAULT 0;
END

-- Backfill existing car-like slots (Car Parking area or TableNo starting with Car)
UPDATE [AppRestaurantTables]
SET [IsCarBay] = 1
WHERE [IsCarBay] = 0
  AND (
        LOWER(LTRIM(RTRIM(ISNULL([Area], '')))) = N'car parking'
     OR LOWER(LTRIM(RTRIM(ISNULL([Area], '')))) LIKE N'%car park%'
     OR LOWER(LTRIM(RTRIM(ISNULL([TableNo], '')))) LIKE N'car %'
     OR LOWER(LTRIM(RTRIM(ISNULL([TableNo], '')))) LIKE N'car-%'
  );
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[AppRestaurantTables]') AND name = N'IsCarBay')
BEGIN
    DECLARE @df sysname;
    SELECT @df = dc.name
    FROM sys.default_constraints dc
    INNER JOIN sys.columns c ON c.default_object_id = dc.object_id
    WHERE dc.parent_object_id = OBJECT_ID(N'[AppRestaurantTables]') AND c.name = N'IsCarBay';
    IF @df IS NOT NULL EXEC(N'ALTER TABLE [AppRestaurantTables] DROP CONSTRAINT [' + @df + N']');
    ALTER TABLE [AppRestaurantTables] DROP COLUMN [IsCarBay];
END
");
        }
    }
}
