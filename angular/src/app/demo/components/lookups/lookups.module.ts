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
import { CheckboxModule } from 'primeng/checkbox';
import { LookUpsRoutingModule } from './lookups-routing.module';
import { LookUpListComponent } from './lookup-list.component';
import { LookUpFormDialogComponent } from './lookup-form-dialog.component';

@NgModule({
    imports: [
        CommonModule,
        FormsModule,
        LookUpsRoutingModule,
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
        CheckboxModule,
    ],
    declarations: [LookUpListComponent, LookUpFormDialogComponent],
})
export class LookUpsModule {}
