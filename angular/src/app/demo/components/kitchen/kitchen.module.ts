import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ButtonModule } from 'primeng/button';
import { RippleModule } from 'primeng/ripple';
import { ToastModule } from 'primeng/toast';
import { KitchenRoutingModule } from './kitchen-routing.module';
import { KitchenComponent } from './kitchen.component';

@NgModule({
    imports: [CommonModule, KitchenRoutingModule, ButtonModule, RippleModule, ToastModule],
    declarations: [KitchenComponent],
})
export class KitchenModule {}
