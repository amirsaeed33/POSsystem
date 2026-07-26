import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';
import { SupplierListComponent } from './supplier-list.component';

@NgModule({
    imports: [
        RouterModule.forChild([
            {
                path: '',
                component: SupplierListComponent,
                data: { breadcrumb: 'Suppliers' },
            },
        ]),
    ],
    exports: [RouterModule],
})
export class SuppliersRoutingModule {}
