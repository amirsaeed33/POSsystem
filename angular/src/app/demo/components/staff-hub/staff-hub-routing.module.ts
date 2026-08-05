import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';
import { StaffHubComponent } from './staff-hub.component';

@NgModule({
    imports: [
        RouterModule.forChild([
            {
                path: '',
                component: StaffHubComponent,
                data: { breadcrumb: 'Staff' },
            },
        ]),
    ],
    exports: [RouterModule],
})
export class StaffHubRoutingModule {}
