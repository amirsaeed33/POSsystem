import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { RippleModule } from 'primeng/ripple';
import { InputTextModule } from 'primeng/inputtext';
import { TableModule } from 'primeng/table';
import { ToastModule } from 'primeng/toast';
import { AdminLogRoutingModule } from './admin-log-routing.module';
import { AdminLogComponent } from './admin-log.component';

@NgModule({
    imports: [
        CommonModule,
        FormsModule,
        AdminLogRoutingModule,
        ButtonModule,
        RippleModule,
        InputTextModule,
        TableModule,
        ToastModule,
    ],
    declarations: [AdminLogComponent],
})
export class AdminLogModule {}
