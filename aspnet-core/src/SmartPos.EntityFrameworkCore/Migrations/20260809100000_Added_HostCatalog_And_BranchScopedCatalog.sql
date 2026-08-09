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
        CONSTRAINT PK_AppHostCatalogItems PRIMARY KEY (Id),
        CONSTRAINT FK_AppHostCatalogItems_CompanyType FOREIGN KEY (CompanyTypeId)
            REFERENCES AppHostCatalogItems(Id) ON DELETE NO ACTION
    );
    CREATE UNIQUE INDEX IX_AppHostCatalogItems_Type_CompanyTypeId_Name
        ON AppHostCatalogItems(Type, CompanyTypeId, Name)
        WHERE IsDeleted = 0;
    CREATE INDEX IX_AppHostCatalogItems_Type_CompanyTypeId_IsActive
        ON AppHostCatalogItems(Type, CompanyTypeId, IsActive);
END
GO

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

IF COL_LENGTH('AppCategories', 'BranchId') IS NULL
BEGIN
    ALTER TABLE AppCategories ADD BranchId int NULL;
    ALTER TABLE AppCategories ADD IsActive bit NOT NULL CONSTRAINT DF_AppCategories_IsActive DEFAULT(1);
    ALTER TABLE AppCategories ADD HostSourceId int NULL;
END
GO

IF COL_LENGTH('AppCategories', 'BranchId') IS NOT NULL
BEGIN
    UPDATE c SET c.BranchId = b.Id
    FROM AppCategories c
    OUTER APPLY (
        SELECT TOP 1 Id FROM AppBranches
        WHERE IsDeleted = 0
          AND ((TenantId = c.TenantId) OR (TenantId IS NULL AND c.TenantId IS NULL))
        ORDER BY CASE WHEN IsDefault = 1 THEN 0 ELSE 1 END, Id
    ) b
    WHERE c.BranchId IS NULL;

    DELETE FROM AppCategories WHERE BranchId IS NULL AND IsDeleted = 0;
END
GO

IF COL_LENGTH('AppCategories', 'BranchId') IS NOT NULL
   AND EXISTS (SELECT 1 FROM AppCategories WHERE BranchId IS NULL)
BEGIN
    -- orphan soft-deleted rows without branch
    UPDATE AppCategories SET BranchId = (SELECT TOP 1 Id FROM AppBranches WHERE IsDeleted = 0 ORDER BY Id)
    WHERE BranchId IS NULL;
END
GO

IF COL_LENGTH('AppCategories', 'BranchId') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'AppCategories') AND name = N'BranchId' AND is_nullable = 0)
BEGIN
    ALTER TABLE AppCategories ALTER COLUMN BranchId int NOT NULL;
END
GO

IF COL_LENGTH('AppCategories', 'BranchId') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AppCategories_AppBranches_BranchId')
    ALTER TABLE AppCategories ADD CONSTRAINT FK_AppCategories_AppBranches_BranchId
        FOREIGN KEY (BranchId) REFERENCES AppBranches(Id) ON DELETE NO ACTION;
GO

IF COL_LENGTH('AppCategories', 'BranchId') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AppCategories_TenantId_BranchId_Name' AND object_id = OBJECT_ID(N'AppCategories'))
    CREATE UNIQUE INDEX IX_AppCategories_TenantId_BranchId_Name
        ON AppCategories(TenantId, BranchId, Name) WHERE IsDeleted = 0;
GO

IF COL_LENGTH('AppCategories', 'BranchId') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AppCategories_BranchId' AND object_id = OBJECT_ID(N'AppCategories'))
    CREATE INDEX IX_AppCategories_BranchId ON AppCategories(BranchId);
GO

IF COL_LENGTH('AppBrands', 'BranchId') IS NULL
BEGIN
    ALTER TABLE AppBrands ADD BranchId int NULL;
    ALTER TABLE AppBrands ADD IsActive bit NOT NULL CONSTRAINT DF_AppBrands_IsActive DEFAULT(1);
    ALTER TABLE AppBrands ADD HostSourceId int NULL;
END
GO

IF COL_LENGTH('AppBrands', 'BranchId') IS NOT NULL
BEGIN
    UPDATE br SET br.BranchId = b.Id
    FROM AppBrands br
    OUTER APPLY (
        SELECT TOP 1 Id FROM AppBranches
        WHERE IsDeleted = 0
          AND ((TenantId = br.TenantId) OR (TenantId IS NULL AND br.TenantId IS NULL))
        ORDER BY CASE WHEN IsDefault = 1 THEN 0 ELSE 1 END, Id
    ) b
    WHERE br.BranchId IS NULL;

    DELETE FROM AppBrands WHERE BranchId IS NULL AND IsDeleted = 0;
END
GO

IF COL_LENGTH('AppBrands', 'BranchId') IS NOT NULL
   AND EXISTS (SELECT 1 FROM AppBrands WHERE BranchId IS NULL)
BEGIN
    UPDATE AppBrands SET BranchId = (SELECT TOP 1 Id FROM AppBranches WHERE IsDeleted = 0 ORDER BY Id)
    WHERE BranchId IS NULL;
END
GO

IF COL_LENGTH('AppBrands', 'BranchId') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'AppBrands') AND name = N'BranchId' AND is_nullable = 0)
    ALTER TABLE AppBrands ALTER COLUMN BranchId int NOT NULL;
GO

IF COL_LENGTH('AppBrands', 'BranchId') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AppBrands_AppBranches_BranchId')
    ALTER TABLE AppBrands ADD CONSTRAINT FK_AppBrands_AppBranches_BranchId
        FOREIGN KEY (BranchId) REFERENCES AppBranches(Id) ON DELETE NO ACTION;
GO

IF COL_LENGTH('AppBrands', 'BranchId') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AppBrands_TenantId_BranchId_Name' AND object_id = OBJECT_ID(N'AppBrands'))
    CREATE UNIQUE INDEX IX_AppBrands_TenantId_BranchId_Name
        ON AppBrands(TenantId, BranchId, Name) WHERE IsDeleted = 0;
GO

IF COL_LENGTH('AppBrands', 'BranchId') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AppBrands_BranchId' AND object_id = OBJECT_ID(N'AppBrands'))
    CREATE INDEX IX_AppBrands_BranchId ON AppBrands(BranchId);
GO

IF COL_LENGTH('AppUnits', 'BranchId') IS NULL
BEGIN
    ALTER TABLE AppUnits ADD BranchId int NULL;
    ALTER TABLE AppUnits ADD Symbol nvarchar(32) NULL;
    ALTER TABLE AppUnits ADD IsActive bit NOT NULL CONSTRAINT DF_AppUnits_IsActive DEFAULT(1);
    ALTER TABLE AppUnits ADD HostSourceId int NULL;
END
GO

IF COL_LENGTH('AppUnits', 'BranchId') IS NOT NULL
BEGIN
    UPDATE u SET u.BranchId = b.Id
    FROM AppUnits u
    OUTER APPLY (
        SELECT TOP 1 Id FROM AppBranches
        WHERE IsDeleted = 0
          AND ((TenantId = u.TenantId) OR (TenantId IS NULL AND u.TenantId IS NULL))
        ORDER BY CASE WHEN IsDefault = 1 THEN 0 ELSE 1 END, Id
    ) b
    WHERE u.BranchId IS NULL;

    DELETE FROM AppUnits WHERE BranchId IS NULL AND IsDeleted = 0;
END
GO

IF COL_LENGTH('AppUnits', 'BranchId') IS NOT NULL
   AND EXISTS (SELECT 1 FROM AppUnits WHERE BranchId IS NULL)
BEGIN
    UPDATE AppUnits SET BranchId = (SELECT TOP 1 Id FROM AppBranches WHERE IsDeleted = 0 ORDER BY Id)
    WHERE BranchId IS NULL;
END
GO

IF COL_LENGTH('AppUnits', 'BranchId') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'AppUnits') AND name = N'BranchId' AND is_nullable = 0)
    ALTER TABLE AppUnits ALTER COLUMN BranchId int NOT NULL;
GO

IF COL_LENGTH('AppUnits', 'BranchId') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AppUnits_AppBranches_BranchId')
    ALTER TABLE AppUnits ADD CONSTRAINT FK_AppUnits_AppBranches_BranchId
        FOREIGN KEY (BranchId) REFERENCES AppBranches(Id) ON DELETE NO ACTION;
GO

IF COL_LENGTH('AppUnits', 'BranchId') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AppUnits_TenantId_BranchId_Name' AND object_id = OBJECT_ID(N'AppUnits'))
    CREATE UNIQUE INDEX IX_AppUnits_TenantId_BranchId_Name
        ON AppUnits(TenantId, BranchId, Name) WHERE IsDeleted = 0;
GO

IF COL_LENGTH('AppUnits', 'BranchId') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AppUnits_BranchId' AND object_id = OBJECT_ID(N'AppUnits'))
    CREATE INDEX IX_AppUnits_BranchId ON AppUnits(BranchId);
GO

IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = N'20260809100000_Added_HostCatalog_And_BranchScopedCatalog')
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES (N'20260809100000_Added_HostCatalog_And_BranchScopedCatalog', N'8.0.0');
GO
