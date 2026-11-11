import { AppFormatService } from 'src/app/demo/service/app-format.service';
import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { Table } from 'primeng/table';
import { ConfirmationService, MessageService } from 'primeng/api';
import {
    CustomerOrderDto,
    CustomerOrderStatus,
    DeliveryRiderStatusDto,
} from 'src/app/demo/api/customer-order';
import { CustomerOrderService } from 'src/app/demo/service/customer-order.service';
import { CustomerChatService } from 'src/app/demo/service/customer-chat.service';
import { RowActionItem } from 'src/app/shared/components/row-actions/row-actions.component';
import { PermissionService } from 'src/app/demo/service/permission.service';
import { PermissionNames } from 'src/app/demo/api/permission-names';
import { ThermalPrintService } from 'src/app/demo/service/thermal-print.service';
import { BranchContextService } from 'src/app/demo/service/branch-context.service';
import { AppSoundService } from 'src/app/demo/service/app-sound.service';
import { DeliveryAreaService, DeliveryAreaDto } from 'src/app/demo/service/delivery-area.service';
import { Subscription } from 'rxjs';

export interface AreaOrderGroup {
    areaName: string;
    deliveryAreaId?: number | null;
    orders: CustomerOrderDto[];
    orderCount: number;
    totalAmount: number;
    totalDeliveryFees: number;
}

@Component({
    templateUrl: './customer-order-list.component.html',
    styleUrls: ['./customer-order-list.component.scss'],
    providers: [MessageService, ConfirmationService],
})
export class CustomerOrderListComponent implements OnInit, OnDestroy {
    activeListTab = 0;
    orders: CustomerOrderDto[] = [];
    loading = false;
    isSilentSyncing = false;
    totalRecords = 0;
    keyword = '';
    statusFilter: number | null = null;

    // Area-Wise Order History State
    deliveryAreas: DeliveryAreaDto[] = [];
    areaOptions: { label: string; value: string | null }[] = [];
    selectedArea: string | null = null;
    areaKeyword = '';
    areaFromDateTime = '';
    areaToDateTime = '';
    areaGroups: AreaOrderGroup[] = [];
    areaGrandTotalOrders = 0;
    areaGrandTotalAmount = 0;

    // Real-Time Auto-Sync State
    autoSyncEnabled = true;
    autoSyncIntervalSeconds = 8;
    autoSyncCountdown = 8;
    lastUpdated = new Date();
    private autoSyncTimer: any;
    private orderCreatedSub?: Subscription;
    private orderStatusSub?: Subscription;
    private branchSub?: Subscription;

    createDialogVisible = false;
    viewDialogVisible = false;
    viewingOrderId: number | null = null;
    chatDialogVisible = false;
    selectedOrderForChat: CustomerOrderDto | null = null;
    unreadOrderNos: Set<string> = new Set<string>();
    private chatSub?: Subscription;

    // Delivery Rider Assignment (Restaurant branches)
    assignRiderDialogVisible = false;
    selectedOrderForRider: CustomerOrderDto | null = null;
    availableRiders: DeliveryRiderStatusDto[] = [];
    selectedRider: DeliveryRiderStatusDto | null = null;
    loadingRiders = false;
    assigningRider = false;
    addRiderDialogVisible = false;
    canCreateStaff = false;

    statusOptions = [
        { label: 'Pending', value: CustomerOrderStatus.Pending },
        { label: 'Approved', value: CustomerOrderStatus.Approved },
        { label: 'In Progress', value: CustomerOrderStatus.InProgress },
        { label: 'Delivered', value: CustomerOrderStatus.Delivered },
        { label: 'Completed', value: CustomerOrderStatus.Completed },
        { label: 'Rejected', value: CustomerOrderStatus.Rejected },
    ];

    canOverrideDeliveryFee = false;

    constructor(
        public formatService: AppFormatService,
        private customerOrderService: CustomerOrderService,
        private customerChatService: CustomerChatService,
        private messageService: MessageService,
        private confirmationService: ConfirmationService,
        private route: ActivatedRoute,
        private permissionService: PermissionService,
        private thermalPrintService: ThermalPrintService,
        private branchContext: BranchContextService,
        private appSound: AppSoundService,
        private deliveryAreaService: DeliveryAreaService
    ) {}

    get isRestaurant(): boolean {
        return this.branchContext.isRestaurant();
    }

    ngOnInit(): void {
        this.loadDeliveryAreas();
        this.canOverrideDeliveryFee = this.permissionService.isGranted(
            PermissionNames.CustomerOrdersOverrideDeliveryFee
        );
        this.canCreateStaff = this.permissionService.isGranted(PermissionNames.StaffCreate);
        // ONLINE-PIPELINE START
        let isInitialParams = true;
        this.route.queryParams.subscribe((params) => {
            if (params['status'] !== undefined && params['status'] !== null && params['status'] !== '') {
                const parsedStatus = Number(params['status']);
                if (!isNaN(parsedStatus)) {
                    this.statusFilter = parsedStatus;
                }
            }
            if (params['id']) {
                this.viewingOrderId = Number(params['id']);
                this.viewDialogVisible = true;
            }
            if (!isInitialParams && params['status'] !== undefined) {
                this.loadOrders();
            }
            isInitialParams = false;
        });
        this.loadOrders();
        // ONLINE-PIPELINE END

        const branchId = this.branchContext.getBranchId();
        if (branchId) {
            this.customerChatService.joinBranchChat(branchId);
        }

        // Reconnect/re-join if user switches branch
        this.branchSub = this.branchContext.currentBranch$.subscribe((b) => {
            if (b?.id) {
                this.customerChatService.joinBranchChat(b.id);
                this.loadDeliveryAreas();
                this.loadOrders(true);
            }
        });

        // 1. Real-time Customer Inquiries & Chat Notifications
        this.chatSub = this.customerChatService.notificationReceived$.subscribe((msg) => {
            if (msg.orderNo) {
                this.unreadOrderNos.add(msg.orderNo.trim().toUpperCase());
            }
        });

        // 2. Real-time New Online Order Placed
        this.orderCreatedSub = this.customerChatService.newOrderReceived$.subscribe((order) => {
            const currentBranchId = this.branchContext.getBranchId();
            if (!order.branchId || !currentBranchId || order.branchId === currentBranchId) {
                this.playOrderAlertSound();
                this.messageService.add({
                    severity: 'success',
                    summary: `🛒 New Customer Order! #${order.orderNo || order.orderId}`,
                    detail: `${order.customerName || 'Online Customer'} placed an order of PKR ${order.totalAmount || 0}`,
                    life: 9000
                });
                this.autoSyncCountdown = this.autoSyncIntervalSeconds;
                this.loadOrders(true);
            }
        });

        // 3. Real-time Order Status Updated (Approved/Rejected/Delivered)
        this.orderStatusSub = this.customerChatService.orderStatusChanged$.subscribe((evt) => {
            const currentBranchId = this.branchContext.getBranchId();
            if (!evt.branchId || !currentBranchId || evt.branchId === currentBranchId) {
                this.loadOrders(true);
            }
        });

        // 4. Start automatic silent refresh countdown loop
        this.startAutoSyncTimer();
    }

    ngOnDestroy(): void {
        if (this.autoSyncTimer) {
            clearInterval(this.autoSyncTimer);
        }
        this.chatSub?.unsubscribe();
        this.orderCreatedSub?.unsubscribe();
        this.orderStatusSub?.unsubscribe();
        this.branchSub?.unsubscribe();
    }

    startAutoSyncTimer(): void {
        if (this.autoSyncTimer) {
            clearInterval(this.autoSyncTimer);
        }
        this.autoSyncTimer = setInterval(() => {
            if (this.autoSyncEnabled && !this.loading && !this.isSilentSyncing) {
                this.autoSyncCountdown--;
                if (this.autoSyncCountdown <= 0) {
                    this.autoSyncCountdown = this.autoSyncIntervalSeconds;
                    this.loadOrders(true);
                }
            }
        }, 1000);
    }

    toggleAutoSync(): void {
        this.autoSyncEnabled = !this.autoSyncEnabled;
        if (this.autoSyncEnabled) {
            this.autoSyncCountdown = this.autoSyncIntervalSeconds;
            this.loadOrders(true);
        }
    }

    manualRefresh(): void {
        this.autoSyncCountdown = this.autoSyncIntervalSeconds;
        this.loadOrders(false);
    }

    playOrderAlertSound(): void {
        if (!this.appSound.canPlay()) {
            return;
        }
        try {
            const AudioContextClass = window.AudioContext || (window as any).webkitAudioContext;
            if (!AudioContextClass) return;
            const ctx = new AudioContextClass();
            const now = ctx.currentTime;

            const osc1 = ctx.createOscillator();
            const gain1 = ctx.createGain();
            osc1.type = 'triangle';
            osc1.frequency.setValueAtTime(587.33, now); // D5
            gain1.gain.setValueAtTime(0.25, now);
            gain1.gain.exponentialRampToValueAtTime(0.001, now + 0.22);
            osc1.connect(gain1);
            gain1.connect(ctx.destination);
            osc1.start(now);
            osc1.stop(now + 0.22);

            const osc2 = ctx.createOscillator();
            const gain2 = ctx.createGain();
            osc2.type = 'triangle';
            osc2.frequency.setValueAtTime(880.00, now + 0.16); // A5
            gain2.gain.setValueAtTime(0.28, now + 0.16);
            gain2.gain.exponentialRampToValueAtTime(0.001, now + 0.45);
            osc2.connect(gain2);
            gain2.connect(ctx.destination);
            osc2.start(now + 0.16);
            osc2.stop(now + 0.45);
        } catch {}
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

    loadOrders(silent = false): void {
        if (!silent) {
            this.loading = true;
        } else {
            this.isSilentSyncing = true;
        }
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
                this.lastUpdated = new Date();
                this.updateAreaOptions();
                this.computeAreaWiseGroups();
            })
            .catch((error) => {
                if (!silent) {
                    this.messageService.add({
                        severity: 'error',
                        summary: 'Error',
                        detail: error?.message || 'Failed to load customer orders',
                    });
                }
            })
            .finally(() => {
                this.loading = false;
                this.isSilentSyncing = false;
            });
    }

    onSearch(): void {
        this.loadOrders();
    }

    onListTabChange(index: number): void {
        this.activeListTab = index;
        if (this.activeListTab === 1) {
            this.computeAreaWiseGroups();
        }
    }

    async loadDeliveryAreas(): Promise<void> {
        try {
            const branchId = this.branchContext.getBranchId();
            const res = await this.deliveryAreaService.getAll({
                branchId: branchId || undefined,
                maxResultCount: 1000,
            });
            this.deliveryAreas = res.items || [];
        } catch (err) {
            console.warn('Failed to load delivery areas lookup', err);
            this.deliveryAreas = [];
        } finally {
            this.updateAreaOptions();
            this.computeAreaWiseGroups();
        }
    }

    updateAreaOptions(): void {
        const areaMap = new Map<string, string>();
        if (this.deliveryAreas && this.deliveryAreas.length > 0) {
            this.deliveryAreas.forEach((a) => {
                if (a.name && a.name.trim()) {
                    areaMap.set(a.name.trim(), a.name.trim());
                }
            });
        }
        if (this.orders && this.orders.length > 0) {
            this.orders.forEach((o) => {
                const isRejectedOrCancelled =
                    o.status === CustomerOrderStatus.Rejected ||
                    o.statusName?.toLowerCase() === 'rejected' ||
                    o.statusName?.toLowerCase() === 'cancelled' ||
                    o.statusName?.toLowerCase() === 'canceled';

                if (!isRejectedOrCancelled && o.deliveryAreaName && o.deliveryAreaName.trim()) {
                    areaMap.set(o.deliveryAreaName.trim(), o.deliveryAreaName.trim());
                }
            });
        }

        const sortedNames = Array.from(areaMap.keys()).sort((a, b) => a.localeCompare(b));
        this.areaOptions = [
            { label: 'All Areas', value: null },
            ...sortedNames.map((name) => ({ label: name, value: name })),
            { label: 'Unassigned / Direct', value: '__UNASSIGNED__' },
        ];
    }

    computeAreaWiseGroups(): void {
        if (!this.orders || this.orders.length === 0) {
            this.areaGroups = [];
            this.areaGrandTotalOrders = 0;
            this.areaGrandTotalAmount = 0;
            return;
        }

        // Exclude Cancelled and Rejected orders from Area-Wise Order History
        let filtered = this.orders.filter(
            (o) =>
                o.status !== CustomerOrderStatus.Rejected &&
                o.statusName?.toLowerCase() !== 'rejected' &&
                o.statusName?.toLowerCase() !== 'cancelled' &&
                o.statusName?.toLowerCase() !== 'canceled'
        );

        // 1. Keyword search (orderNo, customerName, customerPhone, notes, area)
        if (this.areaKeyword && this.areaKeyword.trim()) {
            const kw = this.areaKeyword.trim().toLowerCase();
            filtered = filtered.filter(
                (o) =>
                    (o.orderNo && o.orderNo.toLowerCase().includes(kw)) ||
                    (o.customerName && o.customerName.toLowerCase().includes(kw)) ||
                    (o.customerPhone && o.customerPhone.toLowerCase().includes(kw)) ||
                    (o.notes && o.notes.toLowerCase().includes(kw)) ||
                    (o.deliveryAreaName && o.deliveryAreaName.toLowerCase().includes(kw))
            );
        }

        // 2. Area filter
        if (this.selectedArea) {
            if (this.selectedArea === '__UNASSIGNED__') {
                filtered = filtered.filter((o) => !o.deliveryAreaName || !o.deliveryAreaName.trim());
            } else {
                const targetArea = this.selectedArea.toLowerCase();
                filtered = filtered.filter(
                    (o) => o.deliveryAreaName && o.deliveryAreaName.trim().toLowerCase() === targetArea
                );
            }
        }

        // 3. Date & Time from
        if (this.areaFromDateTime) {
            const fromTime = new Date(this.areaFromDateTime).getTime();
            if (!isNaN(fromTime)) {
                filtered = filtered.filter((o) => {
                    const orderTime = new Date(o.orderDate).getTime();
                    return !isNaN(orderTime) && orderTime >= fromTime;
                });
            }
        }

        // 4. Date & Time to
        if (this.areaToDateTime) {
            const toTime = new Date(this.areaToDateTime).getTime();
            if (!isNaN(toTime)) {
                filtered = filtered.filter((o) => {
                    const orderTime = new Date(o.orderDate).getTime();
                    return !isNaN(orderTime) && orderTime <= toTime;
                });
            }
        }

        // Grouping by area
        const groupMap = new Map<string, CustomerOrderDto[]>();
        for (const order of filtered) {
            const areaName =
                order.deliveryAreaName && order.deliveryAreaName.trim()
                    ? order.deliveryAreaName.trim()
                    : 'Unassigned / Direct';
            if (!groupMap.has(areaName)) {
                groupMap.set(areaName, []);
            }
            groupMap.get(areaName)!.push(order);
        }

        // Sort groups alphabetically, but put 'Unassigned / Direct' at the end
        const sortedGroupNames = Array.from(groupMap.keys()).sort((a, b) => {
            if (a === 'Unassigned / Direct') return 1;
            if (b === 'Unassigned / Direct') return -1;
            return a.localeCompare(b);
        });

        this.areaGroups = sortedGroupNames.map((areaName) => {
            const groupOrders = groupMap.get(areaName) || [];
            // Sort orders descending by date
            groupOrders.sort(
                (a, b) => new Date(b.orderDate).getTime() - new Date(a.orderDate).getTime()
            );
            const orderCount = groupOrders.length;
            const totalAmount = groupOrders.reduce(
                (sum, o) => sum + (Number(o.totalAmount) || 0),
                0
            );
            const totalDeliveryFees = groupOrders.reduce(
                (sum, o) => sum + (Number(o.deliveryFee) || 0),
                0
            );
            return {
                areaName,
                orders: groupOrders,
                orderCount,
                totalAmount,
                totalDeliveryFees,
            };
        });

        this.areaGrandTotalOrders = this.areaGroups.reduce((sum, g) => sum + g.orderCount, 0);
        this.areaGrandTotalAmount = this.areaGroups.reduce((sum, g) => sum + g.totalAmount, 0);
    }

    clearAreaFilters(): void {
        this.areaKeyword = '';
        this.selectedArea = null;
        this.areaFromDateTime = '';
        this.areaToDateTime = '';
        this.computeAreaWiseGroups();
    }

    printAreaWiseReport(): void {
        window.print();
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
            case CustomerOrderStatus.Completed:
                return 'Completed';
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
                return 'contrast';
            case CustomerOrderStatus.Completed:
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

    isCompleted(order: CustomerOrderDto): boolean {
        return order.status === CustomerOrderStatus.Completed;
    }

    /** Finished for rider reassignment / edit locks. */
    isClosed(order: CustomerOrderDto): boolean {
        return (
            order.status === CustomerOrderStatus.Delivered ||
            order.status === CustomerOrderStatus.Completed ||
            order.status === CustomerOrderStatus.Rejected
        );
    }

    async openAssignRiderDialog(order: CustomerOrderDto): Promise<void> {
        this.selectedOrderForRider = order;
        this.selectedRider = null;
        this.assignRiderDialogVisible = true;
        await this.reloadAvailableRiders();
    }

    openAddRiderDialog(): void {
        this.addRiderDialogVisible = true;
    }

    async onRiderStaffSaved(): Promise<void> {
        await this.reloadAvailableRiders();
    }

    private async reloadAvailableRiders(): Promise<void> {
        const order = this.selectedOrderForRider;
        if (!order) {
            return;
        }

        this.loadingRiders = true;
        try {
            const branchId = (order.branchId || this.branchContext.getBranchId()) ?? undefined;
            this.availableRiders = await this.customerOrderService.getAvailableRiders(branchId, order.id);
            if (order.riderId) {
                this.selectedRider = this.availableRiders.find((r) => r.riderId === order.riderId) || null;
            } else if (
                this.selectedRider &&
                !this.availableRiders.some((r) => r.riderId === this.selectedRider?.riderId)
            ) {
                this.selectedRider = null;
            }
        } catch (err: any) {
            console.error('Failed to load available delivery riders', err);
            this.messageService.add({
                severity: 'error',
                summary: 'Error',
                detail: 'Failed to load delivery riders list.',
            });
        } finally {
            this.loadingRiders = false;
        }
    }

    async confirmAssignOrReassignRider(): Promise<void> {
        if (!this.selectedOrderForRider) return;

        if (!this.selectedRider) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Select Rider',
                detail: 'Please select an available delivery rider from the list.',
            });
            return;
        }

        if (this.selectedRider.isBusy) {
            this.messageService.add({
                severity: 'error',
                summary: 'Rider Busy',
                detail: `Rider ${this.selectedRider.riderName} is already delivering Order #${this.selectedRider.busyOrderNo}. Please select an available rider.`,
                life: 5000,
            });
            return;
        }

        this.assigningRider = true;
        const order = this.selectedOrderForRider;
        const isReassign = !!order.riderId;
        try {
            await this.customerOrderService.assignRider({
                orderId: order.id,
                riderId: this.selectedRider.riderId,
                riderName: this.selectedRider.riderName,
                riderPhone: this.selectedRider.riderPhone,
            });

            this.messageService.add({
                severity: 'success',
                summary: isReassign ? 'Rider Reassigned' : 'Rider Assigned',
                detail: isReassign
                    ? `Order ${order.orderNo || '#' + order.id} reassigned to rider ${this.selectedRider.riderName}.`
                    : `Order ${order.orderNo || '#' + order.id} assigned to rider ${this.selectedRider.riderName}.`,
            });

            this.assignRiderDialogVisible = false;
            this.loadOrders();
        } catch (error: any) {
            const message =
                error?.error?.error?.message ||
                error?.error?.message ||
                error?.message ||
                'Failed to assign rider';
            this.messageService.add({
                severity: 'error',
                summary: 'Action Failed',
                detail: message,
                life: 6000,
            });
        } finally {
            this.assigningRider = false;
        }
    }

    onApprove(order: CustomerOrderDto): void {
        const label = order.orderNo || `#${order.id}`;
        const confirmMsg = this.isRestaurant
            ? `Approve customer order "${label}"? An available delivery rider will be automatically assigned.`
            : `Approve customer order "${label}"?`;

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

                    let assignedRiderName: string | null = null;
                    let fullOrder: CustomerOrderDto = order;

                    // For restaurant branches, fetch updated order to get the auto-assigned rider & print receipt
                    if (this.isRestaurant) {
                        try {
                            fullOrder = await this.customerOrderService.get(order.id);
                            assignedRiderName = fullOrder.riderName || null;
                            if (sale?.invoiceNo) {
                                fullOrder.saleInvoiceNo = sale.invoiceNo;
                            }
                            // Auto print thermal receipt
                            this.thermalPrintService.printCustomerOrder(fullOrder, sale?.invoiceNo);
                        } catch (err) {
                            console.warn('Post-approval order fetch or print failed', err);
                        }
                    }

                    if (this.isRestaurant) {
                        if (assignedRiderName) {
                            this.messageService.add({
                                severity: 'success',
                                summary: 'Order Approved & Rider Assigned',
                                detail: `Order "${label}" approved! Automatically assigned to rider ${assignedRiderName} (longest idle rider).${invoice}`,
                                life: 6000,
                            });
                        } else {
                            this.messageService.add({
                                severity: 'warn',
                                summary: 'Order Approved (No Rider Available)',
                                detail: `Order "${label}" approved.${invoice} No delivery rider is currently available — you can assign one later.`,
                                life: 7000,
                            });
                        }
                    } else {
                        this.messageService.add({
                            severity: 'success',
                            summary: 'Success',
                            detail: `Order approved successfully.${invoice}`,
                        });
                    }

                    this.loadOrders();
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
            message: `Mark customer order "${label}" as Delivered? Payment is not recorded yet — use Complete after delivery to add it to Sales.`,
            header: 'Deliver Confirmation',
            icon: 'pi pi-check-circle',
            accept: () => {
                this.customerOrderService
                    .deliver(order.id)
                    .then(() => {
                        this.messageService.add({
                            severity: 'success',
                            summary: 'Delivered',
                            detail: `Order ${label} marked as Delivered. Complete it to record payment in Sales.`,
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

    onComplete(order: CustomerOrderDto): void {
        const label = order.orderNo || `#${order.id}`;
        this.confirmationService.confirm({
            message: `Mark order "${label}" as Completed and insert payment into Sales (PKR ${Number(
                order.totalAmount || 0
            ).toLocaleString()})?`,
            header: 'Complete & Record Payment',
            icon: 'pi pi-wallet',
            accept: () => {
                this.customerOrderService
                    .complete(order.id)
                    .then(() => {
                        this.messageService.add({
                            severity: 'success',
                            summary: 'Completed',
                            detail: `Order ${label} completed and added to Sales.`,
                        });
                        this.loadOrders();
                    })
                    .catch((error) => {
                        const message =
                            error?.error?.error?.message ||
                            error?.error?.message ||
                            error?.message ||
                            'Failed to complete order';
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
                label: order.riderName ? 'Change Rider' : 'Assign Rider',
                icon: 'pi pi-compass',
                visible: this.isRestaurant && (this.isPending(order) || this.isInProgress(order)),
                action: () => this.openAssignRiderDialog(order),
            },
            {
                label: 'Approve',
                icon: 'pi pi-check',
                visible: this.isPending(order),
                action: () => this.onApprove(order),
            },
            {
                label: 'Mark Delivered',
                icon: 'pi pi-truck',
                visible: this.isInProgress(order),
                action: () => this.onDeliver(order),
            },
            {
                label: 'Complete & Record Payment',
                icon: 'pi pi-wallet',
                visible:
                    this.isDelivered(order) ||
                    (this.isInProgress(order) && !order.riderId),
                action: () => this.onComplete(order),
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
