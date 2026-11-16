import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { RippleModule } from 'primeng/ripple';
import { InputTextModule } from 'primeng/inputtext';
import { InputTextareaModule } from 'primeng/inputtextarea';
import { TableModule } from 'primeng/table';
import { DialogModule } from 'primeng/dialog';
import { CheckboxModule } from 'primeng/checkbox';
import { ToastModule } from 'primeng/toast';
import { TooltipModule } from 'primeng/tooltip';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { StyleClassModule } from 'primeng/styleclass';
import { AppRowActionsComponent } from 'src/app/shared/components/row-actions/row-actions.component';
import { SuppliersRoutingModule } from './suppliers-routing.module';
import { SupplierListComponent } from './supplier-list.component';
import { SupplierFormDialogComponent } from './supplier-form-dialog.component';
import { SupplierAssignProductsDialogComponent } from './supplier-assign-products-dialog.component';

@NgModule({
    imports: [
        CommonModule,
        FormsModule,
        SuppliersRoutingModule,
        ButtonModule,
        RippleModule,
        InputTextModule,
        InputTextareaModule,
        TableModule,
        DialogModule,
        CheckboxModule,
        ToastModule,
        TooltipModule,
        ConfirmDialogModule,
        StyleClassModule,
        AppRowActionsComponent,
    ],
    declarations: [
        SupplierListComponent,
        SupplierFormDialogComponent,
        SupplierAssignProductsDialogComponent,
    ],
})
export class SuppliersModule {}
