import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';
import { PurchaseReturnListComponent } from './purchase-return-list.component';

@NgModule({
    imports: [
        RouterModule.forChild([
            {
                path: '',
                component: PurchaseReturnListComponent,
                data: { breadcrumb: 'Purchase Returns' },
            },
        ]),
    ],
    exports: [RouterModule],
})
export class PurchaseReturnsRoutingModule {}
