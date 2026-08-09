import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';
import { HostCatalogListComponent } from './host-catalog-list.component';

@NgModule({
    imports: [RouterModule.forChild([{ path: '', component: HostCatalogListComponent }])],
    exports: [RouterModule],
})
export class HostCatalogRoutingModule {}
