import { NgModule } from '@angular/core';
import { SharedModule } from '@shared/shared.module';
import { AccountsRoutingModule } from './accounts-routing.module';
import { CreateAccountDialogComponent } from './create-account/create-account-dialog.component';
import { EditAccountDialogComponent } from './edit-account/edit-account-dialog.component';
import { AccountsComponent } from './accounts.component';

@NgModule({
    declarations: [
        CreateAccountDialogComponent,
        EditAccountDialogComponent,
        AccountsComponent,
    ],
    imports: [SharedModule, AccountsRoutingModule],
})
export class AccountsModule {}
