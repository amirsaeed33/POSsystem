using System;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Modules;
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
    }
}
