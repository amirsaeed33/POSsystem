using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartPos.Migrations
{
    /// <summary>
    /// Prefer applying the companion .sql script with sqlcmd (GO batches + QUOTED_IDENTIFIER).
    /// This EF migration records history; Up is idempotent for environments that run Migrator.
    /// </summary>
    [Migration("20260809100000_Added_HostCatalog_And_BranchScopedCatalog")]
    public partial class Added_HostCatalog_And_BranchScopedCatalog : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Schema is applied via 20260809100000_Added_HostCatalog_And_BranchScopedCatalog.sql
            // when using sqlcmd. Migrator may already have applied history from that script.
            // Idempotent core tables. Full catalog column alters are in the companion .sql /
            // Ensure_BranchSeedRequest_Tables migration for environments that used sqlcmd.
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'AppHostCatalogItems', N'U') IS NULL
BEGIN
    CREATE TABLE AppHostCatalogItems (
        Id int IDENTITY(1,1) NOT NULL,
        Type nvarchar(32) NOT NULL,
        CompanyTypeId int NULL,
        Name nvarchar(128) NOT NULL,
        Symbol nvarchar(32) NULL,
        IsActive bit NOT NULL CONSTRAINT DF_AppHostCatalogItems_IsActive DEFAULT(1),
        CreationTime datetime2 NOT NULL,
        CreatorUserId bigint NULL,
        LastModificationTime datetime2 NULL,
        LastModifierUserId bigint NULL,
        IsDeleted bit NOT NULL CONSTRAINT DF_AppHostCatalogItems_IsDeleted DEFAULT(0),
        DeleterUserId bigint NULL,
        DeletionTime datetime2 NULL,
        CONSTRAINT PK_AppHostCatalogItems PRIMARY KEY (Id)
    );
END

IF OBJECT_ID(N'AppBranchSeedRequests', N'U') IS NULL
BEGIN
    CREATE TABLE AppBranchSeedRequests (
        Id int IDENTITY(1,1) NOT NULL,
        TenantId int NOT NULL,
        BranchId int NOT NULL,
        RequestedByUserId bigint NOT NULL,
        CompanyTypeId int NOT NULL,
        Status nvarchar(32) NOT NULL,
        ApprovedByUserId bigint NULL,
        ApprovedDate datetime2 NULL,
        CreationTime datetime2 NOT NULL,
        CreatorUserId bigint NULL,
        LastModificationTime datetime2 NULL,
        LastModifierUserId bigint NULL,
        IsDeleted bit NOT NULL CONSTRAINT DF_AppBranchSeedRequests_IsDeleted DEFAULT(0),
        DeleterUserId bigint NULL,
        DeletionTime datetime2 NULL,
        CONSTRAINT PK_AppBranchSeedRequests PRIMARY KEY (Id)
    );
END

IF OBJECT_ID(N'AppBranchSeedRequestItems', N'U') IS NULL
BEGIN
    CREATE TABLE AppBranchSeedRequestItems (
        Id int IDENTITY(1,1) NOT NULL,
        BranchSeedRequestId int NOT NULL,
        HostItemId int NOT NULL,
        CreationTime datetime2 NOT NULL,
        CreatorUserId bigint NULL,
        CONSTRAINT PK_AppBranchSeedRequestItems PRIMARY KEY (Id)
    );
END
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'AppBranchSeedRequestItems', N'U') IS NOT NULL DROP TABLE AppBranchSeedRequestItems;
IF OBJECT_ID(N'AppBranchSeedRequests', N'U') IS NOT NULL DROP TABLE AppBranchSeedRequests;
IF OBJECT_ID(N'AppHostCatalogItems', N'U') IS NOT NULL DROP TABLE AppHostCatalogItems;
");
        }
    }
}
