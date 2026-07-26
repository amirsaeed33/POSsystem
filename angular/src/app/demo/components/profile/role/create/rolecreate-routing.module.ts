import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';
import { RoleCreateComponent } from './rolecreate.component';

@NgModule({
    imports: [RouterModule.forChild([
        { path: '', component: RoleCreateComponent }
    ])],
    exports: [RouterModule]
})
export class RoleCreateRoutingModule { }

