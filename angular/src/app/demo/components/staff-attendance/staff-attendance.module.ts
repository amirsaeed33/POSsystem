import { NgModule } from '@angular/core';
import { StaffAttendanceRoutingModule } from './staff-attendance-routing.module';
import { StaffAttendanceSharedModule } from './staff-attendance-shared.module';

@NgModule({
    imports: [StaffAttendanceSharedModule, StaffAttendanceRoutingModule],
})
export class StaffAttendanceModule {}
