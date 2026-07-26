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
import { SalesRoutingModule } from './sales-routing.module';
import { SaleListComponent } from './sale-list.component';
import { SaleFormDialogComponent } from './sale-form-dialog.component';
import { SaleViewDialogComponent } from './sale-view-dialog.component';

@NgModule({
    imports: [
        CommonModule,
        FormsModule,
        SalesRoutingModule,
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
        SaleListComponent,
        SaleFormDialogComponent,
        SaleViewDialogComponent,
    ],
})
export class SalesModule {}
