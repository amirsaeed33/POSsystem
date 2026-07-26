import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';
import { BrandListComponent } from './brand-list.component';

@NgModule({
    imports: [
        RouterModule.forChild([
            {
                path: '',
                component: BrandListComponent,
                data: { breadcrumb: 'Brands' },
            },
        ]),
    ],
    exports: [RouterModule],
})
export class BrandsRoutingModule {}
