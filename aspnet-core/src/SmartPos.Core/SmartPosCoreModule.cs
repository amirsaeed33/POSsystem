using Abp.Localization;
using Abp.Modules;
using Abp.Reflection.Extensions;
using Abp.Runtime.Security;
using Abp.Timing;
using Abp.Zero;
using Abp.Zero.Configuration;
using SmartPos.Authorization.Roles;
using SmartPos.Authorization.Users;
using SmartPos.Configuration;
using SmartPos.Localization;
using SmartPos.MultiTenancy;
using SmartPos.Timing;

namespace SmartPos
{
    [DependsOn(typeof(AbpZeroCoreModule))]
    public class SmartPosCoreModule : AbpModule
    {
        public override void PreInitialize()
        {
            // Store/serialize timestamps as UTC so Angular date pipes convert to local correctly.
            Clock.Provider = ClockProviders.Utc;

            Configuration.Auditing.IsEnabledForAnonymousUsers = true;

            // Declare entity types
            Configuration.Modules.Zero().EntityTypes.Tenant = typeof(Tenant);
            Configuration.Modules.Zero().EntityTypes.Role = typeof(Role);
            Configuration.Modules.Zero().EntityTypes.User = typeof(User);

            SmartPosLocalizationConfigurer.Configure(Configuration.Localization);

            // Enable this line to create a multi-tenant application.
            Configuration.MultiTenancy.IsEnabled = SmartPosConsts.MultiTenancyEnabled;

            // Configure roles
            AppRoleConfig.Configure(Configuration.Modules.Zero().RoleManagement);

            Configuration.Settings.Providers.Add<AppSettingProvider>();
            
            Configuration.Localization.Languages.Add(new LanguageInfo("en", "English", "famfamfam-flags us", isDefault: true));
            Configuration.Localization.Languages.Add(new LanguageInfo("ar", "العربية", "famfamfam-flags sa"));
            Configuration.Localization.Languages.Add(new LanguageInfo("de", "German", "famfamfam-flags de"));
            Configuration.Localization.Languages.Add(new LanguageInfo("it", "Italiano", "famfamfam-flags it"));
            Configuration.Localization.Languages.Add(new LanguageInfo("fa", "فارسی", "famfamfam-flags ir"));
            Configuration.Localization.Languages.Add(new LanguageInfo("fr", "Français", "famfamfam-flags fr"));
            Configuration.Localization.Languages.Add(new LanguageInfo("pt-BR", "Português", "famfamfam-flags br"));
            Configuration.Localization.Languages.Add(new LanguageInfo("tr", "Türkçe", "famfamfam-flags tr"));
            Configuration.Localization.Languages.Add(new LanguageInfo("ru", "Русский", "famfamfam-flags ru"));
            Configuration.Localization.Languages.Add(new LanguageInfo("zh-Hans", "简体中文", "famfamfam-flags cn"));
            Configuration.Localization.Languages.Add(new LanguageInfo("es-MX", "Español México", "famfamfam-flags mx"));
            Configuration.Localization.Languages.Add(new LanguageInfo("nl", "Nederlands", "famfamfam-flags nl"));
            Configuration.Localization.Languages.Add(new LanguageInfo("ja", "日本語", "famfamfam-flags jp"));
            
            Configuration.Settings.SettingEncryptionConfiguration.DefaultPassPhrase = SmartPosConsts.DefaultPassPhrase;
            SimpleStringCipher.DefaultPassPhrase = SmartPosConsts.DefaultPassPhrase;
        }

        public override void Initialize()
        {
            IocManager.RegisterAssemblyByConvention(typeof(SmartPosCoreModule).GetAssembly());
        }

        public override void PostInitialize()
        {
            IocManager.Resolve<AppTimes>().StartupTime = Clock.Now;
        }
    }
}
