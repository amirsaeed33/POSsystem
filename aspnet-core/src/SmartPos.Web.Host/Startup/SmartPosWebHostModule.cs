using System;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.EntityFrameworkCore.Uow;
using Abp.Modules;
using Abp.MultiTenancy;
using Abp.Reflection.Extensions;
using SmartPos.Branches;
using SmartPos.Configuration;
using SmartPos.EntityFrameworkCore;
using SmartPos.EntityFrameworkCore.Seed;

namespace SmartPos.Web.Host.Startup
{
    [DependsOn(
       typeof(SmartPosWebCoreModule))]
    public class SmartPosWebHostModule: AbpModule
    {
        private readonly IWebHostEnvironment _env;
        private readonly IConfigurationRoot _appConfiguration;

        public SmartPosWebHostModule(IWebHostEnvironment env)
        {
            _env = env;
            _appConfiguration = env.GetAppConfiguration();
        }

        public override void Initialize()
        {
            IocManager.RegisterAssemblyByConvention(typeof(SmartPosWebHostModule).GetAssembly());
        }

        public override void PostInitialize()
        {
            // Code First: apply pending EF Core migrations when the API starts.
            // Set "App:SkipDbMigration": true in appsettings to disable.
            var skip = _appConfiguration["App:SkipDbMigration"];
            if (!string.Equals(skip, "true", StringComparison.OrdinalIgnoreCase))
            {
                using (var migrator = IocManager.ResolveAsDisposable<AbpZeroDbMigrator>())
                {
                    migrator.Object.CreateOrMigrateForHost();
                }
            }
            else
            {
                EnsureUserPresenceTable();
                EnsureSubscriptionBillingTables();
            }

            var runDbSeed = string.Equals(
                _appConfiguration["App:RunDbSeed"],
                "true",
                StringComparison.OrdinalIgnoreCase);
            if (runDbSeed)
            {
                try
                {
                    SeedHelper.SeedHostDb(IocManager);
                }
                catch (Exception ex)
                {
                    // Already seeded or handled
                }
            }

            WarmStorefrontDomainRegistry();
        }

        /// <summary>
        /// Load active custom domains into memory for CORS / origin checks.
        /// </summary>
        private void WarmStorefrontDomainRegistry()
        {
            try
            {
                using (var uowManager = IocManager.ResolveAsDisposable<IUnitOfWorkManager>())
                using (var uow = uowManager.Object.Begin())
                using (uowManager.Object.Current.DisableFilter(
                           AbpDataFilters.MayHaveTenant,
                           AbpDataFilters.MustHaveTenant))
                using (var repo = IocManager.ResolveAsDisposable<IRepository<Branch>>())
                {
                    var domains = repo.Object.GetAll()
                        .AsNoTracking()
                        .Where(b => b.IsActive
                                    && b.CustomDomain != null
                                    && b.CustomDomain != string.Empty)
                        .Select(b => b.CustomDomain)
                        .ToList();
                    StorefrontDomainRegistry.ReplaceAll(domains);
                    uow.Complete();
                }
            }
            catch
            {
                // DB may be unavailable during first boot; registry fills on resolve/update.
            }
        }

        /// <summary>
        /// Production skips EF migrate-on-boot. Create the presence table when it is missing.
        /// </summary>
        private void EnsureUserPresenceTable()
        {
            try
            {
                using (var uowManager = IocManager.ResolveAsDisposable<IUnitOfWorkManager>())
                using (var uow = uowManager.Object.Begin())
                {
                    var context = uowManager.Object.Current.GetDbContext<SmartPosDbContext>(MultiTenancySides.Host);
                    context.Database.ExecuteSqlRaw(@"
IF OBJECT_ID(N'[dbo].[AppUserPresences]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AppUserPresences] (
        [Id] bigint NOT NULL IDENTITY,
        [TenantId] int NULL,
        [UserId] bigint NOT NULL,
        [BranchId] int NULL,
        [UserName] nvarchar(256) NULL,
        [FullName] nvarchar(256) NULL,
        [BranchName] nvarchar(256) NULL,
        [TenancyName] nvarchar(256) NULL,
        [RoleNames] nvarchar(256) NULL,
        [LoginTime] datetime2 NOT NULL,
        [LastSeen] datetime2 NOT NULL,
        [SignedOutTime] datetime2 NULL,
        CONSTRAINT [PK_AppUserPresences] PRIMARY KEY ([Id])
    );
    CREATE UNIQUE INDEX [IX_AppUserPresences_UserId] ON [dbo].[AppUserPresences] ([UserId]);
    CREATE INDEX [IX_AppUserPresences_TenantId_BranchId_LastSeen] ON [dbo].[AppUserPresences] ([TenantId], [BranchId], [LastSeen]);
END");
                    uow.Complete();
                }
            }
            catch
            {
                // Table is created by EF migrations when migrate-on-boot is enabled.
            }
        }

        /// <summary>
        /// Production skips EF migrate-on-boot. Create billing tables when they are missing.
        /// </summary>
        private void EnsureSubscriptionBillingTables()
        {
            try
            {
                using (var uowManager = IocManager.ResolveAsDisposable<IUnitOfWorkManager>())
                using (var uow = uowManager.Object.Begin())
                {
                    var context = uowManager.Object.Current.GetDbContext<SmartPosDbContext>(MultiTenancySides.Host);
                    context.Database.ExecuteSqlRaw(@"
IF OBJECT_ID(N'[dbo].[AppPaymentMethods]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AppPaymentMethods] (
        [Id] int NOT NULL IDENTITY,
        [Kind] int NOT NULL,
        [AccountTitle] nvarchar(128) NOT NULL,
        [AccountNumber] nvarchar(64) NOT NULL,
        [BankName] nvarchar(128) NULL,
        [Instructions] nvarchar(1000) NULL,
        [IsActive] bit NOT NULL,
        [QrImage] varbinary(max) NULL,
        [QrContentType] nvarchar(64) NULL,
        [CreationTime] datetime2 NOT NULL,
        [CreatorUserId] bigint NULL,
        [LastModificationTime] datetime2 NULL,
        [LastModifierUserId] bigint NULL,
        [IsDeleted] bit NOT NULL,
        [DeleterUserId] bigint NULL,
        [DeletionTime] datetime2 NULL,
        CONSTRAINT [PK_AppPaymentMethods] PRIMARY KEY ([Id])
    );
    CREATE UNIQUE INDEX [IX_AppPaymentMethods_Kind] ON [dbo].[AppPaymentMethods] ([Kind]) WHERE [IsDeleted] = 0;
END
IF OBJECT_ID(N'[dbo].[AppTenantSubscriptions]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AppTenantSubscriptions] (
        [Id] int NOT NULL IDENTITY,
        [TenantId] int NOT NULL,
        [PackageId] int NULL,
        [PlanName] nvarchar(128) NOT NULL,
        [Cycle] int NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [PaidUntil] datetime2 NULL,
        [CreationTime] datetime2 NOT NULL,
        [CreatorUserId] bigint NULL,
        [LastModificationTime] datetime2 NULL,
        [LastModifierUserId] bigint NULL,
        [IsDeleted] bit NOT NULL,
        [DeleterUserId] bigint NULL,
        [DeletionTime] datetime2 NULL,
        CONSTRAINT [PK_AppTenantSubscriptions] PRIMARY KEY ([Id])
    );
    CREATE UNIQUE INDEX [IX_AppTenantSubscriptions_TenantId] ON [dbo].[AppTenantSubscriptions] ([TenantId]) WHERE [IsDeleted] = 0;
END
IF OBJECT_ID(N'[dbo].[AppSubscriptionPayments]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AppSubscriptionPayments] (
        [Id] int NOT NULL IDENTITY,
        [TenantId] int NOT NULL,
        [PackageId] int NULL,
        [PlanName] nvarchar(128) NOT NULL,
        [Cycle] int NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [Method] int NOT NULL,
        [PaymentMethodId] int NULL,
        [Status] int NOT NULL,
        [ReceiptImage] varbinary(max) NULL,
        [ReceiptContentType] nvarchar(64) NULL,
        [ReceiptFileName] nvarchar(256) NULL,
        [ReviewNote] nvarchar(1000) NULL,
        [ReviewedTime] datetime2 NULL,
        [ReviewedByUserId] bigint NULL,
        [ApprovalToken] nvarchar(64) NULL,
        [CreationTime] datetime2 NOT NULL,
        [CreatorUserId] bigint NULL,
        [LastModificationTime] datetime2 NULL,
        [LastModifierUserId] bigint NULL,
        [IsDeleted] bit NOT NULL,
        [DeleterUserId] bigint NULL,
        [DeletionTime] datetime2 NULL,
        CONSTRAINT [PK_AppSubscriptionPayments] PRIMARY KEY ([Id])
    );
    CREATE INDEX [IX_AppSubscriptionPayments_TenantId_Status] ON [dbo].[AppSubscriptionPayments] ([TenantId], [Status]);
END
IF COL_LENGTH(N'dbo.AppSubscriptionPayments', N'ApprovalToken') IS NULL
    ALTER TABLE [dbo].[AppSubscriptionPayments] ADD [ApprovalToken] nvarchar(64) NULL;
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_AppSubscriptionPayments_ApprovalToken'
      AND object_id = OBJECT_ID(N'dbo.AppSubscriptionPayments'))
    CREATE UNIQUE INDEX [IX_AppSubscriptionPayments_ApprovalToken]
        ON [dbo].[AppSubscriptionPayments] ([ApprovalToken])
        WHERE [ApprovalToken] IS NOT NULL AND [IsDeleted] = 0;
");
                    uow.Complete();
                }
            }
            catch
            {
                // Tables are created by EF migrations when migrate-on-boot is enabled.
            }
        }
    }
}
