import { Component, OnInit } from '@angular/core';
import { Table } from 'primeng/table';
import { ConfirmationService, MessageService } from 'primeng/api';
import {
    StockAdjustmentDto,
    StockAdjustmentReasons,
} from 'src/app/demo/api/stock-adjustment';
import { StockAdjustmentService } from 'src/app/demo/service/stock-adjustment.service';

import { PermissionNames } from 'src/app/demo/api/permission-names';
import { PermissionService } from 'src/app/demo/service/permission.service';

@Component({
    templateUrl: './stock-adjustment-list.component.html',
    providers: [MessageService, ConfirmationService],
})
export class StockAdjustmentListComponent implements OnInit {
    adjustments: StockAdjustmentDto[] = [];
    loading = false;
    totalRecords = 0;
    keyword = '';

    formDialogVisible = false;
    editingAdjustmentId: number | null = null;

    viewDialogVisible = false;
    viewingAdjustmentId: number | null = null;

    canCreate = false;
    canEdit = false;
    canDelete = false;

    constructor(
        private stockAdjustmentService: StockAdjustmentService,
        private permissionService: PermissionService,
        private messageService: MessageService,
        private confirmationService: ConfirmationService
    ) {}

    ngOnInit(): void {
        this.canCreate = this.permissionService.isGranted(PermissionNames.StockAdjustmentsCreate);
        this.canEdit = this.permissionService.isGranted(PermissionNames.StockAdjustmentsEdit);
        this.canDelete = this.permissionService.isGranted(PermissionNames.StockAdjustmentsDelete);
        this.loadAdjustments();
    }

    loadAdjustments(): void {
        this.loading = true;
        this.stockAdjustmentService
            .getAll({
                keyword: this.keyword?.trim() || undefined,
                skipCount: 0,
                maxResultCount: 1000,
            })
            .then((result) => {
                this.adjustments = result.items;
                this.totalRecords = result.totalCount;
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to load stock adjustments',
                });
            })
            .finally(() => {
                this.loading = false;
            });
    }

    onSearch(): void {
        this.loadAdjustments();
    }

    onGlobalFilter(table: Table, event: Event): void {
        table.filterGlobal(
            (event.target as HTMLInputElement).value,
            'contains'
        );
    }

    openCreateDialog(): void {
        this.editingAdjustmentId = null;
        this.formDialogVisible = true;
    }

    openEditDialog(item: StockAdjustmentDto): void {
        this.editingAdjustmentId = item.id;
        this.formDialogVisible = true;
    }

    openViewDialog(item: StockAdjustmentDto): void {
        this.viewingAdjustmentId = item.id;
        this.viewDialogVisible = true;
    }

    onDialogSaved(): void {
        this.loadAdjustments();
    }

    reasonLabel(reason: number): string {
        switch (reason) {
            case StockAdjustmentReasons.Opening:
                return 'Opening';
            case StockAdjustmentReasons.Damage:
                return 'Damage';
            case StockAdjustmentReasons.Loss:
                return 'Loss';
            case StockAdjustmentReasons.Recount:
                return 'Recount';
            default:
                return 'Other';
        }
    }

    onDelete(item: StockAdjustmentDto): void {
        const label = item.referenceNo || `#${item.id}`;
        this.confirmationService.confirm({
            message: `Are you sure you want to delete stock adjustment "${label}"?`,
            header: 'Delete Confirmation',
            icon: 'pi pi-exclamation-triangle',
            acceptButtonStyleClass: 'p-button-danger',
            accept: () => {
                this.stockAdjustmentService
                    .delete(item.id)
                    .then(() => {
                        this.messageService.add({
                            severity: 'success',
                            summary: 'Success',
                            detail: 'Stock adjustment deleted successfully',
                        });
                        this.loadAdjustments();
                    })
                    .catch((error) => {
                        this.messageService.add({
                            severity: 'error',
                            summary: 'Error',
                            detail:
                                error?.message ||
                                'Failed to delete stock adjustment',
                        });
                    });
            },
        });
    }
}
