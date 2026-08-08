import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';
import { ActivateBranchComponent } from './activate-branch.component';

@NgModule({
    imports: [RouterModule.forChild([
        { path: '', component: ActivateBranchComponent }
    ])],
    exports: [RouterModule]
})
export class ActivateBranchRoutingModule {}
