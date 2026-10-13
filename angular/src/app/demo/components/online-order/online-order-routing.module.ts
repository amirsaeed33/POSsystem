import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';
import { OnlineOrderComponent } from './online-order.component';
import { OnlineProductDetailComponent } from './online-product-detail.component';

@NgModule({
    imports: [
        RouterModule.forChild([
            { path: '', component: OnlineOrderComponent },
            { path: 'product', component: OnlineOrderComponent },
            { path: 'product/:id', component: OnlineProductDetailComponent },
        ]),
    ],
    exports: [RouterModule],
})
export class OnlineOrderRoutingModule {}
