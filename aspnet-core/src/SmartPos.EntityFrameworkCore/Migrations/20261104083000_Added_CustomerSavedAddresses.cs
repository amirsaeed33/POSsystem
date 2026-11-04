using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SmartPos.EntityFrameworkCore;

#nullable disable

namespace SmartPos.Migrations
{
    [DbContext(typeof(SmartPosDbContext))]
    [Migration("20261104083000_Added_CustomerSavedAddresses")]
    public partial class Added_CustomerSavedAddresses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Shared address table for Customer / Staff / Supplier owners.
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[AppSavedAddresses]', N'U') IS NULL
BEGIN
    CREATE TABLE [AppSavedAddresses] (
        [Id] int NOT NULL IDENTITY(1,1),
        [TenantId] int NULL,
        [OwnerType] int NOT NULL,
        [OwnerId] int NOT NULL,
        [Label] nvarchar(64) NULL,
        [Address] nvarchar(512) NOT NULL,
        [Landmark] nvarchar(256) NULL,
        [DeliveryAreaId] int NULL,
        [DeliveryAreaName] nvarchar(128) NULL,
        [Latitude] float NULL,
        [Longitude] float NULL,
        [IsDefault] bit NOT NULL CONSTRAINT [DF_AppSavedAddresses_IsDefault] DEFAULT(0),
        [CreationTime] datetime2 NOT NULL,
        [CreatorUserId] bigint NULL,
        [LastModificationTime] datetime2 NULL,
        [LastModifierUserId] bigint NULL,
        [IsDeleted] bit NOT NULL CONSTRAINT [DF_AppSavedAddresses_IsDeleted] DEFAULT(0),
        [DeleterUserId] bigint NULL,
        [DeletionTime] datetime2 NULL,
        CONSTRAINT [PK_AppSavedAddresses] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AppSavedAddresses_AppDeliveryAreas_DeliveryAreaId]
            FOREIGN KEY ([DeliveryAreaId]) REFERENCES [AppDeliveryAreas]([Id]) ON DELETE SET NULL
    );

    CREATE INDEX [IX_AppSavedAddresses_OwnerType_OwnerId]
        ON [AppSavedAddresses]([OwnerType], [OwnerId]);
    CREATE INDEX [IX_AppSavedAddresses_DeliveryAreaId]
        ON [AppSavedAddresses]([DeliveryAreaId]);
    CREATE INDEX [IX_AppSavedAddresses_TenantId]
        ON [AppSavedAddresses]([TenantId]);
END

-- Upgrade path: rename legacy customer-only table if it already exists.
IF OBJECT_ID(N'[AppCustomerSavedAddresses]', N'U') IS NOT NULL
   AND OBJECT_ID(N'[AppSavedAddresses]', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'AppCustomerSavedAddresses', N'CustomerId') IS NOT NULL
    BEGIN
        INSERT INTO [AppSavedAddresses] (
            [TenantId], [OwnerType], [OwnerId], [Label], [Address], [Landmark],
            [DeliveryAreaId], [DeliveryAreaName], [Latitude], [Longitude], [IsDefault],
            [CreationTime], [CreatorUserId], [LastModificationTime], [LastModifierUserId],
            [IsDeleted], [DeleterUserId], [DeletionTime]
        )
        SELECT
            [TenantId], 1, [CustomerId], [Label], [Address], [Landmark],
            [DeliveryAreaId], [DeliveryAreaName], [Latitude], [Longitude], [IsDefault],
            [CreationTime], [CreatorUserId], [LastModificationTime], [LastModifierUserId],
            [IsDeleted], [DeleterUserId], [DeletionTime]
        FROM [AppCustomerSavedAddresses] c
        WHERE NOT EXISTS (
            SELECT 1 FROM [AppSavedAddresses] s
            WHERE s.[OwnerType] = 1 AND s.[OwnerId] = c.[CustomerId]
              AND LOWER(LTRIM(RTRIM(s.[Address]))) = LOWER(LTRIM(RTRIM(c.[Address])))
              AND ISNULL(s.[DeliveryAreaId], -1) = ISNULL(c.[DeliveryAreaId], -1)
        );
    END
    DROP TABLE [AppCustomerSavedAddresses];
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[AppSavedAddresses]', N'U') IS NOT NULL
BEGIN
    DROP TABLE [AppSavedAddresses];
END
");
        }
    }
}
