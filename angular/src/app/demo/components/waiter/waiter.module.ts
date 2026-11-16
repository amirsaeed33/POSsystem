import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ButtonModule } from 'primeng/button';
import { ToastModule } from 'primeng/toast';
import { WaiterRoutingModule } from './waiter-routing.module';
import { WaiterDashboardComponent } from './dashboard/waiter-dashboard.component';

@NgModule({
    declarations: [WaiterDashboardComponent],
    imports: [CommonModule, WaiterRoutingModule, ButtonModule, ToastModule],
})
export class WaiterModule {}
