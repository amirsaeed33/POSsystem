import { NgModule } from '@angular/core';
import { SharedModule } from '@shared/shared.module';
import { ExpensesRoutingModule } from './expenses-routing.module';
import { ExpensesComponent } from './expenses.component';
import { CreateExpenseDialogComponent } from './create-expense/create-expense-dialog.component';
import { PrintExpenseInvoiceDialogComponent } from './print-expense-invoice/print-expense-invoice-dialog.component';

@NgModule({
    declarations: [ExpensesComponent, CreateExpenseDialogComponent, PrintExpenseInvoiceDialogComponent],
    imports: [SharedModule, ExpensesRoutingModule],
})
export class ExpensesModule {}
