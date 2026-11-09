import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';
import { PackageListComponent } from './package-list.component';

@NgModule({
    imports: [
        RouterModule.forChild([{ path: '', component: PackageListComponent }]),
    ],
    exports: [RouterModule],
})
export class PackagesRoutingModule {}
