import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';
import { OnlineOrderComponent } from './online-order.component';

@NgModule({
    imports: [
        RouterModule.forChild([
            { path: '', component: OnlineOrderComponent },
        ]),
    ],
    exports: [RouterModule],
})
export class OnlineOrderRoutingModule {}
