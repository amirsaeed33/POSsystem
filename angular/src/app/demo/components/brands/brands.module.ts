import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { RippleModule } from 'primeng/ripple';
import { InputTextModule } from 'primeng/inputtext';
import { InputTextareaModule } from 'primeng/inputtextarea';
import { TableModule } from 'primeng/table';
import { DialogModule } from 'primeng/dialog';
import { ToastModule } from 'primeng/toast';
import { TooltipModule } from 'primeng/tooltip';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { BrandsRoutingModule } from './brands-routing.module';
import { BrandListComponent } from './brand-list.component';
import { BrandFormDialogComponent } from './brand-form-dialog.component';

@NgModule({
    imports: [
        CommonModule,
        FormsModule,
        BrandsRoutingModule,
        ButtonModule,
        RippleModule,
        InputTextModule,
        InputTextareaModule,
        TableModule,
        DialogModule,
        ToastModule,
        TooltipModule,
        ConfirmDialogModule,
    ],
    declarations: [BrandListComponent, BrandFormDialogComponent],
})
export class BrandsModule {}
