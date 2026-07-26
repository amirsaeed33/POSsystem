import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';
import { UnitListComponent } from './unit-list.component';

@NgModule({
    imports: [
        RouterModule.forChild([
            {
                path: '',
                component: UnitListComponent,
                data: { breadcrumb: 'Units' },
            },
        ]),
    ],
    exports: [RouterModule],
})
export class UnitsRoutingModule {}
