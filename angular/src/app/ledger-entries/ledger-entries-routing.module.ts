import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { LedgerEntriesComponent } from './ledger-entries.component';

const routes: Routes = [
    {
        path: '',
        component: LedgerEntriesComponent,
        pathMatch: 'full',
    },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class LedgerEntriesRoutingModule {}
