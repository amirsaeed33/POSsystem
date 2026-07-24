using Abp.AutoMapper;
using Abp.Modules;
using Abp.Reflection.Extensions;
using SmartPos.Authorization;

namespace SmartPos
{
    [DependsOn(
        typeof(SmartPosCoreModule), 
        typeof(AbpAutoMapperModule))]
    public class SmartPosApplicationModule : AbpModule
    {
        public override void PreInitialize()
        {
            Configuration.Authorization.Providers.Add<SmartPosAuthorizationProvider>();
        }

        public override void Initialize()
        {
            var thisAssembly = typeof(SmartPosApplicationModule).GetAssembly();

            IocManager.RegisterAssemblyByConvention(thisAssembly);

            Configuration.Modules.AbpAutoMapper().Configurators.Add(
                // Scan the assembly for classes which inherit from AutoMapper.Profile
                cfg => cfg.AddMaps(thisAssembly)
            );
        }
    }
}
