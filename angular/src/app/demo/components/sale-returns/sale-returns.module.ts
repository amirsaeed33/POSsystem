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
import { SaleReturnsRoutingModule } from './sale-returns-routing.module';
import { SaleReturnListComponent } from './sale-return-list.component';
import { SaleReturnFormDialogComponent } from './sale-return-form-dialog.component';
import { SaleReturnViewDialogComponent } from './sale-return-view-dialog.component';

@NgModule({
    imports: [
        CommonModule,
        FormsModule,
        SaleReturnsRoutingModule,
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
    ],
    declarations: [
        SaleReturnListComponent,
        SaleReturnFormDialogComponent,
        SaleReturnViewDialogComponent,
    ],
    exports: [SaleReturnFormDialogComponent],
})
export class SaleReturnsModule {}
