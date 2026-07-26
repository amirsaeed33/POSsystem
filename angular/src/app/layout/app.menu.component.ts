import { Component, OnInit } from '@angular/core';
import { LayoutService } from './service/app.layout.service';

@Component({
    selector: 'app-menu',
    templateUrl: './app.menu.component.html'
})
export class AppMenuComponent implements OnInit {

    model: any[] = [];

    constructor(public layoutService: LayoutService) { }

    ngOnInit() {
        this.model = [
            {
                label: 'POS',
                icon: 'pi pi-shop',
                items: [
                    { label: 'Dashboard', icon: 'pi pi-home', routerLink: ['/'] },
                    { label: 'POS Screen', icon: 'pi pi-desktop', routerLink: ['/pos'] },
                    { label: 'Categories', icon: 'pi pi-tags', routerLink: ['/categories'] },
                    { label: 'Brands', icon: 'pi pi-bookmark', routerLink: ['/brands'] },
                    { label: 'Units', icon: 'pi pi-percentage', routerLink: ['/units'] },
                    { label: 'Customers', icon: 'pi pi-users', routerLink: ['/customers'] },
                    { label: 'Suppliers', icon: 'pi pi-truck', routerLink: ['/suppliers'] },
                    { label: 'Products', icon: 'pi pi-box', routerLink: ['/products'] },
                    { label: 'Customer Orders', icon: 'pi pi-file', routerLink: ['/customer-orders'] },
                    { label: 'Purchases', icon: 'pi pi-shopping-bag', routerLink: ['/purchases'] },
                    { label: 'Sales', icon: 'pi pi-shopping-cart', routerLink: ['/sales'] },
                    { label: 'Stock Adjustments', icon: 'pi pi-sync', routerLink: ['/stock-adjustments'] },
                    { label: 'Expenses', icon: 'pi pi-wallet', routerLink: ['/expenses'] },
                    {
                        label: 'Reports',
                        icon: 'pi pi-chart-bar',
                        items: [
                            { label: 'Sale Report', icon: 'pi pi-shopping-cart', routerLink: ['/reports/sales'] },
                            { label: 'Purchase Report', icon: 'pi pi-shopping-bag', routerLink: ['/reports/purchases'] },
                            { label: 'Expense Report', icon: 'pi pi-wallet', routerLink: ['/reports/expenses'] },
                            { label: 'Stock Report', icon: 'pi pi-box', routerLink: ['/reports/stock'] },
                        ]
                    }
                ]
            },
            {
                label: 'Business',
                icon: 'pi pi-briefcase',
                items: [
                    { label: 'Accounts', icon: 'pi pi-building', routerLink: ['/accounts'] },
                    { label: 'Ledger', icon: 'pi pi-book', routerLink: ['/ledger-entries'] },
                    { label: 'Company Profiles', icon: 'pi pi-id-card', routerLink: ['/company-profiles'] },
                ]
            },
            {
                label: 'Administration',
                icon: 'pi pi-cog',
                items: [
                    { label: 'Users', icon: 'pi pi-user', routerLink: ['/profile/list'] },
                    { label: 'Roles', icon: 'pi pi-lock', routerLink: ['/profile/role'] },
                    { label: 'Tenants', icon: 'pi pi-globe', routerLink: ['/tenants'] },
                ]
            }
        ];
    }
}
