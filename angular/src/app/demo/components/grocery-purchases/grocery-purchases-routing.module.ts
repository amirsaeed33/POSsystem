import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';
import { GroceryPurchaseListComponent } from './grocery-purchase-list.component';

@NgModule({
    imports: [
        RouterModule.forChild([
            { path: '', component: GroceryPurchaseListComponent },
        ]),
    ],
    exports: [RouterModule],
})
export class GroceryPurchasesRoutingModule {}
