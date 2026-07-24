import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { BrandsComponent } from './brands.component';

const routes: Routes = [
    {
        path: '',
        component: BrandsComponent,
        pathMatch: 'full',
    },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class BrandsRoutingModule {}
