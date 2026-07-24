using Abp.AspNetCore.Mvc.Controllers;
using Abp.IdentityFramework;
using Microsoft.AspNetCore.Identity;

namespace SmartPos.Controllers
{
    public abstract class SmartPosControllerBase: AbpController
    {
        protected SmartPosControllerBase()
        {
            LocalizationSourceName = SmartPosConsts.LocalizationSourceName;
        }

        protected void CheckErrors(IdentityResult identityResult)
        {
            identityResult.CheckErrors(LocalizationManager);
        }
    }
}
