using System.Threading.Tasks;
using SmartPos.Configuration.Dto;

namespace SmartPos.Configuration
{
    public interface IConfigurationAppService
    {
        Task ChangeUiTheme(ChangeUiThemeInput input);
    }
}
