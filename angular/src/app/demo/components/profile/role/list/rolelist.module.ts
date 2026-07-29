import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { RippleModule } from 'primeng/ripple';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { InputTextareaModule } from 'primeng/inputtextarea';
import { TableModule } from 'primeng/table';
import { ProgressBarModule } from 'primeng/progressbar';
import { TagModule } from 'primeng/tag';
import { ToastModule } from 'primeng/toast';
import { TooltipModule } from 'primeng/tooltip';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { DialogModule } from 'primeng/dialog';
import { CheckboxModule } from 'primeng/checkbox';
import { RoleListComponent } from './rolelist.component';
import { RoleFormDialogComponent } from './role-form-dialog.component';
import { RoleListRoutingModule } from './rolelist-routing.module';

@NgModule({
	imports: [
		CommonModule,
		FormsModule,
		ReactiveFormsModule,
		RoleListRoutingModule,
		RippleModule,
		ButtonModule,
		InputTextModule,
		InputTextareaModule,
		TableModule,
		ProgressBarModule,
		TagModule,
		ToastModule,
		TooltipModule,
		ConfirmDialogModule,
		DialogModule,
		CheckboxModule
	],
	declarations: [RoleListComponent, RoleFormDialogComponent]
})
export class RoleListModule { }
