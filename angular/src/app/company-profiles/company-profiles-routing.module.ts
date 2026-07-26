import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { CompanyProfilesComponent } from './company-profiles.component';

const routes: Routes = [
    {
        path: '',
        component: CompanyProfilesComponent,
        pathMatch: 'full',
    },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class CompanyProfilesRoutingModule {}
