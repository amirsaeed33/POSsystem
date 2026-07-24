using System.Threading.Tasks;
using Abp.Authorization;
using Abp.Runtime.Session;
using SmartPos.Configuration.Dto;

namespace SmartPos.Configuration
{
    [AbpAuthorize]
    public class ConfigurationAppService : SmartPosAppServiceBase, IConfigurationAppService
    {
        public async Task ChangeUiTheme(ChangeUiThemeInput input)
        {
            await SettingManager.ChangeSettingForUserAsync(AbpSession.ToUserIdentifier(), AppSettingNames.UiTheme, input.Theme);
        }
    }
}
