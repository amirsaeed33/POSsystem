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
import { MenuModule } from 'primeng/menu';
import { PurchasesRoutingModule } from './purchases-routing.module';
import { PurchaseListComponent } from './purchase-list.component';
import { PurchaseFormDialogComponent } from './purchase-form-dialog.component';
import { PurchaseViewDialogComponent } from './purchase-view-dialog.component';
import { PurchasePayDialogComponent } from './purchase-pay-dialog.component';
import { InvoicePrintModule } from '../invoices/invoice-print.module';
import { PurchaseReturnsModule } from '../purchase-returns/purchase-returns.module';

@NgModule({
    imports: [
        CommonModule,
        FormsModule,
        PurchasesRoutingModule,
        InvoicePrintModule,
        PurchaseReturnsModule,
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
        MenuModule,
    ],
    declarations: [
        PurchaseListComponent,
        PurchaseFormDialogComponent,
        PurchaseViewDialogComponent,
        PurchasePayDialogComponent,
    ],
})
export class PurchasesModule {}
