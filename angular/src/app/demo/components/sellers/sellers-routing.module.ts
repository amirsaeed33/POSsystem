import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';
import { SellerListComponent } from './seller-list.component';

@NgModule({
    imports: [
        RouterModule.forChild([{ path: '', component: SellerListComponent }]),
    ],
    exports: [RouterModule],
})
export class SellersRoutingModule {}
