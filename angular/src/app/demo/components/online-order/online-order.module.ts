import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { OnlineOrderRoutingModule } from './online-order-routing.module';
import { OnlineOrderComponent } from './online-order.component';

import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { InputTextareaModule } from 'primeng/inputtextarea';
import { TagModule } from 'primeng/tag';
import { DialogModule } from 'primeng/dialog';
import { SidebarModule } from 'primeng/sidebar';
import { ToastModule } from 'primeng/toast';
import { DropdownModule } from 'primeng/dropdown';

@NgModule({
    imports: [
        CommonModule,
        FormsModule,
        OnlineOrderRoutingModule,
        ButtonModule,
        InputTextModule,
        InputTextareaModule,
        TagModule,
        DialogModule,
        SidebarModule,
        ToastModule,
        DropdownModule,
    ],
    declarations: [OnlineOrderComponent],
})
export class OnlineOrderModule {}
