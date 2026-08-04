using Abp.Authorization;
using Abp.Localization;
using Abp.MultiTenancy;

namespace SmartPos.Authorization
{
    public class SmartPosAuthorizationProvider : AuthorizationProvider
    {
        public override void SetPermissions(IPermissionDefinitionContext context)
        {
            context.CreatePermission(PermissionNames.Pages_Users, L("Users"));
            context.CreatePermission(PermissionNames.Pages_Users_Activation, L("UsersActivation"));
            context.CreatePermission(PermissionNames.Pages_Roles, L("Roles"));
            context.CreatePermission(PermissionNames.Pages_Categories, L("Categories"));
            context.CreatePermission(PermissionNames.Pages_Brands, L("Brands"));
            context.CreatePermission(PermissionNames.Pages_CompanyProfiles, L("CompanyProfiles"));
            context.CreatePermission(PermissionNames.Pages_Branches, L("Branches"));
            context.CreatePermission(PermissionNames.Pages_Units, L("Units"));
            context.CreatePermission(PermissionNames.Pages_Products, L("Products"));
            context.CreatePermission(PermissionNames.Pages_Customers, L("Customers"));
            context.CreatePermission(PermissionNames.Pages_Staff, L("Staff"));

            var staffAttendance = context.CreatePermission(PermissionNames.Pages_StaffAttendance, L("StaffAttendance"));
            staffAttendance.CreateChildPermission(PermissionNames.Pages_StaffAttendance_Create, L("CreateStaffAttendance"));
            staffAttendance.CreateChildPermission(PermissionNames.Pages_StaffAttendance_Edit, L("EditStaffAttendance"));
            staffAttendance.CreateChildPermission(PermissionNames.Pages_StaffAttendance_Delete, L("DeleteStaffAttendance"));

            var staffPayroll = context.CreatePermission(PermissionNames.Pages_StaffPayroll, L("StaffPayroll"));
            staffPayroll.CreateChildPermission(PermissionNames.Pages_StaffPayroll_Create, L("CreateStaffPayroll"));
            staffPayroll.CreateChildPermission(PermissionNames.Pages_StaffPayroll_Edit, L("EditStaffPayroll"));
            staffPayroll.CreateChildPermission(PermissionNames.Pages_StaffPayroll_Delete, L("DeleteStaffPayroll"));

            context.CreatePermission(PermissionNames.Pages_Suppliers, L("Suppliers"));
            context.CreatePermission(PermissionNames.Pages_Accounts, L("Accounts"));
            context.CreatePermission(PermissionNames.Pages_LedgerEntries, L("LedgerEntries"));
            context.CreatePermission(PermissionNames.Pages_Purchases, L("Purchases"));
            context.CreatePermission(PermissionNames.Pages_Sales, L("Sales"));
            context.CreatePermission(PermissionNames.Pages_StockAdjustments, L("StockAdjustments"));
            context.CreatePermission(PermissionNames.Pages_Expenses, L("Expenses"));
            context.CreatePermission(PermissionNames.Pages_Reports, L("Reports"));
            context.CreatePermission(PermissionNames.Pages_CustomerOrders, L("CustomerOrders"));
            context.CreatePermission(PermissionNames.Pages_EmailTemplates, L("EmailTemplates"));
            context.CreatePermission(PermissionNames.Pages_Tenants, L("Tenants"), multiTenancySides: MultiTenancySides.Host);
        }

        private static ILocalizableString L(string name)
        {
            return new LocalizableString(name, SmartPosConsts.LocalizationSourceName);
        }
    }
}
