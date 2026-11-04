import { Component, OnInit } from '@angular/core';
import { Table } from 'primeng/table';
import { ConfirmationService, MessageService } from 'primeng/api';
import { CustomerDto, CustomerType } from 'src/app/demo/api/customer';
import { CustomerService } from 'src/app/demo/service/customer.service';
import { RowActionItem } from 'src/app/shared/components/row-actions/row-actions.component';
import { PermissionNames } from 'src/app/demo/api/permission-names';
import { PermissionService } from 'src/app/demo/service/permission.service';

@Component({
    templateUrl: './customer-list.component.html',
    styleUrls: ['./customer-list.component.scss'],
    providers: [MessageService, ConfirmationService],
})
export class CustomerListComponent implements OnInit {
    customers: CustomerDto[] = [];
    loading = false;
    totalRecords = 0;
    keyword = '';

    dialogVisible = false;
    editingCustomerId: number | null = null;

    canCreate = false;
    canEdit = false;
    canDelete = false;

    /** Track broken image URLs so we fall back to initials. */
    private brokenPhotoIds = new Set<number>();

    constructor(
        private customerService: CustomerService,
        private permissionService: PermissionService,
        private messageService: MessageService,
        private confirmationService: ConfirmationService
    ) {}

    ngOnInit(): void {
        this.canCreate = this.permissionService.isGranted(PermissionNames.CustomersCreate);
        this.canEdit = this.permissionService.isGranted(PermissionNames.CustomersEdit);
        this.canDelete = this.permissionService.isGranted(PermissionNames.CustomersDelete);
        this.loadCustomers();
    }

    loadCustomers(): void {
        this.loading = true;
        this.customerService
            .getAll({
                keyword: this.keyword?.trim() || undefined,
                skipCount: 0,
                maxResultCount: 1000,
            })
            .then((result) => {
                this.customers = result.items;
                this.totalRecords = result.totalCount;
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to load customers',
                });
            })
            .finally(() => {
                this.loading = false;
            });
    }

    onSearch(): void {
        this.loadCustomers();
    }

    onGlobalFilter(table: Table, event: Event): void {
        table.filterGlobal((event.target as HTMLInputElement).value, 'contains');
    }

    openCreateDialog(): void {
        this.editingCustomerId = null;
        this.dialogVisible = true;
    }

    openEditDialog(customer: CustomerDto): void {
        this.editingCustomerId = customer.id;
        this.dialogVisible = true;
    }

    onDialogSaved(): void {
        this.loadCustomers();
    }

    getCustomerTypeLabel(customerType: number): string {
        return customerType === CustomerType.Wholesaler ? 'Wholesaler' : 'Direct';
    }

    customerPhotoUrl(customer: CustomerDto): string {
        if (!customer?.imagePath || this.brokenPhotoIds.has(customer.id)) {
            return '';
        }
        return this.customerService.getImageUrl(customer.imagePath);
    }

    onCustomerPhotoError(customer: CustomerDto): void {
        if (customer?.id != null) {
            this.brokenPhotoIds.add(customer.id);
        }
    }

    customerInitials(customer: CustomerDto): string {
        const parts = (customer?.name || '')
            .trim()
            .split(/\s+/)
            .filter(Boolean);
        if (!parts.length) {
            return '?';
        }
        if (parts.length === 1) {
            return parts[0].slice(0, 2).toUpperCase();
        }
        return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
    }

    onDelete(customer: CustomerDto): void {
        this.confirmationService.confirm({
            message: `Are you sure you want to delete customer "${customer.name}"?`,
            header: 'Delete Confirmation',
            icon: 'pi pi-exclamation-triangle',
            acceptButtonStyleClass: 'p-button-danger',
            accept: () => {
                this.customerService
                    .delete(customer.id)
                    .then(() => {
                        this.messageService.add({
                            severity: 'success',
                            summary: 'Success',
                            detail: 'Customer deleted successfully',
                        });
                        this.loadCustomers();
                    })
                    .catch((error) => {
                        this.messageService.add({
                            severity: 'error',
                            summary: 'Error',
                            detail: error?.message || 'Failed to delete customer',
                        });
                    });
            },
        });
    }

    getRowActions(customer: CustomerDto): RowActionItem[] {
        return [
            {
                label: 'Edit',
                icon: 'pi pi-pencil',
                visible: this.canEdit,
                action: () => this.openEditDialog(customer),
            },
            {
                label: 'Delete',
                icon: 'pi pi-trash',
                danger: true,
                visible: this.canDelete,
                action: () => this.onDelete(customer),
            },
        ];
    }
}
