import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormsModule } from '@angular/forms';
import { RippleModule } from 'primeng/ripple';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { MultiSelectModule } from 'primeng/multiselect';
import { CheckboxModule } from 'primeng/checkbox';
import { ToastModule } from 'primeng/toast';
import { ProfileCreateComponent } from './profilecreate.component';
import { ProfileCreateRoutingModule } from './profilecreate-routing.module';

@NgModule({
	imports: [
		CommonModule,
		ReactiveFormsModule,
		FormsModule,
		ProfileCreateRoutingModule,
		ButtonModule,
		RippleModule,
		InputTextModule,
		MultiSelectModule,
		CheckboxModule,
		ToastModule
	],
	declarations: [ProfileCreateComponent]
})
export class ProfileCreateModule { }
