import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RippleModule } from 'primeng/ripple';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { TableModule } from 'primeng/table';
import { ProgressBarModule } from 'primeng/progressbar';
import { TagModule } from 'primeng/tag';
import { ToastModule } from 'primeng/toast';
import { TooltipModule } from 'primeng/tooltip';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { RoleListComponent } from './rolelist.component';
import { RoleListRoutingModule } from './rolelist-routing.module';

@NgModule({
	imports: [
		CommonModule,
		RoleListRoutingModule,
		RippleModule,
		ButtonModule,
		InputTextModule,
		TableModule,
		ProgressBarModule,
		TagModule,
		ToastModule,
		TooltipModule,
		ConfirmDialogModule
	],
	declarations: [RoleListComponent]
})
export class RoleListModule { }

