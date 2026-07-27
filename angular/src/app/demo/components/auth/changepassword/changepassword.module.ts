import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule } from '@angular/forms';
import { ChangePasswordRoutingModule } from './changepassword-routing.module';
import { ChangePasswordComponent } from './changepassword.component';
import { InputTextModule } from 'primeng/inputtext';
import { ButtonModule } from 'primeng/button';
import { RippleModule } from 'primeng/ripple';
import { ToastModule } from 'primeng/toast';
import { AppConfigModule } from 'src/app/layout/config/config.module';

@NgModule({
    imports: [
        CommonModule,
        ReactiveFormsModule,
        ChangePasswordRoutingModule,
        InputTextModule,
        ButtonModule,
        RippleModule,
        ToastModule,
        AppConfigModule,
    ],
    declarations: [ChangePasswordComponent],
})
export class ChangePasswordModule {}
