IF COL_LENGTH('AppBranches', 'IsDefault') IS NOT NULL
    ALTER TABLE AppBranches DROP COLUMN IsDefault;
GO

IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = N'20260809130000_Removed_Branch_IsDefault')
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES (N'20260809130000_Removed_Branch_IsDefault', N'8.0.0');
GO
