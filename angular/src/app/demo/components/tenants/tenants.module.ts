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
import { CalendarModule } from 'primeng/calendar';
import { DropdownModule } from 'primeng/dropdown';
import { InputNumberModule } from 'primeng/inputnumber';
import { StyleClassModule } from 'primeng/styleclass';
import { AppRowActionsComponent } from 'src/app/shared/components/row-actions/row-actions.component';
import { TenantsRoutingModule } from './tenants-routing.module';
import { TenantListComponent } from './tenant-list.component';
import { TenantFormDialogComponent } from './tenant-form-dialog.component';
import { TenantSubscriptionDialogComponent } from './tenant-subscription-dialog.component';

@NgModule({
    imports: [
        CommonModule,
        FormsModule,
        TenantsRoutingModule,
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
        CalendarModule,
        DropdownModule,
        InputNumberModule,
        StyleClassModule,
        AppRowActionsComponent,
    ],
    declarations: [TenantListComponent, TenantFormDialogComponent, TenantSubscriptionDialogComponent],
})
export class TenantsModule {}
