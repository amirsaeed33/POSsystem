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
import { TagModule } from 'primeng/tag';
import { CustomerOrdersRoutingModule } from './customer-orders-routing.module';
import { CustomerOrderListComponent } from './customer-order-list.component';
import { CustomerOrderFormDialogComponent } from './customer-order-form-dialog.component';
import { CustomerOrderViewDialogComponent } from './customer-order-view-dialog.component';

@NgModule({
    imports: [
        CommonModule,
        FormsModule,
        CustomerOrdersRoutingModule,
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
        TagModule,
    ],
    declarations: [
        CustomerOrderListComponent,
        CustomerOrderFormDialogComponent,
        CustomerOrderViewDialogComponent,
    ],
})
export class CustomerOrdersModule {}
