import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SharedModule } from '@shared/shared.module';
import { LedgerEntriesRoutingModule } from './ledger-entries-routing.module';
import { CreateLedgerEntryDialogComponent } from './create-ledger-entry/create-ledger-entry-dialog.component';
import { EditLedgerEntryDialogComponent } from './edit-ledger-entry/edit-ledger-entry-dialog.component';
import { LedgerEntriesComponent } from './ledger-entries.component';

@NgModule({
    declarations: [
        CreateLedgerEntryDialogComponent,
        EditLedgerEntryDialogComponent,
        LedgerEntriesComponent,
    ],
    imports: [CommonModule, SharedModule, LedgerEntriesRoutingModule],
})
export class LedgerEntriesModule {}
