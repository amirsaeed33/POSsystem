import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';
import { PermissionNames } from '../../api/permission-names';
import { PermissionGuard } from '../../service/permission.guard';

@NgModule({
    imports: [RouterModule.forChild([
        { path: 'list', data: { breadcrumb: 'List', permission: PermissionNames.Users }, canActivate: [PermissionGuard], loadChildren: () => import('./list/profilelist.module').then(m => m.ProfileListModule) },
        { path: 'create', data: { breadcrumb: 'Create', permission: PermissionNames.Users }, canActivate: [PermissionGuard], loadChildren: () => import('./create/profilecreate.module').then(m => m.ProfileCreateModule) },
        { path: 'edit/:id', data: { breadcrumb: 'Edit', permission: PermissionNames.Users }, canActivate: [PermissionGuard], loadChildren: () => import('./create/profilecreate.module').then(m => m.ProfileCreateModule) },
        { path: 'role', data: { breadcrumb: 'Roles', permission: PermissionNames.Roles }, canActivate: [PermissionGuard], loadChildren: () => import('./role/list/rolelist.module').then(m => m.RoleListModule) },
        { path: 'role/create', data: { breadcrumb: 'Create Role', permission: PermissionNames.Roles }, canActivate: [PermissionGuard], loadChildren: () => import('./role/create/rolecreate.module').then(m => m.RoleCreateModule) },
        { path: 'role/edit/:id', data: { breadcrumb: 'Edit Role', permission: PermissionNames.Roles }, canActivate: [PermissionGuard], loadChildren: () => import('./role/create/rolecreate.module').then(m => m.RoleCreateModule) },
        { path: '**', redirectTo: '/notfound' }
    ])],
    exports: [RouterModule]
})
export class ProfileRoutingModule { }
