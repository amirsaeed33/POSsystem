import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormsModule } from '@angular/forms';
import { RippleModule } from 'primeng/ripple';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { InputTextareaModule } from 'primeng/inputtextarea';
import { CheckboxModule } from 'primeng/checkbox';
import { ToastModule } from 'primeng/toast';
import { TooltipModule } from 'primeng/tooltip';
import { RoleCreateComponent } from './rolecreate.component';
import { RoleCreateRoutingModule } from './rolecreate-routing.module';

@NgModule({
	imports: [
		CommonModule,
		ReactiveFormsModule,
		FormsModule,
		RoleCreateRoutingModule,
		ButtonModule,
		RippleModule,
		InputTextModule,
		InputTextareaModule,
		CheckboxModule,
		ToastModule,
		TooltipModule
	],
	declarations: [RoleCreateComponent]
})
export class RoleCreateModule { }

