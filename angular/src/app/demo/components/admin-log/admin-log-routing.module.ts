import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';
import { AdminLogComponent } from './admin-log.component';

@NgModule({
    imports: [
        RouterModule.forChild([
            { path: '', component: AdminLogComponent, data: { breadcrumb: 'Logged in' } },
        ]),
    ],
    exports: [RouterModule],
})
export class AdminLogRoutingModule {}
