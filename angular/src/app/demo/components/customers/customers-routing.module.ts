import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';
import { CustomerListComponent } from './customer-list.component';

@NgModule({
    imports: [
        RouterModule.forChild([
            {
                path: '',
                component: CustomerListComponent,
                data: { breadcrumb: 'Customers' },
            },
        ]),
    ],
    exports: [RouterModule],
})
export class CustomersRoutingModule {}
