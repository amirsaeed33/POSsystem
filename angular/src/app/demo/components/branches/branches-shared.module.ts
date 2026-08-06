import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { RippleModule } from 'primeng/ripple';
import { InputTextModule } from 'primeng/inputtext';
import { InputTextareaModule } from 'primeng/inputtextarea';
import { CheckboxModule } from 'primeng/checkbox';
import { DialogModule } from 'primeng/dialog';
import { ToastModule } from 'primeng/toast';
import { TooltipModule } from 'primeng/tooltip';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { MenuModule } from 'primeng/menu';
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
        CheckboxModule,
        DialogModule,
        ToastModule,
        TooltipModule,
        ConfirmDialogModule,
        MenuModule,
    ],
    declarations: [BranchListComponent, BranchFormDialogComponent],
    exports: [BranchListComponent, BranchFormDialogComponent],
})
export class BranchesSharedModule {}
