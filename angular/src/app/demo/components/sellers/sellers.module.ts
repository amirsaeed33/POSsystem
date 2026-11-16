import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { RippleModule } from 'primeng/ripple';
import { InputTextModule } from 'primeng/inputtext';
import { InputTextareaModule } from 'primeng/inputtextarea';
import { DialogModule } from 'primeng/dialog';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { DropdownModule } from 'primeng/dropdown';
import { TooltipModule } from 'primeng/tooltip';
import { AppRowActionsComponent } from 'src/app/shared/components/row-actions/row-actions.component';
import { SellersRoutingModule } from './sellers-routing.module';
import { SellerListComponent } from './seller-list.component';

@NgModule({
    imports: [
        CommonModule,
        FormsModule,
        SellersRoutingModule,
        ButtonModule,
        RippleModule,
        InputTextModule,
        InputTextareaModule,
        DialogModule,
        ToastModule,
        ConfirmDialogModule,
        DropdownModule,
        TooltipModule,
        AppRowActionsComponent,
    ],
    declarations: [SellerListComponent],
})
export class SellersModule {}
