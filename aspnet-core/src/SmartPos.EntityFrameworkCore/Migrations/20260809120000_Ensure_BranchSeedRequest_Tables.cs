using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartPos.Migrations
{
    /// <summary>
    /// Ensures seed-request tables exist when the earlier HostCatalog EF Up() only created AppHostCatalogItems.
    /// Idempotent — safe if the companion .sql script already applied full schema.
    /// </summary>
    [Migration("20260809120000_Ensure_BranchSeedRequest_Tables")]
    public partial class Ensure_BranchSeedRequest_Tables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
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
        CONSTRAINT PK_AppBranchSeedRequests PRIMARY KEY (Id),
        CONSTRAINT FK_AppBranchSeedRequests_Branch FOREIGN KEY (BranchId)
            REFERENCES AppBranches(Id) ON DELETE NO ACTION,
        CONSTRAINT FK_AppBranchSeedRequests_CompanyType FOREIGN KEY (CompanyTypeId)
            REFERENCES AppHostCatalogItems(Id) ON DELETE NO ACTION
    );
    CREATE INDEX IX_AppBranchSeedRequests_Tenant_Branch_Status
        ON AppBranchSeedRequests(TenantId, BranchId, Status);
END

IF OBJECT_ID(N'AppBranchSeedRequestItems', N'U') IS NULL
BEGIN
    CREATE TABLE AppBranchSeedRequestItems (
        Id int IDENTITY(1,1) NOT NULL,
        BranchSeedRequestId int NOT NULL,
        HostItemId int NOT NULL,
        CreationTime datetime2 NOT NULL,
        CreatorUserId bigint NULL,
        CONSTRAINT PK_AppBranchSeedRequestItems PRIMARY KEY (Id),
        CONSTRAINT FK_AppBranchSeedRequestItems_Request FOREIGN KEY (BranchSeedRequestId)
            REFERENCES AppBranchSeedRequests(Id) ON DELETE CASCADE,
        CONSTRAINT FK_AppBranchSeedRequestItems_HostItem FOREIGN KEY (HostItemId)
            REFERENCES AppHostCatalogItems(Id) ON DELETE NO ACTION
    );
    CREATE UNIQUE INDEX IX_AppBranchSeedRequestItems_Request_HostItem
        ON AppBranchSeedRequestItems(BranchSeedRequestId, HostItemId);
END
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally empty — do not drop seed tables that may still be in use.
        }
    }
}
