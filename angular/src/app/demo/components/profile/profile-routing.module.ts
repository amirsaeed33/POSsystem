import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';

@NgModule({
    imports: [RouterModule.forChild([
        { path: 'list', data: {breadcrumb: 'List'}, loadChildren: () => import('./list/profilelist.module').then(m => m.ProfileListModule) },
        { path: 'create', data: {breadcrumb: 'Create'}, loadChildren: () => import('./create/profilecreate.module').then(m => m.ProfileCreateModule) },
        { path: 'edit/:id', data: {breadcrumb: 'Edit'}, loadChildren: () => import('./create/profilecreate.module').then(m => m.ProfileCreateModule) },
        { path: 'role', data: {breadcrumb: 'Roles'}, loadChildren: () => import('./role/list/rolelist.module').then(m => m.RoleListModule) },
        { path: 'role/create', data: {breadcrumb: 'Create Role'}, loadChildren: () => import('./role/create/rolecreate.module').then(m => m.RoleCreateModule) },
        { path: 'role/edit/:id', data: {breadcrumb: 'Edit Role'}, loadChildren: () => import('./role/create/rolecreate.module').then(m => m.RoleCreateModule) },
        { path: '**', redirectTo: '/notfound' }
    ])],
    exports: [RouterModule]
})
export class ProfileRoutingModule { }
