import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { RippleModule } from 'primeng/ripple';
import { DialogModule } from 'primeng/dialog';
import { PrintSaleInvoiceDialogComponent } from '../sales/print-sale-invoice-dialog.component';
import { PrintPurchaseInvoiceDialogComponent } from '../purchases/print-purchase-invoice-dialog.component';
import { PrintExpenseInvoiceDialogComponent } from '../expenses/print-expense-invoice-dialog.component';

@NgModule({
    imports: [
        CommonModule,
        FormsModule,
        DialogModule,
        ButtonModule,
        RippleModule,
    ],
    declarations: [
        PrintSaleInvoiceDialogComponent,
        PrintPurchaseInvoiceDialogComponent,
        PrintExpenseInvoiceDialogComponent,
    ],
    exports: [
        PrintSaleInvoiceDialogComponent,
        PrintPurchaseInvoiceDialogComponent,
        PrintExpenseInvoiceDialogComponent,
    ],
})
export class InvoicePrintModule {}
