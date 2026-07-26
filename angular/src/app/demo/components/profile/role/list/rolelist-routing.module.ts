import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';
import { RoleListComponent } from './rolelist.component';

@NgModule({
    imports: [RouterModule.forChild([
        { path: '', component: RoleListComponent, data: { breadcrumb: 'Role List' } }
    ])],
    exports: [RouterModule]
})
export class RoleListRoutingModule { }

