using Abp.Configuration.Startup;
using Abp.Localization.Dictionaries;
using Abp.Localization.Dictionaries.Xml;
using Abp.Reflection.Extensions;

namespace SmartPos.Localization
{
    public static class SmartPosLocalizationConfigurer
    {
        public static void Configure(ILocalizationConfiguration localizationConfiguration)
        {
            localizationConfiguration.Sources.Add(
                new DictionaryBasedLocalizationSource(SmartPosConsts.LocalizationSourceName,
                    new XmlEmbeddedFileLocalizationDictionaryProvider(
                        typeof(SmartPosLocalizationConfigurer).GetAssembly(),
                        "SmartPos.Localization.SourceFiles"
                    )
                )
            );
        }
    }
}
