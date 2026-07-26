import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';
import { TenantListComponent } from './tenant-list.component';

@NgModule({
    imports: [
        RouterModule.forChild([
            {
                path: '',
                component: TenantListComponent,
                data: { breadcrumb: 'Tenants' },
            },
        ]),
    ],
    exports: [RouterModule],
})
export class TenantsRoutingModule {}
