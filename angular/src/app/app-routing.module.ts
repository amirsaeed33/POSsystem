import { RouterModule, Routes } from '@angular/router';
import { NgModule } from '@angular/core';
import { AppLayoutComponent } from './layout/app.layout.component';
import { AuthGuard } from './demo/service/auth.guard';
import { PermissionGuard } from './demo/service/permission.guard';
import { PermissionNames } from './demo/api/permission-names';
import { StaffTabRedirectComponent } from './demo/components/staff-hub/staff-tab-redirect.component';

const routes: Routes = [
    {
        path: '',
        component: AppLayoutComponent,
        canActivate: [AuthGuard],
        canActivateChild: [PermissionGuard],
        children: [
            { path: '', loadChildren: () => import('./demo/components/dashboards/dashboards.module').then(m => m.DashboardsModule) },
            { path: 'pos', data: { breadcrumb: 'POS', permission: PermissionNames.Sales }, loadChildren: () => import('./demo/components/pos/pos.module').then(m => m.PosModule) },
            { path: 'categories', redirectTo: '/product-settings', pathMatch: 'full' },
            { path: 'brands', redirectTo: '/product-settings', pathMatch: 'full' },
            { path: 'units', redirectTo: '/product-settings', pathMatch: 'full' },
            {
                path: 'product-settings',
                data: {
                    breadcrumb: 'Product Settings',
                    anyPermission: [
                        PermissionNames.Brands,
                        PermissionNames.Units,
                        PermissionNames.Categories,
                    ],
                },
                loadChildren: () =>
                    import('./demo/components/product-settings/product-settings.module').then(
                        (m) => m.ProductSettingsModule
                    ),
            },
            { path: 'customers', data: { breadcrumb: 'Customers', permission: PermissionNames.Customers }, loadChildren: () => import('./demo/components/customers/customers.module').then(m => m.CustomersModule) },
            {
                path: 'staff',
                data: {
                    breadcrumb: 'Staff',
                    anyPermission: [
                        PermissionNames.Staff,
                        PermissionNames.StaffAttendance,
                        PermissionNames.StaffPayroll,
                    ],
                },
                loadChildren: () =>
                    import('./demo/components/staff-hub/staff-hub.module').then(
                        (m) => m.StaffHubModule
                    ),
            },
            {
                path: 'staff-attendance',
                component: StaffTabRedirectComponent,
                data: { tab: 'attendance' },
            },
            {
                path: 'staff-payroll',
                component: StaffTabRedirectComponent,
                data: { tab: 'payroll' },
            },

            { path: 'suppliers', data: { breadcrumb: 'Suppliers', permission: PermissionNames.Suppliers }, loadChildren: () => import('./demo/components/suppliers/suppliers.module').then(m => m.SuppliersModule) },
            { path: 'products', data: { breadcrumb: 'Products', permission: PermissionNames.Products }, loadChildren: () => import('./demo/components/products/products.module').then(m => m.ProductsModule) },
            { path: 'customer-orders', data: { breadcrumb: 'Customer Orders', permission: PermissionNames.CustomerOrders }, loadChildren: () => import('./demo/components/customer-orders/customer-orders.module').then(m => m.CustomerOrdersModule) },
            { path: 'purchases', data: { breadcrumb: 'Purchases', permission: PermissionNames.Purchases }, loadChildren: () => import('./demo/components/purchases/purchases.module').then(m => m.PurchasesModule) },
            { path: 'sales', data: { breadcrumb: 'Sales', permission: PermissionNames.Sales }, loadChildren: () => import('./demo/components/sales/sales.module').then(m => m.SalesModule) },
            { path: 'sale-returns', data: { breadcrumb: 'Sale Returns', permission: PermissionNames.Sales }, loadChildren: () => import('./demo/components/sale-returns/sale-returns.module').then(m => m.SaleReturnsModule) },
            { path: 'purchase-returns', data: { breadcrumb: 'Purchase Returns', permission: PermissionNames.Purchases }, loadChildren: () => import('./demo/components/purchase-returns/purchase-returns.module').then(m => m.PurchaseReturnsModule) },
            { path: 'stock-adjustments', data: { breadcrumb: 'Stock Adjustments', permission: PermissionNames.StockAdjustments }, loadChildren: () => import('./demo/components/stock-adjustments/stock-adjustments.module').then(m => m.StockAdjustmentsModule) },
            { path: 'expenses', data: { breadcrumb: 'Expenses', permission: PermissionNames.Expenses }, loadChildren: () => import('./demo/components/expenses/expenses.module').then(m => m.ExpensesModule) },
            { path: 'accounts', data: { breadcrumb: 'Accounts', permission: PermissionNames.Accounts }, loadChildren: () => import('./demo/components/accounts/accounts.module').then(m => m.AccountsModule) },
            { path: 'ledger-entries', data: { breadcrumb: 'Ledger', permission: PermissionNames.LedgerEntries }, loadChildren: () => import('./demo/components/ledger-entries/ledger-entries.module').then(m => m.LedgerEntriesModule) },
            { path: 'branches', data: { breadcrumb: 'Branches', permission: PermissionNames.Branches }, loadChildren: () => import('./demo/components/branches/branches.module').then(m => m.BranchesModule) },
            { path: 'lookups', data: { breadcrumb: 'Lookups', permission: PermissionNames.LookUps }, loadChildren: () => import('./demo/components/lookups/lookups.module').then(m => m.LookUpsModule) },
            { path: 'host-catalog', data: { breadcrumb: 'Company types', permission: PermissionNames.HostCatalog }, loadChildren: () => import('./demo/components/host-catalog/host-catalog.module').then(m => m.HostCatalogModule) },
            { path: 'email-templates', data: { breadcrumb: 'Email Templates', permission: PermissionNames.EmailTemplates }, loadChildren: () => import('./demo/components/email-templates/email-templates.module').then(m => m.EmailTemplatesModule) },
            { path: 'tenants', data: { breadcrumb: 'Tenants', permission: PermissionNames.Tenants }, loadChildren: () => import('./demo/components/tenants/tenants.module').then(m => m.TenantsModule) },
            { path: 'reports', data: { breadcrumb: 'Reports', permission: PermissionNames.Reports }, loadChildren: () => import('./demo/components/reports/reports.module').then(m => m.ReportsModule) },
            { path: 'profile', data: { breadcrumb: 'User Management' }, loadChildren: () => import('./demo/components/profile/profile.module').then(m => m.ProfileModule) },
        ]
    },
    { path: 'auth', data: { breadcrumb: 'Auth' }, loadChildren: () => import('./demo/components/auth/auth.module').then(m => m.AuthModule) },
    { path: 'notfound', loadChildren: () => import('./demo/components/notfound/notfound.module').then(m => m.NotfoundModule) },
    { path: '**', redirectTo: '/notfound' }
];

@NgModule({
    imports: [RouterModule.forRoot(routes)],
    exports: [RouterModule]
})
export class AppRoutingModule { }
