import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ButtonModule } from 'primeng/button';
import { RippleModule } from 'primeng/ripple';
import { AppConfigModule } from 'src/app/layout/config/config.module';
import { ActivateBranchRoutingModule } from './activate-branch-routing.module';
import { ActivateBranchComponent } from './activate-branch.component';

@NgModule({
    imports: [
        CommonModule,
        ActivateBranchRoutingModule,
        ButtonModule,
        RippleModule,
        AppConfigModule
    ],
    declarations: [ActivateBranchComponent]
})
export class ActivateBranchModule {}
