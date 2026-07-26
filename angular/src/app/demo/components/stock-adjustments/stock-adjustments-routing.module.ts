import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';
import { StockAdjustmentListComponent } from './stock-adjustment-list.component';

@NgModule({
    imports: [
        RouterModule.forChild([
            {
                path: '',
                component: StockAdjustmentListComponent,
                data: { breadcrumb: 'Stock Adjustments' },
            },
        ]),
    ],
    exports: [RouterModule],
})
export class StockAdjustmentsRoutingModule {}
