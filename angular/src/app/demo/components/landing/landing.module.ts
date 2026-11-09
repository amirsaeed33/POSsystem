import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { RippleModule } from 'primeng/ripple';
import { TooltipModule } from 'primeng/tooltip';
import { LandingComponent } from './landing.component';
import { LandingRoutingModule } from './landing-routing.module';

@NgModule({
    declarations: [LandingComponent],
    imports: [
        CommonModule,
        RouterModule,
        LandingRoutingModule,
        ButtonModule,
        RippleModule,
        TooltipModule,
    ],
})
export class LandingModule {}
