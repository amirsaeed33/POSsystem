import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { RippleModule } from 'primeng/ripple';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { TableModule } from 'primeng/table';
import { DialogModule } from 'primeng/dialog';
import { ToastModule } from 'primeng/toast';
import { TooltipModule } from 'primeng/tooltip';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { DropdownModule } from 'primeng/dropdown';
import { StockAdjustmentsRoutingModule } from './stock-adjustments-routing.module';
import { StockAdjustmentListComponent } from './stock-adjustment-list.component';
import { StockAdjustmentFormDialogComponent } from './stock-adjustment-form-dialog.component';
import { StockAdjustmentViewDialogComponent } from './stock-adjustment-view-dialog.component';

@NgModule({
    imports: [
        CommonModule,
        FormsModule,
        StockAdjustmentsRoutingModule,
        ButtonModule,
        RippleModule,
        InputTextModule,
        InputNumberModule,
        TableModule,
        DialogModule,
        ToastModule,
        TooltipModule,
        ConfirmDialogModule,
        DropdownModule,
    ],
    declarations: [
        StockAdjustmentListComponent,
        StockAdjustmentFormDialogComponent,
        StockAdjustmentViewDialogComponent,
    ],
})
export class StockAdjustmentsModule {}
