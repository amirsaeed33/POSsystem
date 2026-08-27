using Abp.Authorization;
using Abp.Localization;
using Abp.MultiTenancy;

namespace SmartPos.Authorization
{
    public class SmartPosAuthorizationProvider : AuthorizationProvider
    {
        public override void SetPermissions(IPermissionDefinitionContext context)
        {
            var users = context.CreatePermission(PermissionNames.Pages_Users, L("Users"));
            users.CreateChildPermission(PermissionNames.Pages_Users_Create, L("CreateUser"));
            users.CreateChildPermission(PermissionNames.Pages_Users_Edit, L("EditUser"));
            users.CreateChildPermission(PermissionNames.Pages_Users_Delete, L("DeleteUser"));
            users.CreateChildPermission(PermissionNames.Pages_Users_Activation, L("UsersActivation"));

            var roles = context.CreatePermission(PermissionNames.Pages_Roles, L("Roles"));
            roles.CreateChildPermission(PermissionNames.Pages_Roles_Create, L("CreateRole"));
            roles.CreateChildPermission(PermissionNames.Pages_Roles_Edit, L("EditRole"));
            roles.CreateChildPermission(PermissionNames.Pages_Roles_Delete, L("DeleteRole"));

            var categories = context.CreatePermission(PermissionNames.Pages_Categories, L("Categories"));
            categories.CreateChildPermission(PermissionNames.Pages_Categories_Create, L("CreateCategory"));
            categories.CreateChildPermission(PermissionNames.Pages_Categories_Edit, L("EditCategory"));
            categories.CreateChildPermission(PermissionNames.Pages_Categories_Delete, L("DeleteCategory"));

            var brands = context.CreatePermission(PermissionNames.Pages_Brands, L("Brands"));
            brands.CreateChildPermission(PermissionNames.Pages_Brands_Create, L("CreateBrand"));
            brands.CreateChildPermission(PermissionNames.Pages_Brands_Edit, L("EditBrand"));
            brands.CreateChildPermission(PermissionNames.Pages_Brands_Delete, L("DeleteBrand"));

            var branches = context.CreatePermission(PermissionNames.Pages_Branches, L("Branches"));
            branches.CreateChildPermission(PermissionNames.Pages_Branches_Create, L("CreateBranch"));
            branches.CreateChildPermission(PermissionNames.Pages_Branches_Edit, L("EditBranch"));
            branches.CreateChildPermission(PermissionNames.Pages_Branches_Delete, L("DeleteBranch"));
            branches.CreateChildPermission(
                PermissionNames.Pages_Branches_Approve,
                L("ApproveBranches"),
                multiTenancySides: MultiTenancySides.Host);

            var units = context.CreatePermission(PermissionNames.Pages_Units, L("Units"));
            units.CreateChildPermission(PermissionNames.Pages_Units_Create, L("CreateUnit"));
            units.CreateChildPermission(PermissionNames.Pages_Units_Edit, L("EditUnit"));
            units.CreateChildPermission(PermissionNames.Pages_Units_Delete, L("DeleteUnit"));

            var products = context.CreatePermission(PermissionNames.Pages_Products, L("Products"));
            products.CreateChildPermission(PermissionNames.Pages_Products_Create, L("CreateProduct"));
            products.CreateChildPermission(PermissionNames.Pages_Products_Edit, L("EditProduct"));
            products.CreateChildPermission(PermissionNames.Pages_Products_Delete, L("DeleteProduct"));

            var customers = context.CreatePermission(PermissionNames.Pages_Customers, L("Customers"));
            customers.CreateChildPermission(PermissionNames.Pages_Customers_Create, L("CreateCustomer"));
            customers.CreateChildPermission(PermissionNames.Pages_Customers_Edit, L("EditCustomer"));
            customers.CreateChildPermission(PermissionNames.Pages_Customers_Delete, L("DeleteCustomer"));

            var staff = context.CreatePermission(PermissionNames.Pages_Staff, L("Staff"));
            staff.CreateChildPermission(PermissionNames.Pages_Staff_Create, L("CreateStaff"));
            staff.CreateChildPermission(PermissionNames.Pages_Staff_Edit, L("EditStaff"));
            staff.CreateChildPermission(PermissionNames.Pages_Staff_Delete, L("DeleteStaff"));

            var staffAttendance = context.CreatePermission(PermissionNames.Pages_StaffAttendance, L("StaffAttendance"));
            staffAttendance.CreateChildPermission(PermissionNames.Pages_StaffAttendance_Create, L("CreateStaffAttendance"));
            staffAttendance.CreateChildPermission(PermissionNames.Pages_StaffAttendance_Edit, L("EditStaffAttendance"));
            staffAttendance.CreateChildPermission(PermissionNames.Pages_StaffAttendance_Delete, L("DeleteStaffAttendance"));

            var staffPayroll = context.CreatePermission(PermissionNames.Pages_StaffPayroll, L("StaffPayroll"));
            staffPayroll.CreateChildPermission(PermissionNames.Pages_StaffPayroll_Create, L("CreateStaffPayroll"));
            staffPayroll.CreateChildPermission(PermissionNames.Pages_StaffPayroll_Edit, L("EditStaffPayroll"));
            staffPayroll.CreateChildPermission(PermissionNames.Pages_StaffPayroll_Delete, L("DeleteStaffPayroll"));

            var suppliers = context.CreatePermission(PermissionNames.Pages_Suppliers, L("Suppliers"));
            suppliers.CreateChildPermission(PermissionNames.Pages_Suppliers_Create, L("CreateSupplier"));
            suppliers.CreateChildPermission(PermissionNames.Pages_Suppliers_Edit, L("EditSupplier"));
            suppliers.CreateChildPermission(PermissionNames.Pages_Suppliers_Delete, L("DeleteSupplier"));

            var accounts = context.CreatePermission(PermissionNames.Pages_Accounts, L("Accounts"));
            accounts.CreateChildPermission(PermissionNames.Pages_Accounts_Create, L("CreateAccount"));
            accounts.CreateChildPermission(PermissionNames.Pages_Accounts_Edit, L("EditAccount"));
            accounts.CreateChildPermission(PermissionNames.Pages_Accounts_Delete, L("DeleteAccount"));

            var ledgerEntries = context.CreatePermission(PermissionNames.Pages_LedgerEntries, L("LedgerEntries"));
            ledgerEntries.CreateChildPermission(PermissionNames.Pages_LedgerEntries_Create, L("CreateLedgerEntry"));
            ledgerEntries.CreateChildPermission(PermissionNames.Pages_LedgerEntries_Edit, L("EditLedgerEntry"));
            ledgerEntries.CreateChildPermission(PermissionNames.Pages_LedgerEntries_Delete, L("DeleteLedgerEntry"));

            var purchases = context.CreatePermission(PermissionNames.Pages_Purchases, L("Purchases"));
            purchases.CreateChildPermission(PermissionNames.Pages_Purchases_Create, L("CreatePurchase"));
            purchases.CreateChildPermission(PermissionNames.Pages_Purchases_Edit, L("EditPurchase"));
            purchases.CreateChildPermission(PermissionNames.Pages_Purchases_Delete, L("DeletePurchase"));

            var sales = context.CreatePermission(PermissionNames.Pages_Sales, L("Sales"));
            sales.CreateChildPermission(PermissionNames.Pages_Sales_Create, L("CreateSale"));
            sales.CreateChildPermission(PermissionNames.Pages_Sales_Edit, L("EditSale"));
            sales.CreateChildPermission(PermissionNames.Pages_Sales_Delete, L("DeleteSale"));

            var stockAdjustments = context.CreatePermission(PermissionNames.Pages_StockAdjustments, L("StockAdjustments"));
            stockAdjustments.CreateChildPermission(PermissionNames.Pages_StockAdjustments_Create, L("CreateStockAdjustment"));
            stockAdjustments.CreateChildPermission(PermissionNames.Pages_StockAdjustments_Edit, L("EditStockAdjustment"));
            stockAdjustments.CreateChildPermission(PermissionNames.Pages_StockAdjustments_Delete, L("DeleteStockAdjustment"));

            var expenses = context.CreatePermission(PermissionNames.Pages_Expenses, L("Expenses"));
            expenses.CreateChildPermission(PermissionNames.Pages_Expenses_Create, L("CreateExpense"));
            expenses.CreateChildPermission(PermissionNames.Pages_Expenses_Edit, L("EditExpense"));
            expenses.CreateChildPermission(PermissionNames.Pages_Expenses_Delete, L("DeleteExpense"));

            context.CreatePermission(PermissionNames.Pages_Reports, L("Reports"));

            var customerOrders = context.CreatePermission(PermissionNames.Pages_CustomerOrders, L("CustomerOrders"));
            customerOrders.CreateChildPermission(PermissionNames.Pages_CustomerOrders_Create, L("CreateCustomerOrder"));
            customerOrders.CreateChildPermission(PermissionNames.Pages_CustomerOrders_Edit, L("EditCustomerOrder"));
            customerOrders.CreateChildPermission(PermissionNames.Pages_CustomerOrders_Delete, L("DeleteCustomerOrder"));

            var emailTemplates = context.CreatePermission(PermissionNames.Pages_EmailTemplates, L("EmailTemplates"));
            emailTemplates.CreateChildPermission(PermissionNames.Pages_EmailTemplates_Create, L("CreateEmailTemplate"));
            emailTemplates.CreateChildPermission(PermissionNames.Pages_EmailTemplates_Edit, L("EditEmailTemplate"));
            emailTemplates.CreateChildPermission(PermissionNames.Pages_EmailTemplates_Delete, L("DeleteEmailTemplate"));

            var lookUps = context.CreatePermission(PermissionNames.Pages_LookUps, L("LookUps"));
            lookUps.CreateChildPermission(PermissionNames.Pages_LookUps_Create, L("CreateLookUp"));
            lookUps.CreateChildPermission(PermissionNames.Pages_LookUps_Edit, L("EditLookUp"));
            lookUps.CreateChildPermission(PermissionNames.Pages_LookUps_Delete, L("DeleteLookUp"));

            var hostCatalog = context.CreatePermission(
                PermissionNames.Pages_HostCatalog,
                L("HostCatalog"),
                multiTenancySides: MultiTenancySides.Host);
            hostCatalog.CreateChildPermission(PermissionNames.Pages_HostCatalog_Create, L("CreateHostCatalog"));
            hostCatalog.CreateChildPermission(PermissionNames.Pages_HostCatalog_Edit, L("EditHostCatalog"));
            hostCatalog.CreateChildPermission(PermissionNames.Pages_HostCatalog_Delete, L("DeleteHostCatalog"));

            var tenants = context.CreatePermission(PermissionNames.Pages_Tenants, L("Tenants"), multiTenancySides: MultiTenancySides.Host);
            tenants.CreateChildPermission(PermissionNames.Pages_Tenants_Create, L("CreateTenant"));
            tenants.CreateChildPermission(PermissionNames.Pages_Tenants_Edit, L("EditTenant"));
            tenants.CreateChildPermission(PermissionNames.Pages_Tenants_Delete, L("DeleteTenant"));
        }

        private static ILocalizableString L(string name)
        {
            return new LocalizableString(name, SmartPosConsts.LocalizationSourceName);
        }
    }
}
