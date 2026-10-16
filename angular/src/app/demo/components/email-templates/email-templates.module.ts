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
import { CheckboxModule } from 'primeng/checkbox';
import { StyleClassModule } from 'primeng/styleclass';
import { EmailTemplatesRoutingModule } from './email-templates-routing.module';
import { EmailTemplateListComponent } from './email-template-list.component';
import { EmailTemplateFormDialogComponent } from './email-template-form-dialog.component';
import { AppRowActionsComponent } from 'src/app/shared/components/row-actions/row-actions.component';

@NgModule({
    imports: [
        CommonModule,
        FormsModule,
        EmailTemplatesRoutingModule,
        ButtonModule,
        RippleModule,
        InputTextModule,
        InputTextareaModule,
        TableModule,
        DialogModule,
        ToastModule,
        TooltipModule,
        ConfirmDialogModule,
        CheckboxModule,
        StyleClassModule,
        AppRowActionsComponent,
    ],
    declarations: [EmailTemplateListComponent, EmailTemplateFormDialogComponent],
})
export class EmailTemplatesModule {}
