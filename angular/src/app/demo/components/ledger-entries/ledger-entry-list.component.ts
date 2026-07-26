import { Component, OnInit } from '@angular/core';
import { Table } from 'primeng/table';
import { ConfirmationService, MessageService } from 'primeng/api';
import { LedgerEntryDto } from 'src/app/demo/api/ledger-entry';
import { BusinessAccountDto } from 'src/app/demo/api/business-account';
import { LedgerEntryService } from 'src/app/demo/service/ledger-entry.service';
import { BusinessAccountService } from 'src/app/demo/service/business-account.service';

@Component({
    templateUrl: './ledger-entry-list.component.html',
    providers: [MessageService, ConfirmationService],
})
export class LedgerEntryListComponent implements OnInit {
    entries: LedgerEntryDto[] = [];
    accounts: BusinessAccountDto[] = [];
    loading = false;
    totalRecords = 0;
    keyword = '';
    accountId: number | null = null;

    dialogVisible = false;
    editingEntryId: number | null = null;

    constructor(
        private ledgerEntryService: LedgerEntryService,
        private businessAccountService: BusinessAccountService,
        private messageService: MessageService,
        private confirmationService: ConfirmationService
    ) {}

    ngOnInit(): void {
        this.loadAccounts();
        this.loadEntries();
    }

    loadEntries(): void {
        this.loading = true;
        this.ledgerEntryService
            .getAll({
                keyword: this.keyword?.trim() || undefined,
                accountId: this.accountId ?? undefined,
                skipCount: 0,
                maxResultCount: 1000,
            })
            .then((result) => {
                this.entries = result.items;
                this.totalRecords = result.totalCount;
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to load ledger entries',
                });
            })
            .finally(() => {
                this.loading = false;
            });
    }

    onSearch(): void {
        this.loadEntries();
    }

    onGlobalFilter(table: Table, event: Event): void {
        table.filterGlobal((event.target as HTMLInputElement).value, 'contains');
    }

    openCreateDialog(): void {
        this.editingEntryId = null;
        this.dialogVisible = true;
    }

    openEditDialog(entry: LedgerEntryDto): void {
        this.editingEntryId = entry.id;
        this.dialogVisible = true;
    }

    onDialogSaved(): void {
        this.loadEntries();
    }

    onDelete(entry: LedgerEntryDto): void {
        const label = entry.description || `#${entry.id}`;
        this.confirmationService.confirm({
            message: `Are you sure you want to delete ledger entry "${label}"?`,
            header: 'Delete Confirmation',
            icon: 'pi pi-exclamation-triangle',
            acceptButtonStyleClass: 'p-button-danger',
            accept: () => {
                this.ledgerEntryService
                    .delete(entry.id)
                    .then(() => {
                        this.messageService.add({
                            severity: 'success',
                            summary: 'Success',
                            detail: 'Ledger entry deleted successfully',
                        });
                        this.loadEntries();
                    })
                    .catch((error) => {
                        this.messageService.add({
                            severity: 'error',
                            summary: 'Error',
                            detail: error?.message || 'Failed to delete ledger entry',
                        });
                    });
            },
        });
    }

    private loadAccounts(): void {
        this.businessAccountService
            .getAll({ skipCount: 0, maxResultCount: 1000 })
            .then((result) => {
                this.accounts = result.items;
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to load accounts',
                });
            });
    }
}
