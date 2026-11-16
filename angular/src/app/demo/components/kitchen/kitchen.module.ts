import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { RippleModule } from 'primeng/ripple';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { DialogModule } from 'primeng/dialog';
import { TooltipModule } from 'primeng/tooltip';
import { InputNumberModule } from 'primeng/inputnumber';
import { InputTextModule } from 'primeng/inputtext';
import { RadioButtonModule } from 'primeng/radiobutton';
import { KitchenRoutingModule } from './kitchen-routing.module';
import { KitchenComponent } from './kitchen.component';

@NgModule({
    imports: [
        CommonModule,
        FormsModule,
        KitchenRoutingModule,
        ButtonModule,
        RippleModule,
        ToastModule,
        ConfirmDialogModule,
        DialogModule,
        TooltipModule,
        InputNumberModule,
        InputTextModule,
        RadioButtonModule,
    ],
    declarations: [KitchenComponent],
})
export class KitchenModule {}
