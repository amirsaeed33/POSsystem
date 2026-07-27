import { Component, OnInit } from '@angular/core';
import { Table } from 'primeng/table';
import { ConfirmationService, MessageService } from 'primeng/api';
import { SaleReturnDto } from 'src/app/demo/api/sale-return';
import { SaleReturnService } from 'src/app/demo/service/sale-return.service';

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

    constructor(
        private saleReturnService: SaleReturnService,
        private messageService: MessageService,
        private confirmationService: ConfirmationService
    ) {}

    ngOnInit(): void {
        this.loadReturns();
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
