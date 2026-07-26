import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { RippleModule } from 'primeng/ripple';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { TableModule } from 'primeng/table';
import { ToastModule } from 'primeng/toast';
import { DropdownModule } from 'primeng/dropdown';
import { PosRoutingModule } from './pos-routing.module';
import { PosComponent } from './pos.component';

@NgModule({
    imports: [
        CommonModule,
        FormsModule,
        PosRoutingModule,
        ButtonModule,
        RippleModule,
        InputTextModule,
        InputNumberModule,
        TableModule,
        ToastModule,
        DropdownModule,
    ],
    declarations: [PosComponent],
})
export class PosModule {}
