import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';
import { StaffPayrollListComponent } from './staff-payroll-list.component';

@NgModule({
    imports: [
        RouterModule.forChild([
            {
                path: '',
                component: StaffPayrollListComponent,
                data: { breadcrumb: 'Payroll' },
            },
        ]),
    ],
    exports: [RouterModule],
})
export class StaffPayrollRoutingModule {}
