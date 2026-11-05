import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { KitchenComponent } from './kitchen.component';

const routes: Routes = [
    {
        path: '',
        component: KitchenComponent,
        data: { breadcrumb: 'Kitchen' },
    },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class KitchenRoutingModule {}
