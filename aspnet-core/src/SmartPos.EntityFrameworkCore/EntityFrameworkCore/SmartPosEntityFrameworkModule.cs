using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Abp.EntityFrameworkCore.Configuration;
using Abp.Modules;
using Abp.Reflection.Extensions;
using Abp.Zero.EntityFrameworkCore;
using SmartPos.EntityFrameworkCore.Seed;

namespace SmartPos.EntityFrameworkCore
{
    [DependsOn(
        typeof(SmartPosCoreModule), 
        typeof(AbpZeroCoreEntityFrameworkCoreModule))]
    public class SmartPosEntityFrameworkModule : AbpModule
    {
        /* Used it tests to skip dbcontext registration, in order to use in-memory database of EF Core */
        public bool SkipDbContextRegistration { get; set; }

        public bool SkipDbSeed { get; set; }

        public override void PreInitialize()
        {
            if (!SkipDbContextRegistration)
            {
                Configuration.Modules.AbpEfCore().AddDbContext<SmartPosDbContext>(options =>
                {
                    if (options.ExistingConnection != null)
                    {
                        SmartPosDbContextConfigurer.Configure(options.DbContextOptions, options.ExistingConnection);
                    }
                    else
                    {
                        SmartPosDbContextConfigurer.Configure(options.DbContextOptions, options.ConnectionString);
                    }
                });
            }
        }

        public override void Initialize()
        {
            IocManager.RegisterAssemblyByConvention(typeof(SmartPosEntityFrameworkModule).GetAssembly());
        }

        public override void PostInitialize()
        {
            // If SkipDbSeed is true, still ensure essential host data (admin user, roles, languages)
            // is seeded if this is a newly created database with no host admin.
            if (SkipDbSeed)
            {
                EnsureEssentialSeed();
                return;
            }

            SeedHelper.SeedHostDb(IocManager);
        }

        private void EnsureEssentialSeed()
        {
            try
            {
                SeedHelper.WithDbContext<SmartPosDbContext>(IocManager, context =>
                {
                    var hasAdmin = context.Users.IgnoreQueryFilters().Any(u => u.TenantId == null && u.UserName == "admin");
                    if (!hasAdmin)
                    {
                        SeedHelper.SeedHostDb(context);
                    }
                });
            }
            catch (Exception ex)
            {
                Logger.Warn("EnsureEssentialSeed exception: " + ex.Message);
            }
        }
    }
}
