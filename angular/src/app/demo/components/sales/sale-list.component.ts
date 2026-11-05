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
    fromDate = '';
    toDate = '';

    returns: SaleReturnDto[] = [];
    loadingReturns = false;
    totalReturnRecords = 0;
    returnKeyword = '';

    // Sale Type Filter
    selectedSaleType = 'All';
    saleTypeOptions = [
        { label: 'All Types', value: 'All' },
        { label: 'Dine-In', value: 'Dine-In' },
        { label: 'Takeaway', value: 'Takeaway' },
        { label: 'Online Order', value: 'Online Order' }
    ];

    // Collect Payment Dialog State
    collectDialogVisible = false;
    selectedSaleForPayment: SaleDto | null = null;
    collectAmount = 0;
    collectPaymentMethod = 1;
    paymentMethodOptions = [
        { label: 'Cash', value: 1 },
        { label: 'Card', value: 2 }
    ]; // 1 = Cash, 2 = Card
    collectNotes = '';
    collectingPayment = false;

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
        ];

        if (this.getDueAmount(sale) > 0) {
            actions.push({
                label: 'Collect Payment',
                icon: 'pi pi-dollar',
                color: 'green',
                action: () => this.openCollectPaymentDialog(sale),
            });
        }

        actions.push(
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
        );
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

    get filteredSales(): SaleDto[] {
        if (!this.selectedSaleType || this.selectedSaleType === 'All') {
            return this.sales;
        }
        return this.sales.filter(s => this.getSaleType(s) === this.selectedSaleType);
    }

    getSaleType(sale: SaleDto): string {
        if (sale.saleType === 'Online Order' || sale.saleType === 'Dine-In' || sale.saleType === 'Takeaway') {
            return sale.saleType;
        }
        const notes = sale.notes || '';
        if (notes.includes('[Online Order') || notes.toLowerCase().includes('online order') || notes.includes('ORD-') || (sale.deliveryCharges && sale.deliveryCharges > 0) || sale.deliveryAreaName) {
            return 'Online Order';
        }
        if (notes.includes('[Dine-In') || notes.toLowerCase().includes('dine-in') || notes.includes('[Car Service')) {
            return 'Dine-In';
        }
        return 'Takeaway';
    }

    getPaidAmount(sale: SaleDto): number {
        if (sale.paidAmount !== undefined && sale.paidAmount !== null) {
            return sale.paidAmount;
        }
        return (sale.cashAmount || 0) + (sale.cardAmount || 0);
    }

    getDueAmount(sale: SaleDto): number {
        if (sale.dueAmount !== undefined && sale.dueAmount !== null) {
            return sale.dueAmount;
        }
        return Math.max(0, (sale.totalAmount || 0) - this.getPaidAmount(sale));
    }

    openCollectPaymentDialog(sale: SaleDto): void {
        this.selectedSaleForPayment = sale;
        this.collectAmount = this.getDueAmount(sale);
        this.collectPaymentMethod = 1;
        this.collectNotes = '';
        this.collectDialogVisible = true;
    }

    cancelCollectPayment(): void {
        this.collectDialogVisible = false;
        this.selectedSaleForPayment = null;
        this.collectAmount = 0;
        this.collectNotes = '';
    }

    async submitCollectPayment(): Promise<void> {
        if (!this.selectedSaleForPayment) return;
        const due = this.getDueAmount(this.selectedSaleForPayment);
        if (this.collectAmount <= 0) {
            this.messageService.add({ severity: 'warn', summary: 'Invalid Amount', detail: 'Please enter an amount greater than 0.' });
            return;
        }
        if (this.collectAmount > due + 0.01) {
            this.messageService.add({ severity: 'error', summary: 'Amount Exceeded', detail: `Amount cannot exceed remaining due (PKR ${due.toFixed(2)}).` });
            return;
        }

        this.collectingPayment = true;
        try {
            await this.saleService.collectPayment({
                saleId: this.selectedSaleForPayment.id,
                amount: this.collectAmount,
                paymentMethod: this.collectPaymentMethod,
                notes: this.collectNotes?.trim() || undefined
            });

            this.messageService.add({
                severity: 'success',
                summary: 'Payment Collected',
                detail: `Collected PKR ${this.collectAmount.toFixed(2)} for ${this.selectedSaleForPayment.invoiceNo || '#' + this.selectedSaleForPayment.id}`
            });
            this.collectDialogVisible = false;
            this.selectedSaleForPayment = null;
            this.loadSales();
        } catch (err: any) {
            this.messageService.add({
                severity: 'error',
                summary: 'Failed',
                detail: err?.message || 'Failed to collect payment.'
            });
        } finally {
            this.collectingPayment = false;
        }
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
                fromDate: this.fromDate || undefined,
                toDate: this.toDate || undefined,
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

    onDateChange(): void {
        this.loadSales();
    }

    clearDateFilter(): void {
        this.fromDate = '';
        this.toDate = '';
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
