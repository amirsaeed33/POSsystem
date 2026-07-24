using Abp.Authorization;
using SmartPos.Authorization.Roles;
using SmartPos.Authorization.Users;

namespace SmartPos.Authorization
{
    public class PermissionChecker : PermissionChecker<Role, User>
    {
        public PermissionChecker(UserManager userManager)
            : base(userManager)
        {
        }
    }
}
