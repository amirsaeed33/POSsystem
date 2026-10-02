import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { InputTextModule } from 'primeng/inputtext';
import { DropdownModule } from 'primeng/dropdown';
import { DialogModule } from 'primeng/dialog';
import { ButtonModule } from 'primeng/button';
import { CustomerFormDialogComponent } from './customer-form-dialog.component';

@NgModule({
    imports: [
        CommonModule,
        FormsModule,
        InputTextModule,
        DropdownModule,
        DialogModule,
        ButtonModule
    ],
    declarations: [CustomerFormDialogComponent],
    exports: [CustomerFormDialogComponent]
})
export class CustomerSharedModule {}
