import { AppFormatService } from 'src/app/demo/service/app-format.service';
import { Component, OnInit } from '@angular/core';
import { Table } from 'primeng/table';
import { ConfirmationService, MessageService } from 'primeng/api';
import { SaleReturnDto } from 'src/app/demo/api/sale-return';
import { SaleReturnService } from 'src/app/demo/service/sale-return.service';
import { RowActionItem } from 'src/app/shared/components/row-actions/row-actions.component';
import { ThermalPrintService } from 'src/app/demo/service/thermal-print.service';

@Component({
    templateUrl: './sale-return-list.component.html',
    providers: [MessageService, ConfirmationService],
})
export class SaleReturnListComponent implements OnInit {
    returns: SaleReturnDto[] = [];
    loading = false;
    totalRecords = 0;
    keyword = '';

    createDialogVisible = false;
    viewDialogVisible = false;
    viewingReturnId: number | null = null;
    createSaleId: number | null = null;

    getRowActions(item: SaleReturnDto): RowActionItem[] {
        return [
            {
                label: 'View details',
                icon: 'pi pi-eye',
                color: 'blue',
                action: () => this.openViewDialog(item),
            },
            {
                label: 'Print Thermal Receipt',
                icon: 'pi pi-print',
                color: 'green',
                action: () => this.printReceipt(item),
            },
            {
                label: 'Delete',
                icon: 'pi pi-trash',
                color: 'red',
                action: () => this.onDelete(item),
            },
        ];
    }

    constructor(
        public formatService: AppFormatService,
        private saleReturnService: SaleReturnService,
        private thermalPrintService: ThermalPrintService,
        private messageService: MessageService,
        private confirmationService: ConfirmationService
    ) {}

    ngOnInit(): void {
        this.loadReturns();
    }

    printReceipt(item: SaleReturnDto): void {
        this.saleReturnService
            .get(item.id)
            .then((fullReturn) => {
                this.thermalPrintService.printSaleReturn(fullReturn);
            })
            .catch(() => {
                this.thermalPrintService.printSaleReturn(item);
            });
    }

    loadReturns(): void {
        this.loading = true;
        this.saleReturnService
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
                    detail: error?.message || 'Failed to load sale returns',
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
        this.createSaleId = null;
        this.createDialogVisible = true;
    }

    openViewDialog(item: SaleReturnDto): void {
        this.viewingReturnId = item.id;
        this.viewDialogVisible = true;
    }

    onDialogSaved(): void {
        this.loadReturns();
    }

    onDelete(item: SaleReturnDto): void {
        this.confirmationService.confirm({
            message: `Are you sure you want to delete sale return #${item.id}?`,
            header: 'Delete Confirmation',
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
                    })
                    .catch((error) => {
                        this.messageService.add({
                            severity: 'error',
                            summary: 'Error',
                            detail:
                                error?.message || 'Failed to delete sale return',
                        });
                    });
            },
        });
    }
}
