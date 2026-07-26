import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';
import { SaleListComponent } from './sale-list.component';

@NgModule({
    imports: [
        RouterModule.forChild([
            {
                path: '',
                component: SaleListComponent,
                data: { breadcrumb: 'Sales' },
            },
        ]),
    ],
    exports: [RouterModule],
})
export class SalesRoutingModule {}
