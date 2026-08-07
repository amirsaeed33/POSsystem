import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { RippleModule } from 'primeng/ripple';
import { InputTextModule } from 'primeng/inputtext';
import { InputTextareaModule } from 'primeng/inputtextarea';
import { InputNumberModule } from 'primeng/inputnumber';
import { CheckboxModule } from 'primeng/checkbox';
import { DialogModule } from 'primeng/dialog';
import { ToastModule } from 'primeng/toast';
import { TooltipModule } from 'primeng/tooltip';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { StyleClassModule } from 'primeng/styleclass';
import { DropdownModule } from 'primeng/dropdown';
import { BranchListComponent } from './branch-list.component';
import { BranchFormDialogComponent } from './branch-form-dialog.component';

@NgModule({
    imports: [
        CommonModule,
        FormsModule,
        ButtonModule,
        RippleModule,
        InputTextModule,
        InputTextareaModule,
        InputNumberModule,
        CheckboxModule,
        DialogModule,
        ToastModule,
        TooltipModule,
        ConfirmDialogModule,
        StyleClassModule,
        DropdownModule,
    ],
    declarations: [BranchListComponent, BranchFormDialogComponent],
    exports: [BranchListComponent, BranchFormDialogComponent],
})
export class BranchesSharedModule {}
