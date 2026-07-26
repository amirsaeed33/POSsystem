import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';
import { ExpenseListComponent } from './expense-list.component';

@NgModule({
    imports: [
        RouterModule.forChild([
            {
                path: '',
                component: ExpenseListComponent,
                data: { breadcrumb: 'Expenses' },
            },
        ]),
    ],
    exports: [RouterModule],
})
export class ExpensesRoutingModule {}
