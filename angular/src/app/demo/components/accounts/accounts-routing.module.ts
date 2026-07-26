import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';
import { AccountListComponent } from './account-list.component';

@NgModule({
    imports: [
        RouterModule.forChild([
            {
                path: '',
                component: AccountListComponent,
                data: { breadcrumb: 'Accounts' },
            },
        ]),
    ],
    exports: [RouterModule],
})
export class AccountsRoutingModule {}
