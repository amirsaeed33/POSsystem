import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';
import { BranchListComponent } from './branch-list.component';

@NgModule({
    imports: [
        RouterModule.forChild([
            {
                path: '',
                component: BranchListComponent,
                data: { breadcrumb: 'Branches' },
            },
        ]),
    ],
    exports: [RouterModule],
})
export class BranchesRoutingModule {}
