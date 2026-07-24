import { NgModule } from '@angular/core';
import { SharedModule } from '@shared/shared.module';
import { SalesRoutingModule } from './sales-routing.module';
import { SalesComponent } from './sales.component';
import { CreateSaleDialogComponent } from './create-sale/create-sale-dialog.component';
import { CreateSaleReturnDialogComponent } from './create-sale-return/create-sale-return-dialog.component';
import { ViewSaleDialogComponent } from './view-sale/view-sale-dialog.component';
import { PrintSaleInvoiceDialogComponent } from './print-sale-invoice/print-sale-invoice-dialog.component';

@NgModule({
    declarations: [
        SalesComponent,
        CreateSaleDialogComponent,
        CreateSaleReturnDialogComponent,
        ViewSaleDialogComponent,
        PrintSaleInvoiceDialogComponent,
    ],
    imports: [SharedModule, SalesRoutingModule],
})
export class SalesModule {}
