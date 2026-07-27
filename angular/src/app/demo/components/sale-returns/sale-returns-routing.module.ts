import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';
import { SaleReturnListComponent } from './sale-return-list.component';

@NgModule({
    imports: [
        RouterModule.forChild([
            {
                path: '',
                component: SaleReturnListComponent,
                data: { breadcrumb: 'Sale Returns' },
            },
        ]),
    ],
    exports: [RouterModule],
})
export class SaleReturnsRoutingModule {}
