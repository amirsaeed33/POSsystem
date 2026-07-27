import { Component, OnInit } from '@angular/core';
import { Table } from 'primeng/table';
import { ConfirmationService, MessageService } from 'primeng/api';
import { PurchaseReturnDto } from 'src/app/demo/api/purchase-return';
import { PurchaseReturnService } from 'src/app/demo/service/purchase-return.service';

@Component({
    templateUrl: './purchase-return-list.component.html',
    providers: [MessageService, ConfirmationService],
})
export class PurchaseReturnListComponent implements OnInit {
    returns: PurchaseReturnDto[] = [];
    loading = false;
    totalRecords = 0;
    keyword = '';

    createDialogVisible = false;
    viewDialogVisible = false;
    viewingReturnId: number | null = null;
    createPurchaseId: number | null = null;

    constructor(
        private purchaseReturnService: PurchaseReturnService,
        private messageService: MessageService,
        private confirmationService: ConfirmationService
    ) {}

    ngOnInit(): void {
        this.loadReturns();
    }

    loadReturns(): void {
        this.loading = true;
        this.purchaseReturnService
            .getAll({
                keyword: this.keyword?.trim() || undefined,
                skipCount: 0,
                maxResultCount: 1000,
            })
            .then((result) => {
                this.returns = result.items;
                this.totalRecords = result.totalCount;
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to load purchase returns',
                });
            })
            .finally(() => {
                this.loading = false;
            });
    }

    onSearch(): void {
        this.loadReturns();
    }

    onGlobalFilter(table: Table, event: Event): void {
        table.filterGlobal(
            (event.target as HTMLInputElement).value,
            'contains'
        );
    }

    openCreateDialog(): void {
        this.createPurchaseId = null;
        this.createDialogVisible = true;
    }

    openViewDialog(item: PurchaseReturnDto): void {
        this.viewingReturnId = item.id;
        this.viewDialogVisible = true;
    }

    onDialogSaved(): void {
        this.loadReturns();
    }

    onDelete(item: PurchaseReturnDto): void {
        this.confirmationService.confirm({
            message: `Are you sure you want to delete purchase return #${item.id}?`,
            header: 'Delete Confirmation',
            icon: 'pi pi-exclamation-triangle',
            acceptButtonStyleClass: 'p-button-danger',
            accept: () => {
                this.purchaseReturnService
                    .delete(item.id)
                    .then(() => {
                        this.messageService.add({
                            severity: 'success',
                            summary: 'Success',
                            detail: 'Purchase return deleted successfully',
                        });
                        this.loadReturns();
                    })
                    .catch((error) => {
                        this.messageService.add({
                            severity: 'error',
                            summary: 'Error',
                            detail:
                                error?.message ||
                                'Failed to delete purchase return',
                        });
                    });
            },
        });
    }
}
