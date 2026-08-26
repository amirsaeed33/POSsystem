import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { TableModule } from 'primeng/table';
import { InputNumberModule } from 'primeng/inputnumber';
import { ChartModule } from 'primeng/chart';
import { RippleModule } from 'primeng/ripple';
import { TooltipModule } from 'primeng/tooltip';
import { SaaSDashboardComponent } from './saas.dashboard.component';
import { SaaSDashboardRoutingModule } from './saas.dashboard-routing.module';
import { FormsModule } from '@angular/forms';
import { DropdownModule } from 'primeng/dropdown';
import { SelectButtonModule } from 'primeng/selectbutton';
import { AvatarGroupModule } from 'primeng/avatargroup';
import { AvatarModule } from 'primeng/avatar';
import { ProgressBarModule } from 'primeng/progressbar';
import { PanelModule } from 'primeng/panel';
import { TabViewModule } from 'primeng/tabview';
import { TagModule } from 'primeng/tag';

@NgModule({
    imports: [
        CommonModule,
        RouterModule,
        ButtonModule,
        RippleModule,
        TagModule,
        TooltipModule,
        TableModule,
        InputNumberModule,
        ChartModule,
        FormsModule,
        DropdownModule,
        SelectButtonModule,
        AvatarGroupModule,
        AvatarModule,
        ProgressBarModule,
        PanelModule,
        TabViewModule,
        TagModule,
        SaaSDashboardRoutingModule
    ],
    declarations: [SaaSDashboardComponent]
})
export class SaaSDashboardModule { }
