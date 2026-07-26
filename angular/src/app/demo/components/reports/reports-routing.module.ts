import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';
import { SaleReportComponent } from './sale-report.component';
import { PurchaseReportComponent } from './purchase-report.component';
import { ExpenseReportComponent } from './expense-report.component';
import { StockReportComponent } from './stock-report.component';

@NgModule({
    imports: [
        RouterModule.forChild([
            { path: '', redirectTo: 'sales', pathMatch: 'full' },
            {
                path: 'sales',
                component: SaleReportComponent,
                data: { breadcrumb: 'Sale Report' },
            },
            {
                path: 'purchases',
                component: PurchaseReportComponent,
                data: { breadcrumb: 'Purchase Report' },
            },
            {
                path: 'expenses',
                component: ExpenseReportComponent,
                data: { breadcrumb: 'Expense Report' },
            },
            {
                path: 'stock',
                component: StockReportComponent,
                data: { breadcrumb: 'Stock Report' },
            },
        ]),
    ],
    exports: [RouterModule],
})
export class ReportsRoutingModule {}
