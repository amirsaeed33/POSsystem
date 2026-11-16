using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SmartPos.EntityFrameworkCore;

#nullable disable

namespace SmartPos.Migrations
{
    [DbContext(typeof(SmartPosDbContext))]
    [Migration("20261104084000_Drop_Customer_And_Supplier_Address_Columns")]
    public partial class Drop_Customer_And_Supplier_Address_Columns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Move legacy customer Address/GPS into AppSavedAddresses (OwnerType=1 Customer).
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[AppSavedAddresses]', N'U') IS NOT NULL
   AND COL_LENGTH(N'AppCustomers', N'Address') IS NOT NULL
   AND COL_LENGTH(N'AppCustomers', N'Latitude') IS NOT NULL
   AND COL_LENGTH(N'AppCustomers', N'Longitude') IS NOT NULL
BEGIN
    INSERT INTO [AppSavedAddresses] (
        [TenantId], [OwnerType], [OwnerId], [Label], [Address], [Landmark],
        [DeliveryAreaId], [DeliveryAreaName], [Latitude], [Longitude], [IsDefault],
        [CreationTime], [IsDeleted]
    )
    SELECT
        c.[TenantId], 1, c.[Id], N'Default', LTRIM(RTRIM(c.[Address])),
        NULL, NULL, NULL, c.[Latitude], c.[Longitude], 1, SYSUTCDATETIME(), 0
    FROM [AppCustomers] c
    WHERE c.[IsDeleted] = 0
      AND c.[Address] IS NOT NULL
      AND LTRIM(RTRIM(c.[Address])) <> N''
      AND NOT EXISTS (
          SELECT 1 FROM [AppSavedAddresses] a
          WHERE a.[OwnerType] = 1 AND a.[OwnerId] = c.[Id] AND a.[IsDeleted] = 0
            AND LOWER(LTRIM(RTRIM(a.[Address]))) = LOWER(LTRIM(RTRIM(c.[Address])))
      );
END
ELSE IF OBJECT_ID(N'[AppSavedAddresses]', N'U') IS NOT NULL
   AND COL_LENGTH(N'AppCustomers', N'Address') IS NOT NULL
BEGIN
    INSERT INTO [AppSavedAddresses] (
        [TenantId], [OwnerType], [OwnerId], [Label], [Address], [Landmark],
        [DeliveryAreaId], [DeliveryAreaName], [Latitude], [Longitude], [IsDefault],
        [CreationTime], [IsDeleted]
    )
    SELECT
        c.[TenantId], 1, c.[Id], N'Default', LTRIM(RTRIM(c.[Address])),
        NULL, NULL, NULL, NULL, NULL, 1, SYSUTCDATETIME(), 0
    FROM [AppCustomers] c
    WHERE c.[IsDeleted] = 0
      AND c.[Address] IS NOT NULL
      AND LTRIM(RTRIM(c.[Address])) <> N''
      AND NOT EXISTS (
          SELECT 1 FROM [AppSavedAddresses] a
          WHERE a.[OwnerType] = 1 AND a.[OwnerId] = c.[Id] AND a.[IsDeleted] = 0
            AND LOWER(LTRIM(RTRIM(a.[Address]))) = LOWER(LTRIM(RTRIM(c.[Address])))
      );
END
");

            // Move staff Address into AppSavedAddresses (OwnerType=2 Staff).
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[AppSavedAddresses]', N'U') IS NOT NULL
   AND COL_LENGTH(N'AppStaff', N'Address') IS NOT NULL
BEGIN
    INSERT INTO [AppSavedAddresses] (
        [TenantId], [OwnerType], [OwnerId], [Label], [Address], [Landmark],
        [DeliveryAreaId], [DeliveryAreaName], [Latitude], [Longitude], [IsDefault],
        [CreationTime], [IsDeleted]
    )
    SELECT
        s.[TenantId], 2, s.[Id], N'Default', LTRIM(RTRIM(s.[Address])),
        NULL, NULL, NULL, NULL, NULL, 1, SYSUTCDATETIME(), 0
    FROM [AppStaff] s
    WHERE s.[IsDeleted] = 0
      AND s.[Address] IS NOT NULL
      AND LTRIM(RTRIM(s.[Address])) <> N''
      AND NOT EXISTS (
          SELECT 1 FROM [AppSavedAddresses] a
          WHERE a.[OwnerType] = 2 AND a.[OwnerId] = s.[Id] AND a.[IsDeleted] = 0
            AND LOWER(LTRIM(RTRIM(a.[Address]))) = LOWER(LTRIM(RTRIM(s.[Address])))
      );
END
");

            // Move supplier Address into AppSavedAddresses (OwnerType=3 Supplier) if still present.
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[AppSavedAddresses]', N'U') IS NOT NULL
   AND COL_LENGTH(N'AppSuppliers', N'Address') IS NOT NULL
BEGIN
    INSERT INTO [AppSavedAddresses] (
        [TenantId], [OwnerType], [OwnerId], [Label], [Address], [Landmark],
        [DeliveryAreaId], [DeliveryAreaName], [Latitude], [Longitude], [IsDefault],
        [CreationTime], [IsDeleted]
    )
    SELECT
        s.[TenantId], 3, s.[Id], N'Default', LTRIM(RTRIM(s.[Address])),
        NULL, NULL, NULL, NULL, NULL, 1, SYSUTCDATETIME(), 0
    FROM [AppSuppliers] s
    WHERE s.[IsDeleted] = 0
      AND s.[Address] IS NOT NULL
      AND LTRIM(RTRIM(s.[Address])) <> N''
      AND NOT EXISTS (
          SELECT 1 FROM [AppSavedAddresses] a
          WHERE a.[OwnerType] = 3 AND a.[OwnerId] = s.[Id] AND a.[IsDeleted] = 0
            AND LOWER(LTRIM(RTRIM(a.[Address]))) = LOWER(LTRIM(RTRIM(s.[Address])))
      );
END
");

            migrationBuilder.Sql(@"
IF COL_LENGTH(N'AppCustomers', N'Longitude') IS NOT NULL
    ALTER TABLE [AppCustomers] DROP COLUMN [Longitude];
IF COL_LENGTH(N'AppCustomers', N'Latitude') IS NOT NULL
    ALTER TABLE [AppCustomers] DROP COLUMN [Latitude];
IF COL_LENGTH(N'AppCustomers', N'Address') IS NOT NULL
    ALTER TABLE [AppCustomers] DROP COLUMN [Address];
IF COL_LENGTH(N'AppStaff', N'Address') IS NOT NULL
    ALTER TABLE [AppStaff] DROP COLUMN [Address];
IF COL_LENGTH(N'AppSuppliers', N'Address') IS NOT NULL
    ALTER TABLE [AppSuppliers] DROP COLUMN [Address];
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH(N'AppCustomers', N'Address') IS NULL
    ALTER TABLE [AppCustomers] ADD [Address] nvarchar(512) NULL;
IF COL_LENGTH(N'AppCustomers', N'Latitude') IS NULL
    ALTER TABLE [AppCustomers] ADD [Latitude] float NULL;
IF COL_LENGTH(N'AppCustomers', N'Longitude') IS NULL
    ALTER TABLE [AppCustomers] ADD [Longitude] float NULL;
IF COL_LENGTH(N'AppStaff', N'Address') IS NULL
    ALTER TABLE [AppStaff] ADD [Address] nvarchar(512) NULL;
IF COL_LENGTH(N'AppSuppliers', N'Address') IS NULL
    ALTER TABLE [AppSuppliers] ADD [Address] nvarchar(512) NULL;
");
        }
    }
}
