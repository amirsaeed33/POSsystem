import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';
import { PosComponent } from './pos.component';

@NgModule({
    imports: [
        RouterModule.forChild([
            {
                path: '',
                component: PosComponent,
                data: { breadcrumb: 'POS' },
            },
        ]),
    ],
    exports: [RouterModule],
})
export class PosRoutingModule {}
