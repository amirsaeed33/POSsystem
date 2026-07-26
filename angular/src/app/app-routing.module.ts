import { RouterModule, Routes } from '@angular/router';
import { NgModule } from '@angular/core';
import { AppLayoutComponent } from './layout/app.layout.component';
import { AuthGuard } from './demo/service/auth.guard';

const routes: Routes = [
    {
        path: '',
        component: AppLayoutComponent,
        canActivate: [AuthGuard],
        children: [
            { path: '', loadChildren: () => import('./demo/components/dashboards/dashboards.module').then(m => m.DashboardsModule) },
            { path: 'pos', data: { breadcrumb: 'POS' }, loadChildren: () => import('./demo/components/pos/pos.module').then(m => m.PosModule) },
            { path: 'categories', data: { breadcrumb: 'Categories' }, loadChildren: () => import('./demo/components/categories/categories.module').then(m => m.CategoriesModule) },
            { path: 'brands', data: { breadcrumb: 'Brands' }, loadChildren: () => import('./demo/components/brands/brands.module').then(m => m.BrandsModule) },
            { path: 'units', data: { breadcrumb: 'Units' }, loadChildren: () => import('./demo/components/units/units.module').then(m => m.UnitsModule) },
            { path: 'customers', data: { breadcrumb: 'Customers' }, loadChildren: () => import('./demo/components/customers/customers.module').then(m => m.CustomersModule) },
            { path: 'suppliers', data: { breadcrumb: 'Suppliers' }, loadChildren: () => import('./demo/components/suppliers/suppliers.module').then(m => m.SuppliersModule) },
            { path: 'products', data: { breadcrumb: 'Products' }, loadChildren: () => import('./demo/components/products/products.module').then(m => m.ProductsModule) },
            { path: 'customer-orders', data: { breadcrumb: 'Customer Orders' }, loadChildren: () => import('./demo/components/customer-orders/customer-orders.module').then(m => m.CustomerOrdersModule) },
            { path: 'purchases', data: { breadcrumb: 'Purchases' }, loadChildren: () => import('./demo/components/purchases/purchases.module').then(m => m.PurchasesModule) },
            { path: 'sales', data: { breadcrumb: 'Sales' }, loadChildren: () => import('./demo/components/sales/sales.module').then(m => m.SalesModule) },
            { path: 'stock-adjustments', data: { breadcrumb: 'Stock Adjustments' }, loadChildren: () => import('./demo/components/stock-adjustments/stock-adjustments.module').then(m => m.StockAdjustmentsModule) },
            { path: 'expenses', data: { breadcrumb: 'Expenses' }, loadChildren: () => import('./demo/components/expenses/expenses.module').then(m => m.ExpensesModule) },
            { path: 'accounts', data: { breadcrumb: 'Accounts' }, loadChildren: () => import('./demo/components/accounts/accounts.module').then(m => m.AccountsModule) },
            { path: 'ledger-entries', data: { breadcrumb: 'Ledger' }, loadChildren: () => import('./demo/components/ledger-entries/ledger-entries.module').then(m => m.LedgerEntriesModule) },
            { path: 'company-profiles', data: { breadcrumb: 'Company Profiles' }, loadChildren: () => import('./demo/components/company-profiles/company-profiles.module').then(m => m.CompanyProfilesModule) },
            { path: 'tenants', data: { breadcrumb: 'Tenants' }, loadChildren: () => import('./demo/components/tenants/tenants.module').then(m => m.TenantsModule) },
            { path: 'reports', data: { breadcrumb: 'Reports' }, loadChildren: () => import('./demo/components/reports/reports.module').then(m => m.ReportsModule) },
            { path: 'uikit', data: { breadcrumb: 'UI Kit' }, loadChildren: () => import('./demo/components/uikit/uikit.module').then(m => m.UIkitModule) },
            { path: 'utilities', data: { breadcrumb: 'Utilities' }, loadChildren: () => import('./demo/components/utilities/utilities.module').then(m => m.UtilitiesModule) },
            { path: 'pages', data: { breadcrumb: 'Pages' }, loadChildren: () => import('./demo/components/pages/pages.module').then(m => m.PagesModule) },
            { path: 'profile', data: { breadcrumb: 'User Management' }, loadChildren: () => import('./demo/components/profile/profile.module').then(m => m.ProfileModule) },
            { path: 'documentation', data: { breadcrumb: 'Documentation' }, loadChildren: () => import('./demo/components/documentation/documentation.module').then(m => m.DocumentationModule) },
            { path: 'blocks', data: { breadcrumb: 'Prime Blocks' }, loadChildren: () => import('./demo/components/primeblocks/primeblocks.module').then(m => m.PrimeBlocksModule) },
            { path: 'ecommerce', data: { breadcrumb: 'E-Commerce' }, loadChildren: () => import('./demo/components/ecommerce/ecommerce.module').then(m => m.EcommerceModule) },
            { path: 'apps', data: { breadcrumb: 'Apps' }, loadChildren: () => import('./demo/components/apps/apps.module').then(m => m.AppsModule) }
        ]
    },
    { path: 'auth', data: { breadcrumb: 'Auth' }, loadChildren: () => import('./demo/components/auth/auth.module').then(m => m.AuthModule) },
    { path: 'landing', loadChildren: () => import('./demo/components/landing/landing.module').then(m => m.LandingModule) },
    { path: 'notfound', loadChildren: () => import('./demo/components/notfound/notfound.module').then(m => m.NotfoundModule) },
    { path: '**', redirectTo: '/notfound' }
];

@NgModule({
    imports: [RouterModule.forRoot(routes)],
    exports: [RouterModule]
})
export class AppRoutingModule { }
