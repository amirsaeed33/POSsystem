import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';
import { ChangePasswordComponent } from './changepassword.component';

@NgModule({
    imports: [
        RouterModule.forChild([
            { path: '', component: ChangePasswordComponent },
        ]),
    ],
    exports: [RouterModule],
})
export class ChangePasswordRoutingModule {}
