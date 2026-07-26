import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';
import { LedgerEntryListComponent } from './ledger-entry-list.component';

@NgModule({
    imports: [
        RouterModule.forChild([
            {
                path: '',
                component: LedgerEntryListComponent,
                data: { breadcrumb: 'Ledger Entries' },
            },
        ]),
    ],
    exports: [RouterModule],
})
export class LedgerEntriesRoutingModule {}
