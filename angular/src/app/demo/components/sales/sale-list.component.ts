import { AppFormatService } from 'src/app/demo/service/app-format.service';
import { Component, OnInit } from '@angular/core';
import { Table } from 'primeng/table';
import { ConfirmationService, MessageService } from 'primeng/api';
import { PaymentType, SaleDto } from 'src/app/demo/api/sale';
import { SaleService } from 'src/app/demo/service/sale.service';
import { SaleReturnDto } from 'src/app/demo/api/sale-return';
import { SaleReturnService } from 'src/app/demo/service/sale-return.service';

import { PermissionNames } from 'src/app/demo/api/permission-names';
import { PermissionService } from 'src/app/demo/service/permission.service';
import { RowActionItem } from 'src/app/shared/components/row-actions/row-actions.component';

@Component({
    templateUrl: './sale-list.component.html',
    styleUrls: ['./sale-list.component.scss'],
    providers: [MessageService, ConfirmationService],
})
export class SaleListComponent implements OnInit {
    activeTab: 'sales' | 'returns' = 'sales';
    activeListTab = 0;

    sales: SaleDto[] = [];
    loading = false;
    totalRecords = 0;
    keyword = '';

    returns: SaleReturnDto[] = [];
    loadingReturns = false;
    totalReturnRecords = 0;
    returnKeyword = '';

    createDialogInstances: number[] = [];
    viewDialogVisible = false;
    viewingSaleId: number | null = null;
    printDialogVisible = false;
    printingSaleId: number | null = null;
    printAutoPrint = false;
    returnDialogVisible = false;
    returningSaleId: number | null = null;
    viewReturnDialogVisible = false;
    viewingReturnId: number | null = null;

    canCreate = false;
    canEdit = false;
    canDelete = false;

    getRowActions(sale: SaleDto): RowActionItem[] {
        const actions: RowActionItem[] = [
            {
                label: 'View details',
                icon: 'pi pi-eye',
                color: 'blue',
                action: () => this.openViewDialog(sale),
            },
            {
                label: 'Sale Return',
                icon: 'pi pi-replay',
                color: 'amber',
                action: () => this.openReturnDialog(sale),
            },
            {
                label: 'Print invoice',
                icon: 'pi pi-print',
                color: 'indigo',
                action: () => this.openPrintDialog(sale.id),
            }
        ];
        if (this.canDelete) {
            actions.push({
                label: 'Delete',
                icon: 'pi pi-trash',
                color: 'red',
                action: () => this.onDelete(sale),
            });
        }
        return actions;
    }

    getReturnRowActions(item: SaleReturnDto): RowActionItem[] {
        const actions: RowActionItem[] = [
            {
                label: 'View details',
                icon: 'pi pi-eye',
                color: 'blue',
                action: () => this.openViewReturnDialog(item),
            }
        ];
        if (this.canDelete) {
            actions.push({
                label: 'Delete',
                icon: 'pi pi-trash',
                color: 'red',
                action: () => this.onDeleteReturn(item),
            });
        }
        return actions;
    }

    constructor(
        public formatService: AppFormatService,
        private saleService: SaleService,
        private saleReturnService: SaleReturnService,
        private permissionService: PermissionService,
        private messageService: MessageService,
        private confirmationService: ConfirmationService
    ) {}

    ngOnInit(): void {
        this.canCreate = this.permissionService.isGranted(PermissionNames.SalesCreate);
        this.canEdit = this.permissionService.isGranted(PermissionNames.SalesEdit);
        this.canDelete = this.permissionService.isGranted(PermissionNames.SalesDelete);
        this.loadSales();
        this.loadReturns();
    }

    onListTabChange(index: number): void {
        this.activeListTab = index;
        this.activeTab = index === 1 ? 'returns' : 'sales';
        if (this.activeTab === 'returns' && !this.returns.length && !this.loadingReturns) {
            this.loadReturns();
        } else if (this.activeTab === 'sales' && !this.sales.length && !this.loading) {
            this.loadSales();
        }
    }

    switchTab(tab: 'sales' | 'returns'): void {
        this.activeTab = tab;
        this.activeListTab = tab === 'returns' ? 1 : 0;
        if (tab === 'returns' && !this.returns.length && !this.loadingReturns) {
            this.loadReturns();
        } else if (tab === 'sales' && !this.sales.length && !this.loading) {
            this.loadSales();
        }
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

    loadReturns(): void {
        this.loadingReturns = true;
        this.saleReturnService
            .getAll({
                keyword: this.returnKeyword?.trim() || undefined,
                skipCount: 0,
                maxResultCount: 1000,
            })
            .then((result) => {
                this.returns = result.items || [];
                this.totalReturnRecords = result.totalCount ?? this.returns.length;
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to load sale returns',
                });
            })
            .finally(() => {
                this.loadingReturns = false;
            });
    }

    onSearch(): void {
        this.loadSales();
    }

    onSearchReturns(): void {
        this.loadReturns();
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

    openCreateReturnDialog(): void {
        this.returningSaleId = null;
        this.returnDialogVisible = true;
    }

    openViewReturnDialog(item: SaleReturnDto): void {
        this.viewingReturnId = item.id;
        this.viewReturnDialogVisible = true;
    }

    filterReturnsForSale(sale: SaleDto): void {
        this.activeTab = 'returns';
        this.returnKeyword = sale.invoiceNo || `#${sale.id}`;
        this.loadReturns();
    }

    onDialogSaved(): void {
        this.loadSales();
        this.loadReturns();
    }

    formatReturnLines(item: SaleReturnDto): string {
        if (!item?.lines?.length) {
            return '—';
        }
        return item.lines
            .map((line) => `${line.productName || '#' + line.productId} (x${line.quantity})`)
            .join(', ');
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
                        this.loadReturns();
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

    onDeleteReturn(item: SaleReturnDto): void {
        const label = `#${item.id} (${item.saleInvoiceNo || 'Sale #' + item.saleId})`;
        this.confirmationService.confirm({
            message: `Are you sure you want to delete sale return ${label}?`,
            header: 'Delete Return Confirmation',
            icon: 'pi pi-exclamation-triangle',
            acceptButtonStyleClass: 'p-button-danger',
            accept: () => {
                this.saleReturnService
                    .delete(item.id)
                    .then(() => {
                        this.messageService.add({
                            severity: 'success',
                            summary: 'Success',
                            detail: 'Sale return deleted successfully',
                        });
                        this.loadReturns();
                        this.loadSales();
                    })
                    .catch((error) => {
                        this.messageService.add({
                            severity: 'error',
                            summary: 'Error',
                            detail: error?.message || 'Failed to delete sale return',
                        });
                    });
            },
        });
    }
}
