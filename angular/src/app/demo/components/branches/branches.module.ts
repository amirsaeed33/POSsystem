import { NgModule } from '@angular/core';
import { BranchesRoutingModule } from './branches-routing.module';
import { BranchesSharedModule } from './branches-shared.module';

@NgModule({
    imports: [BranchesSharedModule, BranchesRoutingModule],
})
export class BranchesModule {}
