import { NgModule } from '@angular/core';
import { SharedModule } from '@shared/shared.module';
import { StockAdjustmentsRoutingModule } from './stock-adjustments-routing.module';
import { StockAdjustmentsComponent } from './stock-adjustments.component';
import { CreateStockAdjustmentDialogComponent } from './create-stock-adjustment/create-stock-adjustment-dialog.component';
import { ViewStockAdjustmentDialogComponent } from './view-stock-adjustment/view-stock-adjustment-dialog.component';

@NgModule({
    declarations: [
        StockAdjustmentsComponent,
        CreateStockAdjustmentDialogComponent,
        ViewStockAdjustmentDialogComponent,
    ],
    imports: [SharedModule, StockAdjustmentsRoutingModule],
})
export class StockAdjustmentsModule {}
