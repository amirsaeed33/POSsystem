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
import { PurchaseReturnsRoutingModule } from './purchase-returns-routing.module';
import { PurchaseReturnListComponent } from './purchase-return-list.component';
import { PurchaseReturnFormDialogComponent } from './purchase-return-form-dialog.component';
import { PurchaseReturnViewDialogComponent } from './purchase-return-view-dialog.component';

@NgModule({
    imports: [
        CommonModule,
        FormsModule,
        PurchaseReturnsRoutingModule,
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
        PurchaseReturnListComponent,
        PurchaseReturnFormDialogComponent,
        PurchaseReturnViewDialogComponent,
    ],
    exports: [PurchaseReturnFormDialogComponent],
})
export class PurchaseReturnsModule {}
