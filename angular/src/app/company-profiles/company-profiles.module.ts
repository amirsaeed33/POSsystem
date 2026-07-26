import { NgModule } from '@angular/core';
import { SharedModule } from '@shared/shared.module';
import { CompanyProfilesRoutingModule } from './company-profiles-routing.module';
import { CreateCompanyProfileDialogComponent } from './create-company-profile/create-company-profile-dialog.component';
import { EditCompanyProfileDialogComponent } from './edit-company-profile/edit-company-profile-dialog.component';
import { CompanyProfilesComponent } from './company-profiles.component';

@NgModule({
    declarations: [
        CreateCompanyProfileDialogComponent,
        EditCompanyProfileDialogComponent,
        CompanyProfilesComponent,
    ],
    imports: [SharedModule, CompanyProfilesRoutingModule],
})
export class CompanyProfilesModule {}
