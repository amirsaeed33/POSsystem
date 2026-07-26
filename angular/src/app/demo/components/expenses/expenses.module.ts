import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { RippleModule } from 'primeng/ripple';
import { InputTextModule } from 'primeng/inputtext';
import { InputTextareaModule } from 'primeng/inputtextarea';
import { InputNumberModule } from 'primeng/inputnumber';
import { TableModule } from 'primeng/table';
import { DialogModule } from 'primeng/dialog';
import { ToastModule } from 'primeng/toast';
import { TooltipModule } from 'primeng/tooltip';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { DropdownModule } from 'primeng/dropdown';
import { ExpensesRoutingModule } from './expenses-routing.module';
import { ExpenseListComponent } from './expense-list.component';
import { ExpenseFormDialogComponent } from './expense-form-dialog.component';
import { InvoicePrintModule } from '../invoices/invoice-print.module';

@NgModule({
    imports: [
        CommonModule,
        FormsModule,
        ExpensesRoutingModule,
        InvoicePrintModule,
        ButtonModule,
        RippleModule,
        InputTextModule,
        InputTextareaModule,
        InputNumberModule,
        TableModule,
        DialogModule,
        ToastModule,
        TooltipModule,
        ConfirmDialogModule,
        DropdownModule,
    ],
    declarations: [ExpenseListComponent, ExpenseFormDialogComponent],
})
export class ExpensesModule {}
