using System;
using System.Linq;
using Abp.EntityFrameworkCore.Configuration;
using Abp.Modules;
using Abp.Reflection.Extensions;
using Abp.Zero.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SmartPos.EntityFrameworkCore.Seed;
using SmartPos.EntityFrameworkCore.Seed.Host;
using SmartPos.EntityFrameworkCore.Seed.Tenants;

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
                    // Full seed stays off, but host Admin must pick up new host permissions
                    // (product pricing, sellers, developers) without recreating tenants.
                    new HostRoleAndUserCreator(context).Create();

                    // Existing business admins pick up new tenant permissions such as Billing.
                    var tenantIds = context.Tenants.IgnoreQueryFilters().Select(t => t.Id).ToList();
                    foreach (var tenantId in tenantIds)
                    {
                        new TenantRoleAndUserBuilder(context, tenantId).Create(false);
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
