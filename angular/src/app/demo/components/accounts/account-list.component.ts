import { Component, OnInit } from '@angular/core';
import { Table } from 'primeng/table';
import { ConfirmationService, MessageService } from 'primeng/api';
import { BusinessAccountDto } from 'src/app/demo/api/business-account';
import { BusinessAccountService } from 'src/app/demo/service/business-account.service';

import { PermissionNames } from 'src/app/demo/api/permission-names';
import { PermissionService } from 'src/app/demo/service/permission.service';

@Component({
    selector: 'app-account-list',
    templateUrl: './account-list.component.html',
    providers: [MessageService, ConfirmationService],
})
export class AccountListComponent implements OnInit {
    accounts: BusinessAccountDto[] = [];
    loading = false;
    totalRecords = 0;
    keyword = '';

    dialogVisible = false;
    editingAccountId: number | null = null;

    canCreate = false;
    canEdit = false;
    canDelete = false;

    constructor(
        private businessAccountService: BusinessAccountService,
        private permissionService: PermissionService,
        private messageService: MessageService,
        private confirmationService: ConfirmationService
    ) {}

    ngOnInit(): void {
        this.canCreate = this.permissionService.isGranted(PermissionNames.AccountsCreate);
        this.canEdit = this.permissionService.isGranted(PermissionNames.AccountsEdit);
        this.canDelete = this.permissionService.isGranted(PermissionNames.AccountsDelete);
        this.loadAccounts();
    }

    loadAccounts(): void {
        this.loading = true;
        this.businessAccountService
            .getAll({
                keyword: this.keyword?.trim() || undefined,
                skipCount: 0,
                maxResultCount: 1000,
            })
            .then((result) => {
                this.accounts = result.items;
                this.totalRecords = result.totalCount;
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to load accounts',
                });
            })
            .finally(() => {
                this.loading = false;
            });
    }

    onSearch(): void {
        this.loadAccounts();
    }

    onGlobalFilter(table: Table, event: Event): void {
        table.filterGlobal(
            (event.target as HTMLInputElement).value,
            'contains'
        );
    }

    openCreateDialog(): void {
        this.editingAccountId = null;
        this.dialogVisible = true;
    }

    openEditDialog(account: BusinessAccountDto): void {
        this.editingAccountId = account.id;
        this.dialogVisible = true;
    }

    onDialogSaved(): void {
        this.loadAccounts();
    }

    onDelete(account: BusinessAccountDto): void {
        this.confirmationService.confirm({
            message: `Are you sure you want to delete account "${account.name}"?`,
            header: 'Delete Confirmation',
            icon: 'pi pi-exclamation-triangle',
            acceptButtonStyleClass: 'p-button-danger',
            accept: () => {
                this.businessAccountService
                    .delete(account.id)
                    .then(() => {
                        this.messageService.add({
                            severity: 'success',
                            summary: 'Success',
                            detail: 'Account deleted successfully',
                        });
                        this.loadAccounts();
                    })
                    .catch((error) => {
                        this.messageService.add({
                            severity: 'error',
                            summary: 'Error',
                            detail:
                                error?.message || 'Failed to delete account',
                        });
                    });
            },
        });
    }
}
