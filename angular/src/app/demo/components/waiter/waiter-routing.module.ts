import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { WaiterDashboardComponent } from './dashboard/waiter-dashboard.component';

const routes: Routes = [
    {
        path: '',
        redirectTo: 'dashboard',
        pathMatch: 'full',
    },
    {
        path: 'dashboard',
        component: WaiterDashboardComponent,
        data: { breadcrumb: 'My Dashboard' },
    },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class WaiterRoutingModule {}
