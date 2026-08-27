import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { RippleModule } from 'primeng/ripple';
import { InputTextModule } from 'primeng/inputtext';
import { InputTextareaModule } from 'primeng/inputtextarea';
import { InputNumberModule } from 'primeng/inputnumber';
import { TableModule } from 'primeng/table';
import { DialogModule } from 'primeng/dialog';
import { ToastModule } from 'primeng/toast';
import { TooltipModule } from 'primeng/tooltip';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { DropdownModule } from 'primeng/dropdown';
import { MultiSelectModule } from 'primeng/multiselect';
import { CheckboxModule } from 'primeng/checkbox';
import { ProductsRoutingModule } from './products-routing.module';
import { ProductListComponent } from './product-list.component';
import { ProductFormDialogComponent } from './product-form-dialog.component';
import { BarcodePrintDialogComponent } from './barcode-print-dialog.component';

@NgModule({
    imports: [
        CommonModule,
        FormsModule,
        ProductsRoutingModule,
        ButtonModule,
        RippleModule,
        InputTextModule,
        InputTextareaModule,
        InputNumberModule,
        TableModule,
        DialogModule,
        ToastModule,
        TooltipModule,
        ConfirmDialogModule,
        DropdownModule,
        MultiSelectModule,
        CheckboxModule,
    ],
    declarations: [ProductListComponent, ProductFormDialogComponent, BarcodePrintDialogComponent],
})
export class ProductsModule {}
