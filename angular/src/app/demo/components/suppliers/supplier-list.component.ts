import { Component, OnInit } from '@angular/core';
import { Table } from 'primeng/table';
import { ConfirmationService, MessageService } from 'primeng/api';
import { SupplierDto } from 'src/app/demo/api/supplier';
import { SupplierService } from 'src/app/demo/service/supplier.service';

import { PermissionNames } from 'src/app/demo/api/permission-names';
import { PermissionService } from 'src/app/demo/service/permission.service';

@Component({
    templateUrl: './supplier-list.component.html',
    providers: [MessageService, ConfirmationService],
})
export class SupplierListComponent implements OnInit {
    suppliers: SupplierDto[] = [];
    loading = false;
    totalRecords = 0;
    keyword = '';

    dialogVisible = false;
    editingSupplierId: number | null = null;

    canCreate = false;
    canEdit = false;
    canDelete = false;

    constructor(
        private supplierService: SupplierService,
        private permissionService: PermissionService,
        private messageService: MessageService,
        private confirmationService: ConfirmationService
    ) {}

    ngOnInit(): void {
        this.canCreate = this.permissionService.isGranted(PermissionNames.SuppliersCreate);
        this.canEdit = this.permissionService.isGranted(PermissionNames.SuppliersEdit);
        this.canDelete = this.permissionService.isGranted(PermissionNames.SuppliersDelete);
        this.loadSuppliers();
    }

    loadSuppliers(): void {
        this.loading = true;
        this.supplierService
            .getAll({
                keyword: this.keyword?.trim() || undefined,
                skipCount: 0,
                maxResultCount: 1000,
            })
            .then((result) => {
                this.suppliers = result.items;
                this.totalRecords = result.totalCount;
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to load suppliers',
                });
            })
            .finally(() => {
                this.loading = false;
            });
    }

    onSearch(): void {
        this.loadSuppliers();
    }

    onGlobalFilter(table: Table, event: Event): void {
        table.filterGlobal((event.target as HTMLInputElement).value, 'contains');
    }

    openCreateDialog(): void {
        this.editingSupplierId = null;
        this.dialogVisible = true;
    }

    openEditDialog(supplier: SupplierDto): void {
        this.editingSupplierId = supplier.id;
        this.dialogVisible = true;
    }

    onDialogSaved(): void {
        this.loadSuppliers();
    }

    onDelete(supplier: SupplierDto): void {
        this.confirmationService.confirm({
            message: `Are you sure you want to delete supplier "${supplier.name}"?`,
            header: 'Delete Confirmation',
            icon: 'pi pi-exclamation-triangle',
            acceptButtonStyleClass: 'p-button-danger',
            accept: () => {
                this.supplierService
                    .delete(supplier.id)
                    .then(() => {
                        this.messageService.add({
                            severity: 'success',
                            summary: 'Success',
                            detail: 'Supplier deleted successfully',
                        });
                        this.loadSuppliers();
                    })
                    .catch((error) => {
                        this.messageService.add({
                            severity: 'error',
                            summary: 'Error',
                            detail: error?.message || 'Failed to delete supplier',
                        });
                    });
            },
        });
    }
}
