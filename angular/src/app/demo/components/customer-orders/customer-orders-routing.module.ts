import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';
import { CustomerOrderListComponent } from './customer-order-list.component';

@NgModule({
    imports: [
        RouterModule.forChild([
            {
                path: '',
                component: CustomerOrderListComponent,
                data: { breadcrumb: 'Customer Orders' },
            },
        ]),
    ],
    exports: [RouterModule],
})
export class CustomerOrdersRoutingModule {}
