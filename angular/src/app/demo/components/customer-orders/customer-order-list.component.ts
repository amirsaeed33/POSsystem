import { AppFormatService } from 'src/app/demo/service/app-format.service';
import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { Table } from 'primeng/table';
import { ConfirmationService, MessageService } from 'primeng/api';
import {
    CustomerOrderDto,
    CustomerOrderStatus,
} from 'src/app/demo/api/customer-order';
import { CustomerOrderService } from 'src/app/demo/service/customer-order.service';
import { CustomerChatService } from 'src/app/demo/service/customer-chat.service';
import { RowActionItem } from 'src/app/shared/components/row-actions/row-actions.component';
import { PermissionService } from 'src/app/demo/service/permission.service';
import { PermissionNames } from 'src/app/demo/api/permission-names';
import { ThermalPrintService } from 'src/app/demo/service/thermal-print.service';
import { BranchContextService } from 'src/app/demo/service/branch-context.service';
import { Subscription } from 'rxjs';

@Component({
    templateUrl: './customer-order-list.component.html',
    providers: [MessageService, ConfirmationService],
})
export class CustomerOrderListComponent implements OnInit, OnDestroy {
    orders: CustomerOrderDto[] = [];
    loading = false;
    totalRecords = 0;
    keyword = '';
    statusFilter: number | null = null;

    createDialogVisible = false;
    viewDialogVisible = false;
    viewingOrderId: number | null = null;
    chatDialogVisible = false;
    selectedOrderForChat: CustomerOrderDto | null = null;
    unreadOrderNos: Set<string> = new Set<string>();
    private chatSub?: Subscription;

    statusOptions = [
        { label: 'Pending', value: CustomerOrderStatus.Pending },
        { label: 'In Progress', value: CustomerOrderStatus.Approved },
        { label: 'Approved', value: CustomerOrderStatus.Approved },
        { label: 'Rejected', value: CustomerOrderStatus.Rejected },
    ];

    canOverrideDeliveryFee = false;

    constructor(public formatService: AppFormatService, 
        private customerOrderService: CustomerOrderService,
        private customerChatService: CustomerChatService,
        private messageService: MessageService,
        private confirmationService: ConfirmationService,
        private route: ActivatedRoute,
        private permissionService: PermissionService,
        private thermalPrintService: ThermalPrintService,
        private branchContext: BranchContextService
    ) {}

    get isRestaurant(): boolean {
        return this.branchContext.isRestaurant();
    }

    ngOnInit(): void {
        this.canOverrideDeliveryFee = this.permissionService.isGranted(
            PermissionNames.CustomerOrdersOverrideDeliveryFee
        );
        this.loadOrders();
        this.route.queryParams.subscribe((params) => {
            if (params['id']) {
                this.viewingOrderId = Number(params['id']);
                this.viewDialogVisible = true;
            }
        });

        const branchId = this.branchContext.getBranchId();
        if (branchId) {
            this.customerChatService.joinBranchChat(branchId);
        }

        this.chatSub = this.customerChatService.notificationReceived$.subscribe(msg => {
            if (msg.orderNo) {
                this.unreadOrderNos.add(msg.orderNo.trim().toUpperCase());
            }
        });
    }

    ngOnDestroy(): void {
        this.chatSub?.unsubscribe();
    }

    async loadUnreadChatOrders(): Promise<void> {
        const branchId = this.branchContext.getBranchId();
        if (!branchId) return;
        try {
            const convs = await this.customerChatService.getBranchConversations(branchId);
            this.unreadOrderNos.clear();
            for (const c of convs) {
                if (c.orderNo && c.unreadCount > 0) {
                    this.unreadOrderNos.add(c.orderNo.trim().toUpperCase());
                }
            }
        } catch {}
    }

    hasUnreadChat(order: CustomerOrderDto): boolean {
        if (!order?.orderNo) return false;
        return this.unreadOrderNos.has(order.orderNo.trim().toUpperCase());
    }

    loadOrders(): void {
        this.loading = true;
        this.loadUnreadChatOrders();
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

    openChatDialog(order: CustomerOrderDto): void {
        this.selectedOrderForChat = order;
        this.chatDialogVisible = true;
        if (order?.orderNo) {
            this.unreadOrderNos.delete(order.orderNo.trim().toUpperCase());
        }
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
            case CustomerOrderStatus.InProgress:
                return 'In Progress';
            case CustomerOrderStatus.Delivered:
                return 'Delivered';
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
            case CustomerOrderStatus.InProgress:
                return 'info';
            case CustomerOrderStatus.Delivered:
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

    isInProgress(order: CustomerOrderDto): boolean {
        return order.status === CustomerOrderStatus.Approved || order.status === CustomerOrderStatus.InProgress;
    }

    isDelivered(order: CustomerOrderDto): boolean {
        return order.status === CustomerOrderStatus.Delivered;
    }

    onApprove(order: CustomerOrderDto): void {
        const label = order.orderNo || `#${order.id}`;
        const confirmMsg = this.isRestaurant
            ? `Approve customer order "${label}"? This will create a sale invoice and print the receipt.`
            : `Approve customer order "${label}"? This will create a sale invoice.`;

        this.confirmationService.confirm({
            message: confirmMsg,
            header: 'Approve Confirmation',
            icon: 'pi pi-check-circle',
            accept: async () => {
                try {
                    const sale = await this.customerOrderService.approve(order.id);
                    const invoice = sale?.invoiceNo
                        ? ` Sale invoice: ${sale.invoiceNo}.`
                        : '';
                    const detailMsg = this.isRestaurant
                        ? `Order approved successfully.${invoice} Printing thermal receipt...`
                        : `Order approved successfully.${invoice}`;
                    this.messageService.add({
                        severity: 'success',
                        summary: 'Success',
                        detail: detailMsg,
                    });
                    this.loadOrders();

                    // Print thermal receipt automatically ONLY for restaurant branches
                    if (this.isRestaurant) {
                        try {
                            const fullOrder = await this.customerOrderService.get(order.id);
                            if (sale?.invoiceNo) {
                                fullOrder.saleInvoiceNo = sale.invoiceNo;
                            }
                            this.thermalPrintService.printCustomerOrder(fullOrder, sale?.invoiceNo);
                        } catch (err) {
                            console.warn('Auto print failed, falling back to cached order', err);
                            this.thermalPrintService.printCustomerOrder(order, sale?.invoiceNo);
                        }
                    }
                } catch (error: any) {
                    const message =
                        error?.error?.error?.message ||
                        error?.error?.message ||
                        error?.message ||
                        'Failed to approve order';
                    this.messageService.add({
                        severity: 'error',
                        summary: 'Approve Failed',
                        detail: message,
                        life: 6000,
                    });
                }
            },
        });
    }

    printThermalReceipt(order: CustomerOrderDto): void {
        this.customerOrderService
            .get(order.id)
            .then((fullOrder) => {
                this.thermalPrintService.printCustomerOrder(fullOrder, order.saleInvoiceNo);
            })
            .catch(() => {
                this.thermalPrintService.printCustomerOrder(order, order.saleInvoiceNo);
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
                        const message =
                            error?.error?.error?.message ||
                            error?.error?.message ||
                            error?.message ||
                            'Failed to reject order';
                        this.messageService.add({
                            severity: 'error',
                            summary: 'Reject Failed',
                            detail: message,
                            life: 6000,
                        });
                    });
            },
        });
    }

    onDeliver(order: CustomerOrderDto): void {
        const label = order.orderNo || `#${order.id}`;
        this.confirmationService.confirm({
            message: `Mark customer order "${label}" as Delivered?`,
            header: 'Deliver Confirmation',
            icon: 'pi pi-check-circle',
            accept: () => {
                this.customerOrderService
                    .deliver(order.id)
                    .then(() => {
                        this.messageService.add({
                            severity: 'success',
                            summary: 'Delivered',
                            detail: `Order ${label} marked as Delivered successfully.`,
                        });
                        this.loadOrders();
                    })
                    .catch((error) => {
                        const message =
                            error?.error?.error?.message ||
                            error?.error?.message ||
                            error?.message ||
                            'Failed to mark order as delivered';
                        this.messageService.add({
                            severity: 'error',
                            summary: 'Action Failed',
                            detail: message,
                            life: 6000,
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

    getRowActions(order: CustomerOrderDto): RowActionItem[] {
        return [
            {
                label: 'View details',
                icon: 'pi pi-eye',
                action: () => this.openViewDialog(order),
            },
            {
                label: 'Chat with Customer',
                icon: 'pi pi-comments',
                action: () => this.openChatDialog(order),
            },
            {
                label: 'Adjust Delivery Fee',
                icon: 'pi pi-dollar',
                visible: this.isPending(order) && this.canOverrideDeliveryFee,
                action: () => this.openViewDialog(order),
            },
            {
                label: 'Print Thermal Receipt',
                icon: 'pi pi-print',
                visible: this.isRestaurant,
                action: () => this.printThermalReceipt(order),
            },
            {
                label: 'Approve',
                icon: 'pi pi-check',
                visible: this.isPending(order),
                action: () => this.onApprove(order),
            },
            {
                label: 'Mark Delivered',
                icon: 'pi pi-check-circle',
                visible: this.isInProgress(order),
                action: () => this.onDeliver(order),
            },
            {
                label: 'Reject',
                icon: 'pi pi-times',
                visible: this.isPending(order),
                action: () => this.onReject(order),
            },
            {
                label: 'Delete',
                icon: 'pi pi-trash',
                danger: true,
                visible: this.isPending(order),
                action: () => this.onDelete(order),
            },
        ];
    }
}
