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
import { StyleClassModule } from 'primeng/styleclass';
import { GroceryPurchasesRoutingModule } from './grocery-purchases-routing.module';
import { GroceryPurchaseListComponent } from './grocery-purchase-list.component';
import { GroceryPurchaseDialogComponent } from './grocery-purchase-dialog.component';

@NgModule({
    imports: [
        CommonModule,
        FormsModule,
        GroceryPurchasesRoutingModule,
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
        StyleClassModule,
    ],
    declarations: [
        GroceryPurchaseListComponent,
        GroceryPurchaseDialogComponent,
    ],
})
export class GroceryPurchasesModule {}
