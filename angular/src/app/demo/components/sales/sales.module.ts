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
import { TagModule } from 'primeng/tag';
import { SalesRoutingModule } from './sales-routing.module';
import { SaleListComponent } from './sale-list.component';
import { SaleFormDialogComponent } from './sale-form-dialog.component';
import { SaleViewDialogComponent } from './sale-view-dialog.component';
import { InvoicePrintModule } from '../invoices/invoice-print.module';
import { SaleReturnsModule } from '../sale-returns/sale-returns.module';

@NgModule({
    imports: [
        CommonModule,
        FormsModule,
        SalesRoutingModule,
        InvoicePrintModule,
        SaleReturnsModule,
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
        TagModule,
    ],
    declarations: [
        SaleListComponent,
        SaleFormDialogComponent,
        SaleViewDialogComponent,
    ],
})
export class SalesModule {}
