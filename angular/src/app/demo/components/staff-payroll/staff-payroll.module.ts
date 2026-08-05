import { NgModule } from '@angular/core';
import { StaffPayrollRoutingModule } from './staff-payroll-routing.module';
import { StaffPayrollSharedModule } from './staff-payroll-shared.module';

@NgModule({
    imports: [StaffPayrollSharedModule, StaffPayrollRoutingModule],
})
export class StaffPayrollModule {}
