import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';
import { PurchaseListComponent } from './purchase-list.component';

@NgModule({
    imports: [
        RouterModule.forChild([
            {
                path: '',
                component: PurchaseListComponent,
                data: { breadcrumb: 'Purchases' },
            },
        ]),
    ],
    exports: [RouterModule],
})
export class PurchasesRoutingModule {}
