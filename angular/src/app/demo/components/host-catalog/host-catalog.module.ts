import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { RippleModule } from 'primeng/ripple';
import { InputTextModule } from 'primeng/inputtext';
import { TableModule } from 'primeng/table';
import { DialogModule } from 'primeng/dialog';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { CheckboxModule } from 'primeng/checkbox';
import { TooltipModule } from 'primeng/tooltip';
import { StyleClassModule } from 'primeng/styleclass';
import { HostCatalogRoutingModule } from './host-catalog-routing.module';
import { HostCatalogListComponent } from './host-catalog-list.component';
import { AppRowActionsComponent } from 'src/app/shared/components/row-actions/row-actions.component';

@NgModule({
    imports: [
        CommonModule,
        FormsModule,
        HostCatalogRoutingModule,
        ButtonModule,
        RippleModule,
        InputTextModule,
        TableModule,
        DialogModule,
        ToastModule,
        ConfirmDialogModule,
        CheckboxModule,
        TooltipModule,
        StyleClassModule,
        AppRowActionsComponent,
    ],
    declarations: [HostCatalogListComponent],
})
export class HostCatalogModule {}
