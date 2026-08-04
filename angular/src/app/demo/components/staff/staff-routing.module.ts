import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';
import { StaffListComponent } from './staff-list.component';

@NgModule({
    imports: [
        RouterModule.forChild([
            {
                path: '',
                component: StaffListComponent,
                data: { breadcrumb: 'Staff' },
            },
        ]),
    ],
    exports: [RouterModule],
})
export class StaffRoutingModule {}
