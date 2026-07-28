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
import { CompanyProfilesRoutingModule } from './company-profiles-routing.module';
import { CompanyProfileListComponent } from './company-profile-list.component';
import { CompanyProfileFormDialogComponent } from './company-profile-form-dialog.component';
import { CompanyProfileReceiptPreviewDialogComponent } from './company-profile-receipt-preview-dialog.component';

@NgModule({
    imports: [
        CommonModule,
        FormsModule,
        CompanyProfilesRoutingModule,
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
    declarations: [
        CompanyProfileListComponent,
        CompanyProfileFormDialogComponent,
        CompanyProfileReceiptPreviewDialogComponent,
    ],
})
export class CompanyProfilesModule {}
