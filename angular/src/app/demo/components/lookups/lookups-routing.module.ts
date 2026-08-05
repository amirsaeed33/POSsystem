import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';
import { LookUpListComponent } from './lookup-list.component';

@NgModule({
    imports: [RouterModule.forChild([{ path: '', component: LookUpListComponent, data: { breadcrumb: 'Lookups' } }])],
    exports: [RouterModule],
})
export class LookUpsRoutingModule {}
