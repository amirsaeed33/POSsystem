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
import { CheckboxModule } from 'primeng/checkbox';
import { AccountsRoutingModule } from './accounts-routing.module';
import { AccountListComponent } from './account-list.component';
import { AccountFormDialogComponent } from './account-form-dialog.component';

@NgModule({
    imports: [
        CommonModule,
        FormsModule,
        AccountsRoutingModule,
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
        CheckboxModule,
    ],
    declarations: [AccountListComponent, AccountFormDialogComponent],
})
export class AccountsModule {}
