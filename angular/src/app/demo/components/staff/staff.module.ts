import { NgModule } from '@angular/core';
import { StaffRoutingModule } from './staff-routing.module';
import { StaffSharedModule } from './staff-shared.module';

@NgModule({
    imports: [StaffSharedModule, StaffRoutingModule],
})
export class StaffModule {}
