import { Component, OnInit } from '@angular/core';
import { PermissionNames } from '../demo/api/permission-names';
import { PermissionService } from '../demo/service/permission.service';
import { LayoutService } from './service/app.layout.service';

@Component({
    selector: 'app-menu',
    templateUrl: './app.menu.component.html'
})
export class AppMenuComponent implements OnInit {

    model: any[] = [];

    constructor(
        public layoutService: LayoutService,
        private permissionService: PermissionService
    ) { }

    async ngOnInit() {
        try {
            await this.permissionService.ensureLoaded();
        } catch {
            // Menu falls back to cached grants (if any).
        }

        // Every root entry is a collapsible group (static/overlay) or hover panel (slim).
        const menu = [
            {
                label: 'Main',
                icon: 'pi pi-home',
                items: [
                    { label: 'Dashboard', icon: 'pi pi-home', routerLink: ['/'] },
                    { label: 'POS Screen', icon: 'pi pi-desktop', routerLink: ['/pos'], permission: PermissionNames.Sales },
                ]
            },
            {
                label: 'Sales',
                icon: 'pi pi-shopping-cart',
                items: [
                    { label: 'Sales', icon: 'pi pi-shopping-cart', routerLink: ['/sales'], permission: PermissionNames.Sales },
                    { label: 'Sale Returns', icon: 'pi pi-replay', routerLink: ['/sale-returns'], permission: PermissionNames.Sales },
                    { label: 'Customer Orders', icon: 'pi pi-file', routerLink: ['/customer-orders'], permission: PermissionNames.CustomerOrders },
                    { label: 'Customers', icon: 'pi pi-users', routerLink: ['/customers'], permission: PermissionNames.Customers },
                ]
            },
            {
                label: 'Products',
                icon: 'pi pi-box',
                items: [
                    { label: 'Products', icon: 'pi pi-box', routerLink: ['/products'], permission: PermissionNames.Products },
                    {
                        label: 'Settings',
                        icon: 'pi pi-cog',
                        routerLink: ['/product-settings'],
                        anyPermission: [
                            PermissionNames.Brands,
                            PermissionNames.Units,
                            PermissionNames.Categories,
                        ],
                    },
                    { label: 'Purchases', icon: 'pi pi-shopping-bag', routerLink: ['/purchases'], permission: PermissionNames.Purchases },
                    { label: 'Purchase Returns', icon: 'pi pi-replay', routerLink: ['/purchase-returns'], permission: PermissionNames.Purchases },
                    { label: 'Stock Adjustments', icon: 'pi pi-sync', routerLink: ['/stock-adjustments'], permission: PermissionNames.StockAdjustments },
                ]
            },
            {
                label: 'Business',
                icon: 'pi pi-briefcase',
                items: [
                    { label: 'Suppliers', icon: 'pi pi-truck', routerLink: ['/suppliers'], permission: PermissionNames.Suppliers },
                    { label: 'Accounts', icon: 'pi pi-building', routerLink: ['/accounts'], permission: PermissionNames.Accounts },
                    { label: 'Ledger', icon: 'pi pi-book', routerLink: ['/ledger-entries'], permission: PermissionNames.LedgerEntries },
                    { label: 'Expenses', icon: 'pi pi-wallet', routerLink: ['/expenses'], permission: PermissionNames.Expenses },
                    { label: 'Company Profiles', icon: 'pi pi-id-card', routerLink: ['/company-profiles'], permission: PermissionNames.CompanyProfiles },
                ]
            },
            {
                label: 'Reports',
                icon: 'pi pi-chart-bar',
                permission: PermissionNames.Reports,
                items: [
                    { label: 'Sale Report', icon: 'pi pi-shopping-cart', routerLink: ['/reports/sales'], permission: PermissionNames.Reports },
                    { label: 'Purchase Report', icon: 'pi pi-shopping-bag', routerLink: ['/reports/purchases'], permission: PermissionNames.Reports },
                    { label: 'Expense Report', icon: 'pi pi-wallet', routerLink: ['/reports/expenses'], permission: PermissionNames.Reports },
                    { label: 'Stock Report', icon: 'pi pi-box', routerLink: ['/reports/stock'], permission: PermissionNames.Reports },
                ]
            },
            {
                label: 'Administration',
                icon: 'pi pi-cog',
                items: [
                    { label: 'Users', icon: 'pi pi-user', routerLink: ['/profile/list'], permission: PermissionNames.Users },
                    { label: 'Roles', icon: 'pi pi-lock', routerLink: ['/profile/role'], permission: PermissionNames.Roles },
                    { label: 'Tenants', icon: 'pi pi-globe', routerLink: ['/tenants'], permission: PermissionNames.Tenants },
                ]
            }
        ];

        this.model = this.filterMenuByPermission(menu);
    }

    private filterMenuByPermission(items: any[]): any[] {
        return items
            .map((item) => {
                const children = item.items
                    ? this.filterMenuByPermission(item.items)
                    : undefined;
                return {
                    ...item,
                    items: children,
                };
            })
            .filter((item) => {
                if (Array.isArray(item.anyPermission) && item.anyPermission.length) {
                    if (
                        !item.anyPermission.some((p: string) =>
                            this.permissionService.isGranted(p)
                        )
                    ) {
                        return false;
                    }
                } else if (!this.permissionService.isGranted(item.permission)) {
                    return false;
                }
                if (item.items) {
                    return item.items.length > 0;
                }
                return true;
            });
    }
}
