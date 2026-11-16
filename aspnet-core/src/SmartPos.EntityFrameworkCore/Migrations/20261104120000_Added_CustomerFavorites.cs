using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SmartPos.EntityFrameworkCore;

#nullable disable

namespace SmartPos.Migrations
{
    [DbContext(typeof(SmartPosDbContext))]
    [Migration("20261104120000_Added_CustomerFavorites")]
    public partial class Added_CustomerFavorites : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[AppCustomerFavorites]', N'U') IS NULL
BEGIN
    CREATE TABLE [AppCustomerFavorites] (
        [Id] int NOT NULL IDENTITY(1,1),
        [TenantId] int NULL,
        [CustomerId] int NOT NULL,
        [ProductId] int NOT NULL,
        [BranchId] int NOT NULL,
        [CreationTime] datetime2 NOT NULL,
        [CreatorUserId] bigint NULL,
        CONSTRAINT [PK_AppCustomerFavorites] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AppCustomerFavorites_AppCustomers_CustomerId]
            FOREIGN KEY ([CustomerId]) REFERENCES [AppCustomers]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AppCustomerFavorites_AppProducts_ProductId]
            FOREIGN KEY ([ProductId]) REFERENCES [AppProducts]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AppCustomerFavorites_AppBranches_BranchId]
            FOREIGN KEY ([BranchId]) REFERENCES [AppBranches]([Id]) ON DELETE CASCADE
    );

    CREATE UNIQUE INDEX [IX_AppCustomerFavorites_CustomerId_ProductId_BranchId]
        ON [AppCustomerFavorites]([CustomerId], [ProductId], [BranchId]);
    CREATE INDEX [IX_AppCustomerFavorites_ProductId]
        ON [AppCustomerFavorites]([ProductId]);
    CREATE INDEX [IX_AppCustomerFavorites_BranchId]
        ON [AppCustomerFavorites]([BranchId]);
    CREATE INDEX [IX_AppCustomerFavorites_TenantId]
        ON [AppCustomerFavorites]([TenantId]);
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[AppCustomerFavorites]', N'U') IS NOT NULL
BEGIN
    DROP TABLE [AppCustomerFavorites];
END
");
        }
    }
}
