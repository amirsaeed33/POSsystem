import { Component, OnInit } from '@angular/core';
import { Table } from 'primeng/table';
import { ConfirmationService, MessageService } from 'primeng/api';
import { PaymentType, SaleDto } from 'src/app/demo/api/sale';
import { SaleService } from 'src/app/demo/service/sale.service';

@Component({
    templateUrl: './sale-list.component.html',
    providers: [MessageService, ConfirmationService],
})
export class SaleListComponent implements OnInit {
    sales: SaleDto[] = [];
    loading = false;
    totalRecords = 0;
    keyword = '';

    createDialogInstances: number[] = [];
    viewDialogVisible = false;
    viewingSaleId: number | null = null;
    printDialogVisible = false;
    printingSaleId: number | null = null;
    printAutoPrint = false;
    returnDialogVisible = false;
    returningSaleId: number | null = null;

    constructor(
        private saleService: SaleService,
        private messageService: MessageService,
        private confirmationService: ConfirmationService
    ) {}

    ngOnInit(): void {
        this.loadSales();
    }

    loadSales(): void {
        this.loading = true;
        this.saleService
            .getAll({
                keyword: this.keyword?.trim() || undefined,
                skipCount: 0,
                maxResultCount: 1000,
            })
            .then((result) => {
                this.sales = result.items;
                this.totalRecords = result.totalCount;
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to load sales',
                });
            })
            .finally(() => {
                this.loading = false;
            });
    }

    onSearch(): void {
        this.loadSales();
    }

    onGlobalFilter(table: Table, event: Event): void {
        table.filterGlobal((event.target as HTMLInputElement).value, 'contains');
    }

    openCreateDialog(): void {
        this.createDialogInstances.push(Date.now());
    }

    onDialogVisibleChange(instanceId: number, visible: boolean): void {
        if (!visible) {
            this.createDialogInstances = this.createDialogInstances.filter(id => id !== instanceId);
        }
    }

    trackByFn(index: number, item: number): number {
        return item;
    }

    openViewDialog(sale: SaleDto): void {
        this.viewingSaleId = sale.id;
        this.viewDialogVisible = true;
    }

    openPrintDialog(saleId: number, autoPrint = false): void {
        this.printingSaleId = saleId;
        this.printAutoPrint = autoPrint;
        this.printDialogVisible = true;
    }

    openReturnDialog(sale: SaleDto): void {
        this.openReturnDialogForSaleId(sale.id);
    }

    openReturnDialogForSaleId(saleId: number): void {
        this.returningSaleId = saleId;
        this.returnDialogVisible = true;
    }

    onDialogSaved(): void {
        this.loadSales();
    }

    paymentTypeLabel(paymentType: number): string {
        switch (paymentType) {
            case PaymentType.Cash:
                return 'Cash';
            case PaymentType.Card:
                return 'Card';
            case PaymentType.Credit:
                return 'Credit';
            case PaymentType.Mixed:
                return 'Mixed';
            default:
                return String(paymentType);
        }
    }

    onDelete(sale: SaleDto): void {
        const label = sale.invoiceNo || `#${sale.id}`;
        this.confirmationService.confirm({
            message: `Are you sure you want to delete sale "${label}"?`,
            header: 'Delete Confirmation',
            icon: 'pi pi-exclamation-triangle',
            acceptButtonStyleClass: 'p-button-danger',
            accept: () => {
                this.saleService
                    .delete(sale.id)
                    .then(() => {
                        this.messageService.add({
                            severity: 'success',
                            summary: 'Success',
                            detail: 'Sale deleted successfully',
                        });
                        this.loadSales();
                    })
                    .catch((error) => {
                        this.messageService.add({
                            severity: 'error',
                            summary: 'Error',
                            detail: error?.message || 'Failed to delete sale',
                        });
                    });
            },
        });
    }
}
