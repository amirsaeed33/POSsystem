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
import { StyleClassModule } from 'primeng/styleclass';
import { InputSwitchModule } from 'primeng/inputswitch';
import { AppRowActionsComponent } from 'src/app/shared/components/row-actions/row-actions.component';
import { DepartmentListComponent } from './department-list.component';
import { DepartmentFormDialogComponent } from './department-form-dialog.component';

@NgModule({
    imports: [
        CommonModule,
        FormsModule,
        ButtonModule,
        RippleModule,
        InputTextModule,
        InputTextareaModule,
        TableModule,
        DialogModule,
        ToastModule,
        TooltipModule,
        ConfirmDialogModule,
        StyleClassModule,
        InputSwitchModule,
        AppRowActionsComponent,
    ],
    declarations: [DepartmentListComponent, DepartmentFormDialogComponent],
    exports: [DepartmentListComponent, DepartmentFormDialogComponent],
})
export class DepartmentsSharedModule {}

