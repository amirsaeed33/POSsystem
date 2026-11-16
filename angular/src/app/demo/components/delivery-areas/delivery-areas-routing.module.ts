import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';
import { DeliveryAreasComponent } from './delivery-areas.component';

@NgModule({
    imports: [
        RouterModule.forChild([
            {
                path: '',
                component: DeliveryAreasComponent,
                data: { breadcrumb: 'Delivery Areas' },
            },
        ]),
    ],
    exports: [RouterModule],
})
export class DeliveryAreasRoutingModule {}
