import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { CheckboxModule } from 'primeng/checkbox';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { DialogModule } from 'primeng/dialog';
import { DropdownModule } from 'primeng/dropdown';
import { InputTextModule } from 'primeng/inputtext';
import { InputTextareaModule } from 'primeng/inputtextarea';
import { TableModule } from 'primeng/table';
import { TabViewModule } from 'primeng/tabview';
import { ToastModule } from 'primeng/toast';
import { SubscriptionsComponent } from './subscriptions.component';

@NgModule({
    imports: [
        CommonModule,
        FormsModule,
        RouterModule.forChild([{ path: '', component: SubscriptionsComponent }]),
        ButtonModule,
        CheckboxModule,
        ConfirmDialogModule,
        DialogModule,
        DropdownModule,
        InputTextModule,
        InputTextareaModule,
        TableModule,
        TabViewModule,
        ToastModule,
    ],
    declarations: [SubscriptionsComponent],
})
export class SubscriptionsModule {}
