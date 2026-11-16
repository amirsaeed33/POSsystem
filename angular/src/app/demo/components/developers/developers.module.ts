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
import { TooltipModule } from 'primeng/tooltip';
import { AppRowActionsComponent } from 'src/app/shared/components/row-actions/row-actions.component';
import { DevelopersRoutingModule } from './developers-routing.module';
import { DeveloperListComponent } from './developer-list.component';

@NgModule({
    imports: [
        CommonModule,
        FormsModule,
        DevelopersRoutingModule,
        ButtonModule,
        RippleModule,
        InputTextModule,
        InputTextareaModule,
        InputNumberModule,
        DialogModule,
        ToastModule,
        ConfirmDialogModule,
        CheckboxModule,
        TooltipModule,
        AppRowActionsComponent,
    ],
    declarations: [DeveloperListComponent],
})
export class DevelopersModule {}
