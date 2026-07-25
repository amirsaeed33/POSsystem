import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { PosComponent } from './pos.component';

const routes: Routes = [
    {
        path: '',
        component: PosComponent,
        pathMatch: 'full',
    },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class PosRoutingModule {}
