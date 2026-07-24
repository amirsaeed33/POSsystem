import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AppRouteGuard } from '@shared/auth/auth-route-guard';
import { SaleReportComponent } from './sale-report/sale-report.component';
import { PurchaseReportComponent } from './purchase-report/purchase-report.component';
import { ExpenseReportComponent } from './expense-report/expense-report.component';
import { StockReportComponent } from './stock-report/stock-report.component';

const routes: Routes = [
  { path: 'sales', component: SaleReportComponent, canActivate: [AppRouteGuard] },
  { path: 'purchases', component: PurchaseReportComponent, canActivate: [AppRouteGuard] },
  { path: 'expenses', component: ExpenseReportComponent, canActivate: [AppRouteGuard] },
  { path: 'stock', component: StockReportComponent, canActivate: [AppRouteGuard] },
  { path: '', redirectTo: 'sales', pathMatch: 'full' },
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule],
})
export class ReportsRoutingModule {}
