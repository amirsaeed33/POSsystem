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
                    { label: 'Categories', icon: 'pi pi-tags', routerLink: ['/categories'] },
                    { label: 'Brands', icon: 'pi pi-bookmark', routerLink: ['/brands'] },
                    { label: 'Units', icon: 'pi pi-percentage', routerLink: ['/units'] },
                    { label: 'Customers', icon: 'pi pi-users', routerLink: ['/customers'] },
                    { label: 'Suppliers', icon: 'pi pi-truck', routerLink: ['/suppliers'] },
                    { label: 'Products', icon: 'pi pi-box', routerLink: ['/products'] },
                    { label: 'Purchases', icon: 'pi pi-shopping-bag', routerLink: ['/purchases'] },
                    { label: 'Sales', icon: 'pi pi-shopping-cart', routerLink: ['/sales'] },
                    { label: 'Stock Adjustments', icon: 'pi pi-sync', routerLink: ['/stock-adjustments'] },
                    { label: 'Expenses', icon: 'pi pi-wallet', routerLink: ['/expenses'] },
                    { label: 'Reports', icon: 'pi pi-chart-bar' }
                ]
            }
        ];
    }
}
