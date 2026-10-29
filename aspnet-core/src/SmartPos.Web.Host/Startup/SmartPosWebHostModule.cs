using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Abp.Dependency;
using Abp.Modules;
using Abp.Reflection.Extensions;
using SmartPos.Configuration;
using SmartPos.EntityFrameworkCore;

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
            if (string.Equals(skip, "true", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            using (var migrator = IocManager.ResolveAsDisposable<AbpZeroDbMigrator>())
            {
                migrator.Object.CreateOrMigrateForHost();
            }
        }
    }
}
