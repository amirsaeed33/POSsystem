import { Component, OnInit } from '@angular/core';
import { Table } from 'primeng/table';
import { ConfirmationService, MessageService } from 'primeng/api';
import { TenantDto } from 'src/app/demo/api/tenant';
import { TenantService } from 'src/app/demo/service/tenant.service';

@Component({
    templateUrl: './tenant-list.component.html',
    providers: [MessageService, ConfirmationService],
})
export class TenantListComponent implements OnInit {
    tenants: TenantDto[] = [];
    loading = false;
    totalRecords = 0;
    keyword = '';

    dialogVisible = false;
    editingTenantId: number | null = null;

    constructor(
        private tenantService: TenantService,
        private messageService: MessageService,
        private confirmationService: ConfirmationService
    ) {}

    ngOnInit(): void {
        this.loadTenants();
    }

    loadTenants(): void {
        this.loading = true;
        this.tenantService
            .getAll({
                keyword: this.keyword?.trim() || undefined,
                skipCount: 0,
                maxResultCount: 1000,
            })
            .then((result) => {
                this.tenants = result.items;
                this.totalRecords = result.totalCount;
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to load tenants',
                });
            })
            .finally(() => {
                this.loading = false;
            });
    }

    onSearch(): void {
        this.loadTenants();
    }

    onGlobalFilter(table: Table, event: Event): void {
        table.filterGlobal(
            (event.target as HTMLInputElement).value,
            'contains'
        );
    }

    openCreateDialog(): void {
        this.editingTenantId = null;
        this.dialogVisible = true;
    }

    openEditDialog(tenant: TenantDto): void {
        this.editingTenantId = tenant.id;
        this.dialogVisible = true;
    }

    onDialogSaved(): void {
        this.loadTenants();
    }

    onDelete(tenant: TenantDto): void {
        this.confirmationService.confirm({
            message: `Are you sure you want to delete tenant "${tenant.tenancyName}"?`,
            header: 'Delete Confirmation',
            icon: 'pi pi-exclamation-triangle',
            acceptButtonStyleClass: 'p-button-danger',
            accept: () => {
                this.tenantService
                    .delete(tenant.id)
                    .then(() => {
                        this.messageService.add({
                            severity: 'success',
                            summary: 'Success',
                            detail: 'Tenant deleted successfully',
                        });
                        this.loadTenants();
                    })
                    .catch((error) => {
                        this.messageService.add({
                            severity: 'error',
                            summary: 'Error',
                            detail:
                                error?.message || 'Failed to delete tenant',
                        });
                    });
            },
        });
    }
}
