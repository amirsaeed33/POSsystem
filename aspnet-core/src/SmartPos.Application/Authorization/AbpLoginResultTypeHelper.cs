using System;
using Abp;
using Abp.Authorization;
using Abp.Dependency;
using Abp.UI;

namespace SmartPos.Authorization
{
    public class AbpLoginResultTypeHelper : AbpServiceBase, ITransientDependency
    {
        public AbpLoginResultTypeHelper()
        {
            LocalizationSourceName = SmartPosConsts.LocalizationSourceName;
        }

        private string SafeL(string name, string fallback, params object[] args)
        {
            try
            {
                var text = args != null && args.Length > 0 ? L(name, args) : L(name);
                if (!string.IsNullOrWhiteSpace(text)) return text;
            }
            catch
            {
                // Localization dictionary fallback
            }
            return args != null && args.Length > 0 ? string.Format(fallback, args) : fallback;
        }

        public Exception CreateExceptionForFailedLoginAttempt(AbpLoginResultType result, string usernameOrEmailAddress, string tenancyName)
        {
            switch (result)
            {
                case AbpLoginResultType.Success:
                    return new Exception("Don't call this method with a success result!");
                case AbpLoginResultType.InvalidUserNameOrEmailAddress:
                case AbpLoginResultType.InvalidPassword:
                    return new UserFriendlyException(
                        SafeL("LoginFailed", "Login failed"),
                        SafeL("InvalidUserNameOrPassword", "Invalid user name or password")
                    );
                case AbpLoginResultType.InvalidTenancyName:
                    return new UserFriendlyException(
                        SafeL("LoginFailed", "Login failed"),
                        SafeL("ThereIsNoTenantDefinedWithName{0}", "There is no tenant defined with name {0}", tenancyName)
                    );
                case AbpLoginResultType.TenantIsNotActive:
                    return new UserFriendlyException(
                        SafeL("LoginFailed", "Login failed"),
                        SafeL("TenantIsNotActive", "Tenant {0} is not active.", tenancyName)
                    );
                case AbpLoginResultType.UserIsNotActive:
                    return new UserFriendlyException(
                        SafeL("LoginFailed", "Login failed"),
                        SafeL("UserIsNotActiveAndCanNotLogin", "User {0} is not active and can not login.", usernameOrEmailAddress)
                    );
                case AbpLoginResultType.UserEmailIsNotConfirmed:
                    return new UserFriendlyException(
                        SafeL("LoginFailed", "Login failed"),
                        SafeL("UserEmailIsNotConfirmedAndCanNotLogin", "User email is not confirmed and cannot login.")
                    );
                case AbpLoginResultType.LockedOut:
                    return new UserFriendlyException(
                        SafeL("LoginFailed", "Login failed"),
                        SafeL("UserLockedOutMessage", "Your account is locked out.")
                    );
                default: // Can not fall to default actually. But other result types can be added in the future and we may forget to handle it
                    Logger.Warn("Unhandled login fail reason: " + result);
                    return new UserFriendlyException(SafeL("LoginFailed", "Login failed"));
            }
        }

        public string CreateLocalizedMessageForFailedLoginAttempt(AbpLoginResultType result, string usernameOrEmailAddress, string tenancyName)
        {
            switch (result)
            {
                case AbpLoginResultType.Success:
                    throw new Exception("Don't call this method with a success result!");
                case AbpLoginResultType.InvalidUserNameOrEmailAddress:
                case AbpLoginResultType.InvalidPassword:
                    return SafeL("InvalidUserNameOrPassword", "Invalid user name or password");
                case AbpLoginResultType.InvalidTenancyName:
                    return SafeL("ThereIsNoTenantDefinedWithName{0}", "There is no tenant defined with name {0}", tenancyName);
                case AbpLoginResultType.TenantIsNotActive:
                    return SafeL("TenantIsNotActive", "Tenant {0} is not active.", tenancyName);
                case AbpLoginResultType.UserIsNotActive:
                    return SafeL("UserIsNotActiveAndCanNotLogin", "User {0} is not active and can not login.", usernameOrEmailAddress);
                case AbpLoginResultType.UserEmailIsNotConfirmed:
                    return SafeL("UserEmailIsNotConfirmedAndCanNotLogin", "User email is not confirmed and cannot login.");
                default: // Can not fall to default actually. But other result types can be added in the future and we may forget to handle it
                    Logger.Warn("Unhandled login fail reason: " + result);
                    return SafeL("LoginFailed", "Login failed");
            }
        }
    }
}
