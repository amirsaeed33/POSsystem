import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { RippleModule } from 'primeng/ripple';
import { TooltipModule } from 'primeng/tooltip';
import { DialogModule } from 'primeng/dialog';
import { ToastModule } from 'primeng/toast';
import { InputTextModule } from 'primeng/inputtext';
import { InputTextareaModule } from 'primeng/inputtextarea';
import { LandingComponent } from './landing.component';
import { LandingRoutingModule } from './landing-routing.module';

@NgModule({
    declarations: [LandingComponent],
    imports: [
        CommonModule,
        FormsModule,
        RouterModule,
        LandingRoutingModule,
        ButtonModule,
        RippleModule,
        TooltipModule,
        DialogModule,
        ToastModule,
        InputTextModule,
        InputTextareaModule,
    ],
})
export class LandingModule {}
