import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { RippleModule } from 'primeng/ripple';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { InputTextareaModule } from 'primeng/inputtextarea';
import { InputSwitchModule } from 'primeng/inputswitch';
import { TableModule } from 'primeng/table';
import { DialogModule } from 'primeng/dialog';
import { ToastModule } from 'primeng/toast';
import { TooltipModule } from 'primeng/tooltip';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { DropdownModule } from 'primeng/dropdown';
import { TagModule } from 'primeng/tag';
import { DeliveryAreasRoutingModule } from './delivery-areas-routing.module';
import { DeliveryAreasComponent } from './delivery-areas.component';

@NgModule({
    imports: [
        CommonModule,
        FormsModule,
        DeliveryAreasRoutingModule,
        ButtonModule,
        RippleModule,
        InputTextModule,
        InputNumberModule,
        InputTextareaModule,
        InputSwitchModule,
        TableModule,
        DialogModule,
        ToastModule,
        TooltipModule,
        ConfirmDialogModule,
        DropdownModule,
        TagModule,
    ],
    declarations: [DeliveryAreasComponent],
})
export class DeliveryAreasModule {}
