import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TabViewModule } from 'primeng/tabview';
import { StaffSharedModule } from '../staff/staff-shared.module';
import { StaffAttendanceSharedModule } from '../staff-attendance/staff-attendance-shared.module';
import { StaffPayrollSharedModule } from '../staff-payroll/staff-payroll-shared.module';
import { StaffHubRoutingModule } from './staff-hub-routing.module';
import { StaffHubComponent } from './staff-hub.component';

@NgModule({
    imports: [
        CommonModule,
        TabViewModule,
        StaffSharedModule,
        StaffAttendanceSharedModule,
        StaffPayrollSharedModule,
        StaffHubRoutingModule,
    ],
    declarations: [StaffHubComponent],
})
export class StaffHubModule {}
