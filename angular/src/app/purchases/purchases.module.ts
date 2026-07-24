import { NgModule } from '@angular/core';
import { SharedModule } from '@shared/shared.module';
import { PurchasesRoutingModule } from './purchases-routing.module';
import { PurchasesComponent } from './purchases.component';
import { CreatePurchaseDialogComponent } from './create-purchase/create-purchase-dialog.component';
import { CreatePurchaseReturnDialogComponent } from './create-purchase-return/create-purchase-return-dialog.component';
import { ViewPurchaseDialogComponent } from './view-purchase/view-purchase-dialog.component';
import { PrintPurchaseInvoiceDialogComponent } from './print-purchase-invoice/print-purchase-invoice-dialog.component';

@NgModule({
    declarations: [
        PurchasesComponent,
        CreatePurchaseDialogComponent,
        CreatePurchaseReturnDialogComponent,
        ViewPurchaseDialogComponent,
        PrintPurchaseInvoiceDialogComponent,
    ],
    imports: [SharedModule, PurchasesRoutingModule],
})
export class PurchasesModule {}
