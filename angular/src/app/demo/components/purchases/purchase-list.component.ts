import { Component, OnInit } from '@angular/core';
import { Table } from 'primeng/table';
import { ConfirmationService, MessageService } from 'primeng/api';
import { PurchaseDto } from 'src/app/demo/api/purchase';
import { PurchaseService } from 'src/app/demo/service/purchase.service';

@Component({
    templateUrl: './purchase-list.component.html',
    providers: [MessageService, ConfirmationService],
})
export class PurchaseListComponent implements OnInit {
    purchases: PurchaseDto[] = [];
    loading = false;
    totalRecords = 0;
    keyword = '';

    createDialogVisible = false;
    viewDialogVisible = false;
    viewingPurchaseId: number | null = null;
    printDialogVisible = false;
    printingPurchaseId: number | null = null;
    printAutoPrint = false;

    constructor(
        private purchaseService: PurchaseService,
        private messageService: MessageService,
        private confirmationService: ConfirmationService
    ) {}

    ngOnInit(): void {
        this.loadPurchases();
    }

    loadPurchases(): void {
        this.loading = true;
        this.purchaseService
            .getAll({
                keyword: this.keyword?.trim() || undefined,
                skipCount: 0,
                maxResultCount: 1000,
            })
            .then((result) => {
                this.purchases = result.items;
                this.totalRecords = result.totalCount;
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to load purchases',
                });
            })
            .finally(() => {
                this.loading = false;
            });
    }

    onSearch(): void {
        this.loadPurchases();
    }

    onGlobalFilter(table: Table, event: Event): void {
        table.filterGlobal((event.target as HTMLInputElement).value, 'contains');
    }

    openCreateDialog(): void {
        this.createDialogVisible = true;
    }

    openViewDialog(purchase: PurchaseDto): void {
        this.viewingPurchaseId = purchase.id;
        this.viewDialogVisible = true;
    }

    openPrintDialog(purchaseId: number, autoPrint = false): void {
        this.printingPurchaseId = purchaseId;
        this.printAutoPrint = autoPrint;
        this.printDialogVisible = true;
    }

    onDialogSaved(): void {
        this.loadPurchases();
    }

    onDelete(purchase: PurchaseDto): void {
        const label = purchase.invoiceNo || `#${purchase.id}`;
        this.confirmationService.confirm({
            message: `Are you sure you want to delete purchase "${label}"?`,
            header: 'Delete Confirmation',
            icon: 'pi pi-exclamation-triangle',
            acceptButtonStyleClass: 'p-button-danger',
            accept: () => {
                this.purchaseService
                    .delete(purchase.id)
                    .then(() => {
                        this.messageService.add({
                            severity: 'success',
                            summary: 'Success',
                            detail: 'Purchase deleted successfully',
                        });
                        this.loadPurchases();
                    })
                    .catch((error) => {
                        this.messageService.add({
                            severity: 'error',
                            summary: 'Error',
                            detail: error?.message || 'Failed to delete purchase',
                        });
                    });
            },
        });
    }
}
