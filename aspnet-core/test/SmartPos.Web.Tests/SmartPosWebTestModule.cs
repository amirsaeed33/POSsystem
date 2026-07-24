using Abp.AspNetCore;
using Abp.AspNetCore.TestBase;
using Abp.Modules;
using Abp.Reflection.Extensions;
using SmartPos.EntityFrameworkCore;
using SmartPos.Web.Startup;
using Microsoft.AspNetCore.Mvc.ApplicationParts;

namespace SmartPos.Web.Tests
{
    [DependsOn(
        typeof(SmartPosWebMvcModule),
        typeof(AbpAspNetCoreTestBaseModule)
    )]
    public class SmartPosWebTestModule : AbpModule
    {
        public SmartPosWebTestModule(SmartPosEntityFrameworkModule abpProjectNameEntityFrameworkModule)
        {
            abpProjectNameEntityFrameworkModule.SkipDbContextRegistration = true;
        } 
        
        public override void PreInitialize()
        {
            Configuration.UnitOfWork.IsTransactional = false; //EF Core InMemory DB does not support transactions.
        }

        public override void Initialize()
        {
            IocManager.RegisterAssemblyByConvention(typeof(SmartPosWebTestModule).GetAssembly());
        }
        
        public override void PostInitialize()
        {
            IocManager.Resolve<ApplicationPartManager>()
                .AddApplicationPartsIfNotAddedBefore(typeof(SmartPosWebMvcModule).Assembly);
        }
    }
}