import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';
import { BranchListComponent } from './branch-list.component';
import { BranchEditComponent } from './branch-edit/branch-edit.component';

@NgModule({
    imports: [
        RouterModule.forChild([
            {
                path: '',
                component: BranchListComponent,
                data: { breadcrumb: 'Branches' },
            },
             {
                path: 'edit/:id',
                component: BranchEditComponent,
                data: { breadcrumb: 'Edit Branch' },
            },
        ]),
    ],
    exports: [RouterModule],
})
export class BranchesRoutingModule {}
