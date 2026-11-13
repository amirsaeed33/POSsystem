import { Component, OnDestroy, OnInit } from '@angular/core';
import { ConfirmationService, MessageService } from 'primeng/api';
import { Subscription } from 'rxjs';
import {
    CustomerOrderDto,
    CustomerOrderStatus,
    DeliveryRiderStatusDto,
} from '../../api/customer-order';
import {
    OrderTypes,
    RestaurantKitchenStatuses,
    RestaurantOrderDto,
    RestaurantOrderLineDto,
    RestaurantOrderStatuses,
} from '../../api/restaurant-order';
import { PaymentType } from '../../api/sale';
import { BranchContextService } from '../../service/branch-context.service';
import { CustomerOrderService } from '../../service/customer-order.service';
import { RestaurantOrderService } from '../../service/restaurant-order.service';

type RunningOrderFilter = 'all' | 'dinein' | 'takeaway' | 'car' | 'online';

export interface RunningOrderRow extends RestaurantOrderDto {
    customerOrder?: CustomerOrderDto | null;
}

@Component({
    selector: 'app-kitchen',
    templateUrl: './kitchen.component.html',
    styleUrls: ['./kitchen.component.scss'],
    providers: [MessageService, ConfirmationService],
})
export class KitchenComponent implements OnInit, OnDestroy {
    orders: RunningOrderRow[] = [];
    loading = false;
    lastRefreshedAt: Date | null = null;
    filter: RunningOrderFilter = 'all';
    nowMs = Date.now();
    actionBusyId: number | null = null;

    paymentDialogVisible = false;
    payingOrder: RunningOrderRow | null = null;
    paidAmount: number | null = null;
    paymentNotes = '';
    settling = false;

    assignRiderDialogVisible = false;
    selectedOrderForRider: RunningOrderRow | null = null;
    availableRiders: DeliveryRiderStatusDto[] = [];
    selectedRider: DeliveryRiderStatusDto | null = null;
    loadingRiders = false;
    assigningRider = false;

    private branchId: number | null = null;
    private branchSub?: Subscription;
    private pollTimer: any;
    private clockTimer: any;
    private customerOrdersById = new Map<number, CustomerOrderDto>();

    readonly OrderTypes = OrderTypes;
    readonly OrderStatuses = RestaurantOrderStatuses;
    readonly KitchenStatuses = RestaurantKitchenStatuses;

    constructor(
        private restaurantOrderService: RestaurantOrderService,
        private customerOrderService: CustomerOrderService,
        private branchContext: BranchContextService,
        private confirmationService: ConfirmationService,
        private messageService: MessageService
    ) {}

    ngOnInit(): void {
        this.branchSub = this.branchContext.currentBranch$.subscribe((branch) => {
            const id = branch?.id ?? this.branchContext.getBranchId();
            if (!id) {
                this.branchId = null;
                this.orders = [];
                return;
            }
            if (this.branchId !== id) {
                this.branchId = id;
                void this.loadOrders();
            }
        });

        this.pollTimer = setInterval(() => void this.loadOrders(true), 12000);
        this.clockTimer = setInterval(() => {
            this.nowMs = Date.now();
        }, 1000);
    }

    ngOnDestroy(): void {
        this.branchSub?.unsubscribe();
        if (this.pollTimer) {
            clearInterval(this.pollTimer);
        }
        if (this.clockTimer) {
            clearInterval(this.clockTimer);
        }
    }

    get filteredOrders(): RunningOrderRow[] {
        let list = this.orders || [];
        if (this.filter === 'dinein') {
            list = list.filter((o) => o.orderType === OrderTypes.DineIn);
        } else if (this.filter === 'takeaway') {
            list = list.filter((o) => o.orderType === OrderTypes.Takeaway);
        } else if (this.filter === 'car') {
            list = list.filter((o) => o.orderType === OrderTypes.CarService);
        } else if (this.filter === 'online') {
            list = list.filter((o) => o.orderType === OrderTypes.Delivery);
        }
        return list;
    }

    get openCount(): number {
        return this.orders.length;
    }

    get dineInCount(): number {
        return (this.orders || []).filter((o) => o.orderType === OrderTypes.DineIn).length;
    }

    get takeawayCount(): number {
        return (this.orders || []).filter((o) => o.orderType === OrderTypes.Takeaway).length;
    }

    get carCount(): number {
        return (this.orders || []).filter((o) => o.orderType === OrderTypes.CarService).length;
    }

    get onlineCount(): number {
        return (this.orders || []).filter((o) => o.orderType === OrderTypes.Delivery).length;
    }

    get changeToReturn(): number {
        if (!this.payingOrder || this.paidAmount == null) {
            return 0;
        }
        const total = this.paymentTotal(this.payingOrder);
        const paid = Number(this.paidAmount) || 0;
        return Math.max(0, Math.round((paid - total) * 100) / 100);
    }

    get remainingDue(): number {
        if (!this.payingOrder || this.paidAmount == null) {
            return 0;
        }
        const total = this.paymentTotal(this.payingOrder);
        const paid = Number(this.paidAmount) || 0;
        return Math.max(0, Math.round((total - paid) * 100) / 100);
    }

    async loadOrders(silent = false): Promise<void> {
        if (!this.branchId) {
            return;
        }
        if (!silent) {
            this.loading = true;
        }
        try {
            const [all, customerPage] = await Promise.all([
                this.restaurantOrderService.getActiveOrders(this.branchId),
                this.customerOrderService.getAll({ maxResultCount: 300, skipCount: 0 }),
            ]);

            this.customerOrdersById.clear();
            for (const co of customerPage?.items || []) {
                if (co?.id) {
                    this.customerOrdersById.set(co.id, co);
                }
            }

            this.orders = (all || [])
                .map((o) => this.toRunningRow(o))
                .filter((o) => this.isRunningUnpaidOrder(o))
                .sort((a, b) => this.orderStartTime(b) - this.orderStartTime(a));
            this.lastRefreshedAt = new Date();
        } catch (err: any) {
            if (!silent) {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Running Orders',
                    detail: err?.message || 'Failed to load running orders',
                });
            }
        } finally {
            this.loading = false;
        }
    }

    private toRunningRow(raw: any): RunningOrderRow {
        const order = this.normalizeOrder(raw);
        const coId = order.customerOrderId || null;
        const customerOrder = coId ? this.customerOrdersById.get(coId) || null : null;
        return { ...order, customerOrder };
    }

    /**
     * Unfinished running orders only. Online Completed/Rejected never appear.
     * Delivered online stays until Paid (Complete).
     */
    private isRunningUnpaidOrder(order: RunningOrderRow): boolean {
        if (
            order.status === RestaurantOrderStatuses.Settled ||
            order.status === RestaurantOrderStatuses.Cancelled
        ) {
            return false;
        }

        if (
            order.status !== RestaurantOrderStatuses.Open &&
            order.status !== RestaurantOrderStatuses.Billed
        ) {
            return false;
        }

        const co = order.customerOrder;
        if (co) {
            if (
                co.status === CustomerOrderStatus.Completed ||
                co.status === CustomerOrderStatus.Rejected
            ) {
                return false;
            }
            // Linked online ticket with active customer order (incl. Delivered awaiting pay).
            return true;
        }

        if (order.status === RestaurantOrderStatuses.Billed || order.billedTime) {
            return true;
        }

        const lines = order.lines || [];
        const hasKotEvidence = lines.some(
            (l) => !!l.isSentToKitchen || Number(l.printedQuantity || 0) > 0
        );

        return hasKotEvidence || !!order.lastPrintedTime;
    }

    setFilter(filter: RunningOrderFilter): void {
        this.filter = filter;
    }

    isOnline(order: RunningOrderRow): boolean {
        return order.orderType === OrderTypes.Delivery && !!order.customerOrderId;
    }

    isOnlineInProgress(order: RunningOrderRow): boolean {
        const s = order.customerOrder?.status;
        return (
            s === CustomerOrderStatus.Pending ||
            s === CustomerOrderStatus.Approved ||
            s === CustomerOrderStatus.InProgress
        );
    }

    isOnlineDelivered(order: RunningOrderRow): boolean {
        return order.customerOrder?.status === CustomerOrderStatus.Delivered;
    }

    hasRider(order: RunningOrderRow): boolean {
        return !!(order.customerOrder?.riderId || order.customerOrder?.riderName);
    }

    /** Online: Assign Rider while still in progress. */
    canAssignRider(order: RunningOrderRow): boolean {
        return this.isOnline(order) && this.isOnlineInProgress(order);
    }

    /** Online: Complete Order (mark Delivered) after rider assigned. */
    canCompleteOrder(order: RunningOrderRow): boolean {
        return this.isOnline(order) && this.isOnlineInProgress(order) && this.hasRider(order);
    }

    /** Paid: online only after Delivered; dine-in/takeaway/car anytime on running ticket. */
    canPay(order: RunningOrderRow): boolean {
        if (this.isOnline(order)) {
            return this.isOnlineDelivered(order);
        }
        return true;
    }

    orderTitle(order: RunningOrderRow): string {
        if (order.orderType === OrderTypes.Delivery) {
            const token = order.tokenNo ?? order.customerOrder?.tokenNo;
            return token != null ? `Token #${token}` : order.orderNo || 'Online';
        }
        if (order.orderType === OrderTypes.Takeaway) {
            return order.tokenNo != null ? `Token #${order.tokenNo}` : order.orderNo;
        }
        if (order.orderType === OrderTypes.CarService) {
            const car = [order.carName, order.carNumber].filter(Boolean).join(' · ');
            return car || order.tableNo || order.orderNo;
        }
        const table = (order.tableNo || '').trim();
        if (!table) {
            return order.orderNo;
        }
        return /^table\b/i.test(table) ? table : `Table ${table}`;
    }

    orderTypeLabel(order: RunningOrderRow): string {
        if (order.orderType === OrderTypes.Delivery) return 'Online';
        if (order.orderType === OrderTypes.Takeaway) return 'Takeaway';
        if (order.orderType === OrderTypes.CarService) return 'Car';
        return 'Dine-in';
    }

    orderTypeClass(order: RunningOrderRow): string {
        if (order.orderType === OrderTypes.Takeaway) return 'type-takeaway';
        if (order.orderType === OrderTypes.CarService) return 'type-car';
        if (order.orderType === OrderTypes.Delivery) return 'type-online';
        return 'type-dinein';
    }

    customerDisplayName(order: RunningOrderRow): string {
        return (
            order.customerName ||
            order.customerOrder?.customerName ||
            ''
        ).trim();
    }

    /** Real staff/waiter name — ignore placeholder "Online". */
    waiterDisplayName(order: RunningOrderRow): string {
        const clean = (value: string) =>
            value.replace(/\s*\([^)]*\)\s*$/, '').trim();
        const isPlaceholder = (value: string) =>
            !value || /^online\b/i.test(value) || /^online\s+delivery$/i.test(value);

        const raw = clean(order.waiterName || '');
        if (!isPlaceholder(raw)) {
            return raw;
        }
        const printed = clean(order.lastPrintedBy || '');
        if (!isPlaceholder(printed)) {
            return printed;
        }
        return '';
    }

    riderDisplayName(order: RunningOrderRow): string {
        return (order.customerOrder?.riderName || '').trim();
    }

    statusLabel(order: RunningOrderRow): string {
        if (this.isOnline(order) && order.customerOrder) {
            const s = order.customerOrder.status;
            if (s === CustomerOrderStatus.Delivered) {
                return 'Delivered';
            }
            if (s === CustomerOrderStatus.Completed) {
                return 'Completed';
            }
            if (
                s === CustomerOrderStatus.Approved ||
                s === CustomerOrderStatus.InProgress ||
                s === CustomerOrderStatus.Pending
            ) {
                return this.hasRider(order) ? 'In Progress' : 'Awaiting Rider';
            }
        }

        if (order.status === RestaurantOrderStatuses.Billed || !!order.billedTime) {
            return 'Guest Bill';
        }

        const kitchen = Number(order.kitchenStatus ?? RestaurantKitchenStatuses.None);
        if (kitchen === RestaurantKitchenStatuses.ReadyToPick) {
            return 'Ready to Pick';
        }
        if (kitchen === RestaurantKitchenStatuses.InProgress) {
            return 'In Progress';
        }

        return 'KOT Sent';
    }

    statusClass(order: RunningOrderRow): string {
        if (this.isOnline(order) && order.customerOrder) {
            const s = order.customerOrder.status;
            if (s === CustomerOrderStatus.Delivered) {
                return 'status-delivered';
            }
            if (!this.hasRider(order) && this.isOnlineInProgress(order)) {
                return 'status-awaiting';
            }
            if (this.isOnlineInProgress(order)) {
                return 'status-progress';
            }
        }

        if (order.status === RestaurantOrderStatuses.Billed || !!order.billedTime) {
            return 'status-billed';
        }
        const kitchen = Number(order.kitchenStatus ?? RestaurantKitchenStatuses.None);
        if (kitchen === RestaurantKitchenStatuses.ReadyToPick) {
            return 'status-ready';
        }
        if (kitchen === RestaurantKitchenStatuses.InProgress) {
            return 'status-progress';
        }
        return 'status-kot';
    }

    itemCount(order: RunningOrderRow): number {
        return (order.lines || []).reduce((sum, l) => sum + (Number(l.quantity) || 0), 0);
    }

    elapsedLabel(order: RunningOrderRow): string {
        const start = this.orderStartTime(order);
        if (!start) {
            return '—';
        }
        const totalSec = Math.max(0, Math.floor((this.nowMs - start) / 1000));
        const mins = Math.floor(totalSec / 60);
        const secs = totalSec % 60;
        return `${String(mins).padStart(2, '0')}:${String(secs).padStart(2, '0')}`;
    }

    paymentTotal(order: RunningOrderRow): number {
        if (order.customerOrder?.totalAmount != null) {
            return Math.round(Number(order.customerOrder.totalAmount) * 100) / 100;
        }
        return Math.round((Number(order.grandTotal) || 0) * 100) / 100;
    }

    async openAssignRider(order: RunningOrderRow): Promise<void> {
        if (!this.canAssignRider(order) || !order.customerOrder) {
            return;
        }
        this.selectedOrderForRider = order;
        this.selectedRider = null;
        this.assignRiderDialogVisible = true;
        this.loadingRiders = true;
        try {
            const branchId =
                order.customerOrder.branchId || this.branchId || undefined;
            this.availableRiders = await this.customerOrderService.getAvailableRiders(
                branchId,
                order.customerOrder.id
            );
            if (order.customerOrder.riderId) {
                this.selectedRider =
                    this.availableRiders.find(
                        (r) => r.riderId === order.customerOrder!.riderId
                    ) || null;
            }
        } catch (err: any) {
            this.messageService.add({
                severity: 'error',
                summary: 'Riders',
                detail: err?.message || 'Failed to load delivery riders',
            });
        } finally {
            this.loadingRiders = false;
        }
    }

    async confirmAssignRider(): Promise<void> {
        const order = this.selectedOrderForRider;
        if (!order?.customerOrder?.id || !this.selectedRider) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Select Rider',
                detail: 'Please select an available delivery rider.',
            });
            return;
        }
        if (this.selectedRider.isBusy) {
            this.messageService.add({
                severity: 'error',
                summary: 'Rider Busy',
                detail: `Rider ${this.selectedRider.riderName} is already delivering another order.`,
            });
            return;
        }

        this.assigningRider = true;
        this.actionBusyId = order.id;
        try {
            const updated = await this.customerOrderService.assignRider({
                orderId: order.customerOrder.id,
                riderId: this.selectedRider.riderId,
                riderName: this.selectedRider.riderName,
                riderPhone: this.selectedRider.riderPhone,
            });
            this.customerOrdersById.set(updated.id, updated);
            order.customerOrder = updated;
            this.messageService.add({
                severity: 'success',
                summary: 'Rider Assigned',
                detail: `Assigned to ${this.selectedRider.riderName}.`,
            });
            this.assignRiderDialogVisible = false;
            this.selectedOrderForRider = null;
            await this.loadOrders(true);
        } catch (err: any) {
            this.messageService.add({
                severity: 'error',
                summary: 'Assign Failed',
                detail: err?.message || 'Failed to assign rider',
            });
        } finally {
            this.assigningRider = false;
            this.actionBusyId = null;
        }
    }

    confirmCompleteOrder(order: RunningOrderRow): void {
        if (!this.canCompleteOrder(order) || !order.customerOrder?.id) {
            return;
        }
        this.confirmationService.confirm({
            message: `Mark "${this.orderTitle(order)}" as Delivered? Then you can take payment.`,
            header: 'Complete Order',
            icon: 'pi pi-check-circle',
            acceptButtonStyleClass: 'p-button-success',
            accept: () => void this.completeOrderDeliver(order),
        });
    }

    private async completeOrderDeliver(order: RunningOrderRow): Promise<void> {
        if (!order.customerOrder?.id) {
            return;
        }
        this.actionBusyId = order.id;
        try {
            await this.customerOrderService.deliver(order.customerOrder.id);
            const refreshed = await this.customerOrderService.get(order.customerOrder.id);
            this.customerOrdersById.set(refreshed.id, refreshed);
            order.customerOrder = refreshed;
            this.messageService.add({
                severity: 'success',
                summary: 'Delivered',
                detail: 'Order marked delivered. Use Paid to record payment.',
            });
            await this.loadOrders(true);
        } catch (err: any) {
            this.messageService.add({
                severity: 'error',
                summary: 'Complete Failed',
                detail: err?.message || 'Failed to mark delivered',
            });
        } finally {
            this.actionBusyId = null;
        }
    }

    payOrder(order: RunningOrderRow): void {
        if (!this.canPay(order) || !order?.id || this.actionBusyId === order.id) {
            return;
        }
        this.payingOrder = order;
        this.paidAmount = this.paymentTotal(order);
        this.paymentNotes = '';
        this.paymentDialogVisible = true;
    }

    onPaymentDialogHide(): void {
        if (!this.settling) {
            this.paymentDialogVisible = false;
            this.payingOrder = null;
            this.paidAmount = null;
            this.paymentNotes = '';
        }
    }

    onPaidAmountFocus(event: any): void {
        try {
            event?.target?.select?.();
        } catch {
            /* ignore */
        }
    }

    async confirmPayment(): Promise<void> {
        if (!this.payingOrder?.id || this.settling) {
            return;
        }

        if (this.paidAmount == null || this.paidAmount < 0) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Validation',
                detail: 'Enter the amount paid by the customer',
            });
            return;
        }

        const order = this.payingOrder;
        const total = this.paymentTotal(order);
        const paid = Math.round((Number(this.paidAmount) || 0) * 100) / 100;
        const change = Math.max(0, Math.round((paid - total) * 100) / 100);
        const remaining = Math.max(0, Math.round((total - paid) * 100) / 100);

        this.settling = true;
        this.actionBusyId = order.id;
        try {
            if (this.isOnline(order) && order.customerOrderId) {
                await this.customerOrderService.complete(order.customerOrderId, paid);
            } else {
                const appliedCash = Math.min(paid, total);
                const paymentType = paid >= total ? PaymentType.Cash : PaymentType.Mixed;
                const cashAmount = paid >= total ? 0 : appliedCash;
                const customerId =
                    order.customerId && order.customerId > 0 ? order.customerId : 1;

                await this.restaurantOrderService.settleOrder({
                    orderId: order.id,
                    customerId,
                    paymentType,
                    cashAmount,
                    cardAmount: 0,
                    notes: this.paymentNotes?.trim() || undefined,
                });
            }

            const detail =
                change > 0
                    ? `Settled. Return ${change.toFixed(2)} change.`
                    : remaining > 0
                      ? `Settled. Balance ${remaining.toFixed(2)} on customer credit.`
                      : `Order ${order.orderNo || '#' + order.id} paid.`;

            this.messageService.add({
                severity: 'success',
                summary: 'Payment Done',
                detail,
            });

            this.paymentDialogVisible = false;
            this.payingOrder = null;
            this.paidAmount = null;
            this.paymentNotes = '';

            this.orders = this.orders.filter((o) => o.id !== order.id);
            await this.loadOrders(true);
        } catch (err: any) {
            this.messageService.add({
                severity: 'error',
                summary: 'Payment Failed',
                detail: err?.message || 'Failed to settle order',
            });
        } finally {
            this.settling = false;
            this.actionBusyId = null;
        }
    }

    confirmCancel(order: RunningOrderRow): void {
        if (!order?.id || this.actionBusyId === order.id) {
            return;
        }
        this.confirmationService.confirm({
            message: `Cancel "${this.orderTitle(order)}" (${order.orderNo})? It will leave Running Orders.`,
            header: 'Cancel Order',
            icon: 'pi pi-exclamation-triangle',
            acceptButtonStyleClass: 'p-button-danger',
            accept: () => void this.cancelOrder(order),
        });
    }

    private async cancelOrder(order: RunningOrderRow): Promise<void> {
        this.actionBusyId = order.id;
        try {
            await this.restaurantOrderService.cancelOrder(order.id);
            this.messageService.add({
                severity: 'success',
                summary: 'Cancelled',
                detail: `Order ${order.orderNo || '#' + order.id} cancelled.`,
            });
            if (this.payingOrder?.id === order.id) {
                this.paymentDialogVisible = false;
                this.payingOrder = null;
            }
            this.orders = this.orders.filter((o) => o.id !== order.id);
            await this.loadOrders(true);
        } catch (err: any) {
            this.messageService.add({
                severity: 'error',
                summary: 'Cancel Failed',
                detail: err?.message || 'Failed to cancel order',
            });
        } finally {
            this.actionBusyId = null;
        }
    }

    trackByOrderId(_: number, order: RunningOrderRow): number {
        return order.id;
    }

    private orderStartTime(order: RunningOrderRow): number {
        const sentTimes = (order.lines || [])
            .map((l) => (l.sentToKitchenTime ? new Date(l.sentToKitchenTime).getTime() : 0))
            .filter((t) => t > 0);
        if (sentTimes.length) {
            return Math.min(...sentTimes);
        }
        if (order.lastPrintedTime) {
            return new Date(order.lastPrintedTime).getTime();
        }
        if (order.billedTime) {
            return new Date(order.billedTime).getTime();
        }
        return order.creationTime ? new Date(order.creationTime).getTime() : 0;
    }

    private normalizeOrder(raw: any): RestaurantOrderDto {
        const lines = (raw?.lines || raw?.Lines || []).map((l: any) => this.normalizeLine(l));
        return {
            ...raw,
            id: raw?.id ?? raw?.Id ?? 0,
            branchId: raw?.branchId ?? raw?.BranchId ?? 0,
            orderNo: raw?.orderNo ?? raw?.OrderNo ?? '',
            tokenNo: raw?.tokenNo ?? raw?.TokenNo ?? null,
            tableId: raw?.tableId ?? raw?.TableId ?? null,
            tableNo: raw?.tableNo ?? raw?.TableNo ?? '',
            orderType: Number(raw?.orderType ?? raw?.OrderType ?? 0) as any,
            orderTypeName: raw?.orderTypeName ?? raw?.OrderTypeName ?? '',
            carName: raw?.carName ?? raw?.CarName,
            carNumber: raw?.carNumber ?? raw?.CarNumber,
            carLocation: raw?.carLocation ?? raw?.CarLocation,
            status: Number(raw?.status ?? raw?.Status ?? 0) as any,
            statusName: raw?.statusName ?? raw?.StatusName ?? '',
            kitchenStatus: Number(
                raw?.kitchenStatus ?? raw?.KitchenStatus ?? RestaurantKitchenStatuses.None
            ) as any,
            kitchenStatusName: raw?.kitchenStatusName ?? raw?.KitchenStatusName,
            customerId: raw?.customerId ?? raw?.CustomerId ?? null,
            customerOrderId: raw?.customerOrderId ?? raw?.CustomerOrderId ?? null,
            customerName: raw?.customerName ?? raw?.CustomerName,
            waiterName: raw?.waiterName ?? raw?.WaiterName,
            notes: raw?.notes ?? raw?.Notes,
            subTotal: Number(raw?.subTotal ?? raw?.SubTotal ?? 0),
            discountPercent: Number(raw?.discountPercent ?? raw?.DiscountPercent ?? 0),
            discountAmount: Number(raw?.discountAmount ?? raw?.DiscountAmount ?? 0),
            taxPercent: Number(raw?.taxPercent ?? raw?.TaxPercent ?? 0),
            taxAmount: Number(raw?.taxAmount ?? raw?.TaxAmount ?? 0),
            serviceChargePercent: Number(
                raw?.serviceChargePercent ?? raw?.ServiceChargePercent ?? 0
            ),
            serviceChargeAmount: Number(
                raw?.serviceChargeAmount ?? raw?.ServiceChargeAmount ?? 0
            ),
            grandTotal: Number(raw?.grandTotal ?? raw?.GrandTotal ?? 0),
            saleId: raw?.saleId ?? raw?.SaleId ?? null,
            creationTime: raw?.creationTime ?? raw?.CreationTime,
            billedTime: raw?.billedTime ?? raw?.BilledTime ?? null,
            settledTime: raw?.settledTime ?? raw?.SettledTime ?? null,
            lastPrintedBy: raw?.lastPrintedBy ?? raw?.LastPrintedBy,
            lastPrintedTime: raw?.lastPrintedTime ?? raw?.LastPrintedTime ?? null,
            lines,
        };
    }

    private normalizeLine(line: any): RestaurantOrderLineDto {
        return {
            id: line?.id ?? line?.Id ?? 0,
            orderId: line?.orderId ?? line?.OrderId ?? 0,
            productId: line?.productId ?? line?.ProductId ?? 0,
            productName: line?.productName ?? line?.ProductName ?? '',
            imagePath: line?.imagePath ?? line?.ImagePath,
            quantity: Number(line?.quantity ?? line?.Quantity ?? 0),
            unitPrice: Number(line?.unitPrice ?? line?.UnitPrice ?? 0),
            lineTotal: Number(line?.lineTotal ?? line?.LineTotal ?? 0),
            kitchenNotes: (line?.kitchenNotes ?? line?.KitchenNotes ?? '') || undefined,
            selectedModifiers: (line?.selectedModifiers ?? line?.SelectedModifiers ?? '') || undefined,
            isSentToKitchen: !!(line?.isSentToKitchen ?? line?.IsSentToKitchen),
            printedQuantity: Number(line?.printedQuantity ?? line?.PrintedQuantity ?? 0),
            sentToKitchenTime: line?.sentToKitchenTime ?? line?.SentToKitchenTime ?? null,
        };
    }
}
