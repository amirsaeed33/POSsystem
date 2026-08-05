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
import { DropdownModule } from 'primeng/dropdown';
import { TagModule } from 'primeng/tag';
import { StaffAttendanceListComponent } from './staff-attendance-list.component';
import { StaffAttendanceFormDialogComponent } from './staff-attendance-form-dialog.component';

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
        DropdownModule,
        TagModule,
    ],
    declarations: [
        StaffAttendanceListComponent,
        StaffAttendanceFormDialogComponent,
    ],
    exports: [
        StaffAttendanceListComponent,
        StaffAttendanceFormDialogComponent,
    ],
})
export class StaffAttendanceSharedModule {}
