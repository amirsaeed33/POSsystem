import {Component, Injector, OnInit} from '@angular/core';
import {AppComponentBase} from '@shared/app-component-base';
import {
    Router,
    RouterEvent,
    NavigationEnd,
    PRIMARY_OUTLET
} from '@angular/router';
import {BehaviorSubject} from 'rxjs';
import {filter} from 'rxjs/operators';
import {MenuItem} from '@shared/layout/menu-item';

@Component({
    selector: 'sidebar-menu',
    templateUrl: './sidebar-menu.component.html'
})
export class SidebarMenuComponent extends AppComponentBase implements OnInit {
    menuItems: MenuItem[];
    menuItemsMap: { [key: number]: MenuItem } = {};
    activatedMenuItems: MenuItem[] = [];
    routerEvents: BehaviorSubject<RouterEvent> = new BehaviorSubject(undefined);
    homeRoute = '/app/home';

    constructor(injector: Injector, private router: Router) {
        super(injector);
    }

    ngOnInit(): void {
        this.menuItems = this.getMenuItems();
        this.patchMenuItems(this.menuItems);
        this.deactivateMenuItems(this.menuItems);

        this.router.events
            .pipe(filter((event): event is NavigationEnd => event instanceof NavigationEnd))
            .subscribe((event: NavigationEnd) => {
                const currentUrl = event.urlAfterRedirects !== '/' ? event.urlAfterRedirects : this.homeRoute;
                const primaryUrlSegmentGroup = this.router.parseUrl(currentUrl).root
                    .children[PRIMARY_OUTLET];
                if (primaryUrlSegmentGroup) {
                    this.activateMenuItems('/' + primaryUrlSegmentGroup.toString());
                }
            });
    }

    getMenuItems(): MenuItem[] {
        return [
            new MenuItem(this.l('HomePage'), '/app/home', 'fas fa-home'),
            new MenuItem(this.l('Administration'), '', 'fas fa-cog', '', [
                new MenuItem(
                    this.l('Roles'),
                    '/app/roles',
                    'fas fa-theater-masks',
                    'Pages.Roles'
                ),
                new MenuItem(
                    this.l('Tenants'),
                    '/app/tenants',
                    'fas fa-building',
                    'Pages.Tenants'
                ),
                new MenuItem(
                    this.l('Users'),
                    '/app/users',
                    'fas fa-users',
                    'Pages.Users'
                ),
            ]),
            new MenuItem(this.l('ProductManagement'), '', 'fas fa-cubes', '', [
                new MenuItem(
                    this.l('Products'),
                    '/app/products',
                    'fas fa-box',
                    'Pages.Products'
                ),
                new MenuItem(
                    this.l('Brands'),
                    '/app/brands',
                    'fas fa-copyright',
                    'Pages.Brands'
                ),
                new MenuItem(
                    this.l('Categories'),
                    '/app/categories',
                    'fas fa-tags',
                    'Pages.Categories'
                ),
                new MenuItem(
                    this.l('Units'),
                    '/app/units',
                    'fas fa-balance-scale',
                    'Pages.Units'
                ),
                new MenuItem(
                    this.l('Purchases'),
                    '/app/purchases',
                    'fas fa-shopping-cart',
                    'Pages.Purchases'
                ),
                new MenuItem(
                    this.l('Sales'),
                    '/app/sales',
                    'fas fa-cash-register',
                    'Pages.Sales'
                ),
                new MenuItem(
                    this.l('CustomerOrders'),
                    '/app/customer-orders',
                    'fas fa-clipboard-list',
                    'Pages.CustomerOrders'
                ),
            ]),
            new MenuItem(this.l('BusinessManagement'), '', 'fas fa-briefcase', '', [
                new MenuItem(
                    this.l('Customers'),
                    '/app/customers',
                    'fas fa-user-friends',
                    'Pages.Customers'
                ),
                new MenuItem(
                    this.l('Suppliers'),
                    '/app/suppliers',
                    'fas fa-truck',
                    'Pages.Suppliers'
                ),
                new MenuItem(
                    this.l('Accounts'),
                    '/app/accounts',
                    'fas fa-wallet',
                    'Pages.Accounts'
                ),
                new MenuItem(
                    this.l('LedgerEntries'),
                    '/app/ledger-entries',
                    'fas fa-book',
                    'Pages.LedgerEntries'
                ),
                new MenuItem(
                    this.l('Expenses'),
                    '/app/expenses',
                    'fas fa-receipt',
                    'Pages.Expenses'
                ),
            ]),
            new MenuItem(this.l('Reports'), '', 'fas fa-chart-bar', 'Pages.Reports', [
                new MenuItem(
                    this.l('SaleReport'),
                    '/app/reports/sales',
                    'fas fa-file-invoice-dollar',
                    'Pages.Reports'
                ),
                new MenuItem(
                    this.l('PurchaseReport'),
                    '/app/reports/purchases',
                    'fas fa-file-invoice',
                    'Pages.Reports'
                ),
                new MenuItem(
                    this.l('ExpenseReport'),
                    '/app/reports/expenses',
                    'fas fa-file-alt',
                    'Pages.Reports'
                ),
                new MenuItem(
                    this.l('StockReport'),
                    '/app/reports/stock',
                    'fas fa-warehouse',
                    'Pages.Reports'
                ),
            ]),
        ];
    }

    patchMenuItems(items: MenuItem[], parentId?: number): void {
        items.forEach((item: MenuItem, index: number) => {
            item.id = parentId ? Number(parentId + '' + (index + 1)) : index + 1;
            if (parentId) {
                item.parentId = parentId;
            }
            if (parentId || item.children) {
                this.menuItemsMap[item.id] = item;
            }
            if (item.children) {
                this.patchMenuItems(item.children, item.id);
            }
        });
    }

    activateMenuItems(url: string): void {
        this.deactivateMenuItems(this.menuItems);
        this.activatedMenuItems = [];
        const foundedItems = this.findMenuItemsByUrl(url, this.menuItems);
        foundedItems.forEach((item) => {
            this.activateMenuItem(item);
        });
    }

    deactivateMenuItems(items: MenuItem[]): void {
        items.forEach((item: MenuItem) => {
            item.isActive = false;
            item.isCollapsed = true;
            if (item.children) {
                this.deactivateMenuItems(item.children);
            }
        });
    }

    findMenuItemsByUrl(
        url: string,
        items: MenuItem[],
        foundedItems: MenuItem[] = []
    ): MenuItem[] {
        items.forEach((item: MenuItem) => {
            if (item.route === url) {
                foundedItems.push(item);
            } else if (item.children) {
                this.findMenuItemsByUrl(url, item.children, foundedItems);
            }
        });
        return foundedItems;
    }

    activateMenuItem(item: MenuItem): void {
        item.isActive = true;
        if (item.children) {
            item.isCollapsed = false;
        }
        this.activatedMenuItems.push(item);
        if (item.parentId) {
            this.activateMenuItem(this.menuItemsMap[item.parentId]);
        }
    }

    toggleMenuItem(item: MenuItem): void {
        const willOpen = item.isCollapsed;
        if (willOpen) {
            this.collapseAllMenuItems(this.menuItems);
        }
        item.isCollapsed = !willOpen;
    }

    collapseAllMenuItems(items: MenuItem[]): void {
        items.forEach((menuItem: MenuItem) => {
            menuItem.isCollapsed = true;
            if (menuItem.children) {
                this.collapseAllMenuItems(menuItem.children);
            }
        });
    }

    isMenuItemVisible(item: MenuItem): boolean {
        if (!item.permissionName) {
            return true;
        }
        return this.permission.isGranted(item.permissionName);
    }
}
