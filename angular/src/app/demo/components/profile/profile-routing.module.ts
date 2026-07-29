import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';
import { PermissionNames } from '../../api/permission-names';
import { PermissionGuard } from '../../service/permission.guard';

@NgModule({
    imports: [RouterModule.forChild([
        { path: 'list', data: { breadcrumb: 'List', permission: PermissionNames.Users }, canActivate: [PermissionGuard], loadChildren: () => import('./list/profilelist.module').then(m => m.ProfileListModule) },
        { path: 'role', data: { breadcrumb: 'Roles', permission: PermissionNames.Roles }, canActivate: [PermissionGuard], loadChildren: () => import('./role/list/rolelist.module').then(m => m.RoleListModule) },
        { path: '**', redirectTo: '/notfound' }
    ])],
    exports: [RouterModule]
})
export class ProfileRoutingModule { }
