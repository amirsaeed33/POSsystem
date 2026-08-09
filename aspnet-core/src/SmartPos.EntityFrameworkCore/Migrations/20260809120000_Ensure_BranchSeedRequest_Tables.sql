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
GO

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
GO

IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = N'20260809120000_Ensure_BranchSeedRequest_Tables')
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES (N'20260809120000_Ensure_BranchSeedRequest_Tables', N'8.0.0');
GO
