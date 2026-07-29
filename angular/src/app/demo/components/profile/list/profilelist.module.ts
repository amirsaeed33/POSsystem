import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { RippleModule } from 'primeng/ripple';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { TableModule } from 'primeng/table';
import { ProgressBarModule } from 'primeng/progressbar';
import { TagModule } from 'primeng/tag';
import { ToastModule } from 'primeng/toast';
import { TooltipModule } from 'primeng/tooltip';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { DialogModule } from 'primeng/dialog';
import { MultiSelectModule } from 'primeng/multiselect';
import { CheckboxModule } from 'primeng/checkbox';
import { ProfileListComponent } from './profilelist.component';
import { ResetPasswordDialogComponent } from './reset-password-dialog.component';
import { UserFormDialogComponent } from './user-form-dialog.component';
import { ProfileListRoutingModule } from './profilelist-routing.module';

@NgModule({
	imports: [
		CommonModule,
		FormsModule,
		ReactiveFormsModule,
		ProfileListRoutingModule,
		RippleModule,
		ButtonModule,
		InputTextModule,
		TableModule,
		ProgressBarModule,
		TagModule,
		ToastModule,
		TooltipModule,
		ConfirmDialogModule,
		DialogModule,
		MultiSelectModule,
		CheckboxModule
	],
	declarations: [
		ProfileListComponent,
		ResetPasswordDialogComponent,
		UserFormDialogComponent
	]
})
export class ProfileListModule { }
