import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';
import { StaffAttendanceListComponent } from './staff-attendance-list.component';

@NgModule({
    imports: [
        RouterModule.forChild([
            {
                path: '',
                component: StaffAttendanceListComponent,
                data: { breadcrumb: 'Attendance' },
            },
        ]),
    ],
    exports: [RouterModule],
})
export class StaffAttendanceRoutingModule {}
