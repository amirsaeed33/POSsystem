import { NgModule } from '@angular/core';
import { NotificationSettingsRoutingModule } from './notification-settings-routing.module';
import { NotificationSettingsSharedModule } from './notification-settings-shared.module';

@NgModule({
    imports: [
        NotificationSettingsSharedModule,
        NotificationSettingsRoutingModule
    ]
})
export class NotificationSettingsModule {}
