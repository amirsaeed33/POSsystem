using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SmartPos.EntityFrameworkCore;

#nullable disable

namespace SmartPos.Migrations
{
    [DbContext(typeof(SmartPosDbContext))]
    [Migration("20261029130000_Added_SupplierProducts")]
    public partial class Added_SupplierProducts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
SET QUOTED_IDENTIFIER ON;
IF OBJECT_ID(N'[AppSupplierProducts]', N'U') IS NULL
BEGIN
    CREATE TABLE [AppSupplierProducts] (
        [Id] int NOT NULL IDENTITY,
        [TenantId] int NULL,
        [SupplierId] int NOT NULL,
        [ProductId] int NOT NULL,
        [CreationTime] datetime2 NOT NULL,
        [CreatorUserId] bigint NULL,
        [LastModificationTime] datetime2 NULL,
        [LastModifierUserId] bigint NULL,
        [IsDeleted] bit NOT NULL CONSTRAINT [DF_AppSupplierProducts_IsDeleted] DEFAULT CAST(0 AS bit),
        [DeleterUserId] bigint NULL,
        [DeletionTime] datetime2 NULL,
        CONSTRAINT [PK_AppSupplierProducts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AppSupplierProducts_AppSuppliers_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [AppSuppliers] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AppSupplierProducts_AppProducts_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [AppProducts] ([Id]) ON DELETE NO ACTION
    );

    CREATE UNIQUE INDEX [IX_AppSupplierProducts_TenantId_SupplierId_ProductId]
        ON [AppSupplierProducts] ([TenantId], [SupplierId], [ProductId])
        WHERE [IsDeleted] = 0;

    CREATE INDEX [IX_AppSupplierProducts_ProductId] ON [AppSupplierProducts] ([ProductId]);
    CREATE INDEX [IX_AppSupplierProducts_SupplierId] ON [AppSupplierProducts] ([SupplierId]);
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[AppSupplierProducts]', N'U') IS NOT NULL
BEGIN
    DROP TABLE [AppSupplierProducts];
END
");
        }
    }
}
