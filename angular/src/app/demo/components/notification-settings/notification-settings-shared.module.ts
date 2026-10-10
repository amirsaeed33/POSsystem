import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { RippleModule } from 'primeng/ripple';
import { InputTextModule } from 'primeng/inputtext';
import { InputSwitchModule } from 'primeng/inputswitch';
import { ToastModule } from 'primeng/toast';
import { TooltipModule } from 'primeng/tooltip';
import { TagModule } from 'primeng/tag';
import { NotificationSettingsComponent } from './notification-settings.component';

@NgModule({
    imports: [
        CommonModule,
        FormsModule,
        ButtonModule,
        RippleModule,
        InputTextModule,
        InputSwitchModule,
        ToastModule,
        TooltipModule,
        TagModule
    ],
    declarations: [NotificationSettingsComponent],
    exports: [NotificationSettingsComponent]
})
export class NotificationSettingsSharedModule {}
