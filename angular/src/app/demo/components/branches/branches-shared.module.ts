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
import { MessageService } from 'primeng/api';
import { BranchListComponent } from './branch-list.component';
import { BranchFormDialogComponent } from './branch-form-dialog.component';
import { BranchEditComponent } from './branch-edit/branch-edit.component';
import { BranchCreateComponent } from './branch-create/branch-create.component';

import { TabViewModule } from 'primeng/tabview';
import { InputSwitchModule } from 'primeng/inputswitch';
import { NotificationSettingsSharedModule } from '../notification-settings/notification-settings-shared.module';
import { AppRowActionsComponent } from 'src/app/shared/components/row-actions/row-actions.component';

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
        TabViewModule,
        InputSwitchModule,
        NotificationSettingsSharedModule,
        AppRowActionsComponent,
    ],
    providers: [MessageService],
    declarations: [
        BranchListComponent,
        BranchFormDialogComponent,
        BranchEditComponent,
        BranchCreateComponent,
    ],
    exports: [BranchListComponent, BranchFormDialogComponent],
})
export class BranchesSharedModule {}
