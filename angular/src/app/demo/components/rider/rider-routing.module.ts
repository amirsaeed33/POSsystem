import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { RiderDashboardComponent } from './dashboard/rider-dashboard.component';
import { RiderHistoryComponent } from './history/rider-history.component';

const routes: Routes = [
    {
        path: '',
        redirectTo: 'dashboard',
        pathMatch: 'full',
    },
    {
        path: 'dashboard',
        component: RiderDashboardComponent,
        data: { breadcrumb: 'My Dashboard' },
    },
    {
        path: 'history',
        component: RiderHistoryComponent,
        data: { breadcrumb: 'Order History' },
    },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class RiderRoutingModule {}
