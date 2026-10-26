import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ConfirmationService, MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { InputTextareaModule } from 'primeng/inputtextarea';
import { DialogModule } from 'primeng/dialog';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { PaginatorModule } from 'primeng/paginator';
import { TagModule } from 'primeng/tag';
import { TooltipModule } from 'primeng/tooltip';
import { CardModule } from 'primeng/card';
import { RiderRoutingModule } from './rider-routing.module';
import { RiderDashboardComponent } from './dashboard/rider-dashboard.component';
import { RiderHistoryComponent } from './history/rider-history.component';

@NgModule({
    declarations: [
        RiderDashboardComponent,
        RiderHistoryComponent,
    ],
    imports: [
        CommonModule,
        FormsModule,
        RiderRoutingModule,
        ButtonModule,
        InputTextModule,
        InputTextareaModule,
        DialogModule,
        ToastModule,
        ConfirmDialogModule,
        PaginatorModule,
        TagModule,
        TooltipModule,
        CardModule,
    ],
    providers: [
        MessageService,
        ConfirmationService,
    ],
})
export class RiderModule {}
