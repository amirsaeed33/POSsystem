import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';
import { AppRouteGuard } from '@shared/auth/auth-route-guard';
import { AppComponent } from './app.component';

@NgModule({
    imports: [
        RouterModule.forChild([
            {
                path: '',
                component: AppComponent,
                children: [
                    {
                        path: 'home',
                        loadChildren: () => import('./home/home.module').then((m) => m.HomeModule),
                        canActivate: [AppRouteGuard]
                    },
                    {
                        path: 'about',
                        loadChildren: () => import('./about/about.module').then((m) => m.AboutModule),
                        canActivate: [AppRouteGuard]
                    },
                    {
                        path: 'users',
                        loadChildren: () => import('./users/users.module').then((m) => m.UsersModule),
                        data: { permission: 'Pages.Users' },
                        canActivate: [AppRouteGuard]
                    },
                    {
                        path: 'roles',
                        loadChildren: () => import('./roles/roles.module').then((m) => m.RolesModule),
                        data: { permission: 'Pages.Roles' },
                        canActivate: [AppRouteGuard]
                    },
                    {
                        path: 'tenants',
                        loadChildren: () => import('./tenants/tenants.module').then((m) => m.TenantsModule),
                        data: { permission: 'Pages.Tenants' },
                        canActivate: [AppRouteGuard]
                    },
                    {
                        path: 'categories',
                        loadChildren: () => import('./categories/categories.module').then((m) => m.CategoriesModule),
                        data: { permission: 'Pages.Categories' },
                        canActivate: [AppRouteGuard]
                    },
                    {
                        path: 'brands',
                        loadChildren: () => import('./brands/brands.module').then((m) => m.BrandsModule),
                        data: { permission: 'Pages.Brands' },
                        canActivate: [AppRouteGuard]
                    },
                    {
                        path: 'company-profiles',
                        loadChildren: () => import('./company-profiles/company-profiles.module').then((m) => m.CompanyProfilesModule),
                        data: { permission: 'Pages.CompanyProfiles' },
                        canActivate: [AppRouteGuard]
                    },
                    {
                        path: 'units',
                        loadChildren: () => import('./units/units.module').then((m) => m.UnitsModule),
                        data: { permission: 'Pages.Units' },
                        canActivate: [AppRouteGuard]
                    },
                    {
                        path: 'products',
                        loadChildren: () => import('./products/products.module').then((m) => m.ProductsModule),
                        data: { permission: 'Pages.Products' },
                        canActivate: [AppRouteGuard]
                    },
                    {
                        path: 'purchases',
                        loadChildren: () => import('./purchases/purchases.module').then((m) => m.PurchasesModule),
                        data: { permission: 'Pages.Purchases' },
                        canActivate: [AppRouteGuard]
                    },
                    {
                        path: 'sales',
                        loadChildren: () => import('./sales/sales.module').then((m) => m.SalesModule),
                        data: { permission: 'Pages.Sales' },
                        canActivate: [AppRouteGuard]
                    },
                    {
                        path: 'pos',
                        loadChildren: () => import('./pos/pos.module').then((m) => m.PosModule),
                        data: { permission: 'Pages.Sales' },
                        canActivate: [AppRouteGuard]
                    },
                    {
                        path: 'stock-adjustments',
                        loadChildren: () => import('./stock-adjustments/stock-adjustments.module').then((m) => m.StockAdjustmentsModule),
                        data: { permission: 'Pages.StockAdjustments' },
                        canActivate: [AppRouteGuard]
                    },
                    {
                        path: 'customer-orders',
                        loadChildren: () => import('./customer-orders/customer-orders.module').then((m) => m.CustomerOrdersModule),
                        data: { permission: 'Pages.CustomerOrders' },
                        canActivate: [AppRouteGuard]
                    },
                    {
                        path: 'customers',
                        loadChildren: () => import('./customers/customers.module').then((m) => m.CustomersModule),
                        data: { permission: 'Pages.Customers' },
                        canActivate: [AppRouteGuard]
                    },
                    {
                        path: 'suppliers',
                        loadChildren: () => import('./suppliers/suppliers.module').then((m) => m.SuppliersModule),
                        data: { permission: 'Pages.Suppliers' },
                        canActivate: [AppRouteGuard]
                    },
                    {
                        path: 'accounts',
                        loadChildren: () => import('./accounts/accounts.module').then((m) => m.AccountsModule),
                        data: { permission: 'Pages.Accounts' },
                        canActivate: [AppRouteGuard]
                    },
                    {
                        path: 'ledger-entries',
                        loadChildren: () => import('./ledger-entries/ledger-entries.module').then((m) => m.LedgerEntriesModule),
                        data: { permission: 'Pages.LedgerEntries' },
                        canActivate: [AppRouteGuard]
                    },
                    {
                        path: 'expenses',
                        loadChildren: () => import('./expenses/expenses.module').then((m) => m.ExpensesModule),
                        data: { permission: 'Pages.Expenses' },
                        canActivate: [AppRouteGuard]
                    },
                    {
                        path: 'reports',
                        loadChildren: () => import('./reports/reports.module').then((m) => m.ReportsModule),
                        data: { permission: 'Pages.Reports' },
                        canActivate: [AppRouteGuard]
                    },
                    {
                        path: 'update-password',
                        loadChildren: () => import('./users/users.module').then((m) => m.UsersModule),
                        canActivate: [AppRouteGuard]
                    },
                ]
            }
        ])
    ],
    exports: [RouterModule]
})
export class AppRoutingModule { }
