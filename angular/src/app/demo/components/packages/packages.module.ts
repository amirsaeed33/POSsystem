import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { RippleModule } from 'primeng/ripple';
import { InputTextModule } from 'primeng/inputtext';
import { InputTextareaModule } from 'primeng/inputtextarea';
import { InputNumberModule } from 'primeng/inputnumber';
import { DialogModule } from 'primeng/dialog';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { CheckboxModule } from 'primeng/checkbox';
import { AppRowActionsComponent } from 'src/app/shared/components/row-actions/row-actions.component';
import { PackagesRoutingModule } from './packages-routing.module';
import { PackageListComponent } from './package-list.component';

@NgModule({
    imports: [
        CommonModule,
        FormsModule,
        PackagesRoutingModule,
        ButtonModule,
        RippleModule,
        InputTextModule,
        InputTextareaModule,
        InputNumberModule,
        DialogModule,
        ToastModule,
        ConfirmDialogModule,
        CheckboxModule,
        AppRowActionsComponent,
    ],
    declarations: [PackageListComponent],
})
export class PackagesModule {}
