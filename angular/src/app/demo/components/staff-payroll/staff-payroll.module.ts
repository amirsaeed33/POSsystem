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
import { TagModule } from 'primeng/tag';
import { StaffPayrollRoutingModule } from './staff-payroll-routing.module';
import { StaffPayrollListComponent } from './staff-payroll-list.component';
import { StaffPayrollFormDialogComponent } from './staff-payroll-form-dialog.component';

@NgModule({
    imports: [
        CommonModule,
        FormsModule,
        StaffPayrollRoutingModule,
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
        TagModule,
    ],
    declarations: [StaffPayrollListComponent, StaffPayrollFormDialogComponent],
})
export class StaffPayrollModule {}
