import { NgModule } from '@angular/core';
import { SharedModule } from '@shared/shared.module';
import { ReportsRoutingModule } from './reports-routing.module';
import { SaleReportComponent } from './sale-report/sale-report.component';
import { PurchaseReportComponent } from './purchase-report/purchase-report.component';
import { ExpenseReportComponent } from './expense-report/expense-report.component';
import { StockReportComponent } from './stock-report/stock-report.component';

@NgModule({
  declarations: [
    SaleReportComponent,
    PurchaseReportComponent,
    ExpenseReportComponent,
    StockReportComponent,
  ],
  imports: [SharedModule, ReportsRoutingModule],
})
export class ReportsModule {}
