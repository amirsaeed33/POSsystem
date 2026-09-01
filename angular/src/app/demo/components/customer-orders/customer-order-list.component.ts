import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { Table } from 'primeng/table';
import { ConfirmationService, MessageService } from 'primeng/api';
import {
    CustomerOrderDto,
    CustomerOrderStatus,
} from 'src/app/demo/api/customer-order';
import { CustomerOrderService } from 'src/app/demo/service/customer-order.service';

@Component({
    templateUrl: './customer-order-list.component.html',
    providers: [MessageService, ConfirmationService],
})
export class CustomerOrderListComponent implements OnInit {
    orders: CustomerOrderDto[] = [];
    loading = false;
    totalRecords = 0;
    keyword = '';
    statusFilter: number | null = null;

    createDialogVisible = false;
    viewDialogVisible = false;
    viewingOrderId: number | null = null;

    statusOptions = [
        { label: 'Pending', value: CustomerOrderStatus.Pending },
        { label: 'Approved', value: CustomerOrderStatus.Approved },
        { label: 'Rejected', value: CustomerOrderStatus.Rejected },
    ];

    constructor(
        private customerOrderService: CustomerOrderService,
        private messageService: MessageService,
        private confirmationService: ConfirmationService,
        private route: ActivatedRoute
    ) {}

    ngOnInit(): void {
        this.loadOrders();
        this.route.queryParams.subscribe((params) => {
            if (params['id']) {
                this.viewingOrderId = Number(params['id']);
                this.viewDialogVisible = true;
            }
        });
    }

    loadOrders(): void {
        this.loading = true;
        this.customerOrderService
            .getAll({
                keyword: this.keyword?.trim() || undefined,
                status: this.statusFilter ?? undefined,
                skipCount: 0,
                maxResultCount: 1000,
            })
            .then((result) => {
                this.orders = result.items;
                this.totalRecords = result.totalCount;
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to load customer orders',
                });
            })
            .finally(() => {
                this.loading = false;
            });
    }

    onSearch(): void {
        this.loadOrders();
    }

    onGlobalFilter(table: Table, event: Event): void {
        table.filterGlobal((event.target as HTMLInputElement).value, 'contains');
    }

    openCreateDialog(): void {
        this.createDialogVisible = true;
    }

    openViewDialog(order: CustomerOrderDto): void {
        this.viewingOrderId = order.id;
        this.viewDialogVisible = true;
    }

    onDialogSaved(): void {
        this.loadOrders();
    }

    statusLabel(order: CustomerOrderDto): string {
        if (order.statusName) {
            return order.statusName;
        }
        switch (order.status) {
            case CustomerOrderStatus.Pending:
                return 'Pending';
            case CustomerOrderStatus.Approved:
                return 'Approved';
            case CustomerOrderStatus.Rejected:
                return 'Rejected';
            default:
                return String(order.status);
        }
    }

    statusSeverity(
        status: number
    ): 'success' | 'info' | 'warning' | 'danger' | 'secondary' | 'contrast' | undefined {
        switch (status) {
            case CustomerOrderStatus.Pending:
                return 'warning';
            case CustomerOrderStatus.Approved:
                return 'success';
            case CustomerOrderStatus.Rejected:
                return 'danger';
            default:
                return 'secondary';
        }
    }

    isPending(order: CustomerOrderDto): boolean {
        return order.status === CustomerOrderStatus.Pending;
    }

    onApprove(order: CustomerOrderDto): void {
        const label = order.orderNo || `#${order.id}`;
        this.confirmationService.confirm({
            message: `Approve customer order "${label}"? This will create a sale invoice.`,
            header: 'Approve Confirmation',
            icon: 'pi pi-check-circle',
            accept: () => {
                this.customerOrderService
                    .approve(order.id)
                    .then((sale) => {
                        const invoice = sale?.invoiceNo
                            ? ` Sale invoice: ${sale.invoiceNo}.`
                            : '';
                        this.messageService.add({
                            severity: 'success',
                            summary: 'Success',
                            detail: `Order approved successfully.${invoice}`,
                        });
                        this.loadOrders();
                    })
                    .catch((error) => {
                        this.messageService.add({
                            severity: 'error',
                            summary: 'Error',
                            detail: error?.message || 'Failed to approve order',
                        });
                    });
            },
        });
    }

    onReject(order: CustomerOrderDto): void {
        const label = order.orderNo || `#${order.id}`;
        this.confirmationService.confirm({
            message: `Reject customer order "${label}"?`,
            header: 'Reject Confirmation',
            icon: 'pi pi-times-circle',
            acceptButtonStyleClass: 'p-button-danger',
            accept: () => {
                this.customerOrderService
                    .reject(order.id)
                    .then(() => {
                        this.messageService.add({
                            severity: 'success',
                            summary: 'Success',
                            detail: 'Order rejected successfully',
                        });
                        this.loadOrders();
                    })
                    .catch((error) => {
                        this.messageService.add({
                            severity: 'error',
                            summary: 'Error',
                            detail: error?.message || 'Failed to reject order',
                        });
                    });
            },
        });
    }

    onDelete(order: CustomerOrderDto): void {
        const label = order.orderNo || `#${order.id}`;
        this.confirmationService.confirm({
            message: `Are you sure you want to delete customer order "${label}"?`,
            header: 'Delete Confirmation',
            icon: 'pi pi-exclamation-triangle',
            acceptButtonStyleClass: 'p-button-danger',
            accept: () => {
                this.customerOrderService
                    .delete(order.id)
                    .then(() => {
                        this.messageService.add({
                            severity: 'success',
                            summary: 'Success',
                            detail: 'Customer order deleted successfully',
                        });
                        this.loadOrders();
                    })
                    .catch((error) => {
                        this.messageService.add({
                            severity: 'error',
                            summary: 'Error',
                            detail:
                                error?.message || 'Failed to delete customer order',
                        });
                    });
            },
        });
    }
}
