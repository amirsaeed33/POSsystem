import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';
import { CompanyProfileListComponent } from './company-profile-list.component';

@NgModule({
    imports: [
        RouterModule.forChild([
            {
                path: '',
                component: CompanyProfileListComponent,
                data: { breadcrumb: 'Company Profiles' },
            },
        ]),
    ],
    exports: [RouterModule],
})
export class CompanyProfilesRoutingModule {}
