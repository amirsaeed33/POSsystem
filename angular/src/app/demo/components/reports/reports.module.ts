import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { RippleModule } from 'primeng/ripple';
import { InputTextModule } from 'primeng/inputtext';
import { TableModule } from 'primeng/table';
import { ToastModule } from 'primeng/toast';
import { TagModule } from 'primeng/tag';
import { TooltipModule } from 'primeng/tooltip';
import { ReportsRoutingModule } from './reports-routing.module';
import { SaleReportComponent } from './sale-report.component';
import { PurchaseReportComponent } from './purchase-report.component';
import { ExpenseReportComponent } from './expense-report.component';
import { StockReportComponent } from './stock-report.component';
import { ProductProfitReportComponent } from './product-profit-report.component';
import { InvoicePrintModule } from '../invoices/invoice-print.module';

@NgModule({
    imports: [
        CommonModule,
        FormsModule,
        ReportsRoutingModule,
        InvoicePrintModule,
        ButtonModule,
        RippleModule,
        InputTextModule,
        TableModule,
        ToastModule,
        TagModule,
        TooltipModule,
    ],
    declarations: [
        SaleReportComponent,
        PurchaseReportComponent,
        ExpenseReportComponent,
        StockReportComponent,
        ProductProfitReportComponent,
    ],
})
export class ReportsModule {}
