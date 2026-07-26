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
import { PurchasesRoutingModule } from './purchases-routing.module';
import { PurchaseListComponent } from './purchase-list.component';
import { PurchaseFormDialogComponent } from './purchase-form-dialog.component';
import { PurchaseViewDialogComponent } from './purchase-view-dialog.component';
import { PrintPurchaseInvoiceDialogComponent } from './print-purchase-invoice-dialog.component';

@NgModule({
    imports: [
        CommonModule,
        FormsModule,
        PurchasesRoutingModule,
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
        PurchaseListComponent,
        PurchaseFormDialogComponent,
        PurchaseViewDialogComponent,
        PrintPurchaseInvoiceDialogComponent,
    ],
})
export class PurchasesModule {}
