import { Component, OnDestroy, OnInit } from '@angular/core';
import { MessageService } from 'primeng/api';
import { Subscription } from 'rxjs';
import {
    OrderTypes,
    RestaurantKitchenStatuses,
    RestaurantOrderDto,
    RestaurantOrderLineDto,
    RestaurantOrderStatuses,
} from '../../api/restaurant-order';
import { BranchContextService } from '../../service/branch-context.service';
import { ProductService } from '../../service/product.service';
import { RestaurantOrderService } from '../../service/restaurant-order.service';
import { ThermalPrintService } from '../../service/thermal-print.service';

type KitchenFilter = 'all' | 'dinein' | 'takeaway' | 'car';

@Component({
    selector: 'app-kitchen',
    templateUrl: './kitchen.component.html',
    styleUrls: ['./kitchen.component.scss'],
    providers: [MessageService],
})
export class KitchenComponent implements OnInit, OnDestroy {
    orders: RestaurantOrderDto[] = [];
    loading = false;
    lastRefreshedAt: Date | null = null;
    filter: KitchenFilter = 'all';
    nowMs = Date.now();
    statusBusy: { [orderId: number]: boolean } = {};

    private branchId: number | null = null;
    private branchSub?: Subscription;
    private pollTimer: any;
    private clockTimer: any;

    readonly OrderTypes = OrderTypes;
    readonly KitchenStatuses = RestaurantKitchenStatuses;

    constructor(
        private restaurantOrderService: RestaurantOrderService,
        private productService: ProductService,
        private branchContext: BranchContextService,
        private thermalPrintService: ThermalPrintService,
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
        // Keep elapsed time running every second on the KDS.
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

    get filteredOrders(): RestaurantOrderDto[] {
        let list = this.orders || [];
        if (this.filter === 'dinein') {
            list = list.filter((o) => o.orderType === OrderTypes.DineIn);
        } else if (this.filter === 'takeaway') {
            list = list.filter((o) => o.orderType === OrderTypes.Takeaway);
        } else if (this.filter === 'car') {
            list = list.filter((o) => o.orderType === OrderTypes.CarService);
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

    async loadOrders(silent = false): Promise<void> {
        if (!this.branchId) {
            return;
        }
        if (!silent) {
            this.loading = true;
        }
        try {
            const all = await this.restaurantOrderService.getActiveOrders(this.branchId);
            // Kitchen: only open running tickets that have been sent to KOT.
            this.orders = (all || [])
                .filter((o) => o.status === RestaurantOrderStatuses.Open)
                .map((o) => {
                    const lines = (o.lines || (o as any).Lines || [])
                        .map((l: any) => this.normalizeLine(l))
                        .filter((l) => l.isSentToKitchen && l.quantity > 0);
                    return { ...o, lines };
                })
                .filter((o) => (o.lines || []).length > 0)
                .sort((a, b) => {
                    const at = this.orderKitchenTime(a);
                    const bt = this.orderKitchenTime(b);
                    return at - bt;
                });
            this.lastRefreshedAt = new Date();
        } catch (err: any) {
            if (!silent) {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Kitchen',
                    detail: err?.message || 'Failed to load running orders',
                });
            }
        } finally {
            this.loading = false;
        }
    }

    setFilter(filter: KitchenFilter): void {
        this.filter = filter;
    }

    orderTitle(order: RestaurantOrderDto): string {
        if (order.orderType === OrderTypes.Takeaway) {
            return order.tokenNo != null ? `Token #${order.tokenNo}` : order.orderNo;
        }
        if (order.orderType === OrderTypes.CarService) {
            const car = [order.carName, order.carNumber].filter(Boolean).join(' · ');
            return car || order.tableNo || order.orderNo;
        }
        return order.tableNo ? `Table ${order.tableNo}` : order.orderNo;
    }

    hasToken(order: RestaurantOrderDto): boolean {
        return order?.tokenNo != null && Number(order.tokenNo) > 0;
    }

    printToken(order: RestaurantOrderDto, event?: Event): void {
        event?.stopPropagation();
        if (!this.hasToken(order)) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Token',
                detail: 'No token number on this order yet.',
            });
            return;
        }
        try {
            this.thermalPrintService.printTokenSlip({
                tokenNo: order.tokenNo,
                orderNo: order.orderNo,
                tableNo: order.tableNo,
                orderTypeName: order.orderTypeName,
                orderTypeLabel: this.orderTypeLabel(order),
                carName: order.carName,
                carNumber: order.carNumber,
                waiterName: order.waiterName,
                customerName: order.customerName,
            });
        } catch (err: any) {
            this.messageService.add({
                severity: 'error',
                summary: 'Token Print',
                detail: err?.message || 'Failed to print token',
            });
        }
    }

    orderTypeLabel(order: RestaurantOrderDto): string {
        if (order.orderTypeName) {
            return order.orderTypeName;
        }
        if (order.orderType === OrderTypes.Takeaway) return 'Takeaway';
        if (order.orderType === OrderTypes.CarService) return 'Car';
        if (order.orderType === OrderTypes.Delivery) return 'Delivery';
        return 'Dine-in';
    }

    orderTypeClass(order: RestaurantOrderDto): string {
        if (order.orderType === OrderTypes.Takeaway) return 'type-takeaway';
        if (order.orderType === OrderTypes.CarService) return 'type-car';
        if (order.orderType === OrderTypes.Delivery) return 'type-delivery';
        return 'type-dinein';
    }

    kitchenStatus(order: RestaurantOrderDto): number {
        const status = order?.kitchenStatus ?? RestaurantKitchenStatuses.None;
        if (status === RestaurantKitchenStatuses.ReadyToPick) {
            return RestaurantKitchenStatuses.ReadyToPick;
        }
        // KOT tickets default to In Progress until kitchen marks Ready.
        return RestaurantKitchenStatuses.InProgress;
    }

    isReadyToPick(order: RestaurantOrderDto): boolean {
        return this.kitchenStatus(order) === RestaurantKitchenStatuses.ReadyToPick;
    }

    kitchenStatusLabel(order: RestaurantOrderDto): string {
        return this.isReadyToPick(order) ? 'Ready to Pick' : 'In Progress';
    }

    kitchenLines(order: RestaurantOrderDto): RestaurantOrderLineDto[] {
        return order.lines || [];
    }

    getLineNote(line: RestaurantOrderLineDto | any): string {
        const note = (line?.kitchenNotes ?? line?.KitchenNotes ?? '').toString().trim();
        return note;
    }

    getLineModifiers(line: RestaurantOrderLineDto | any): string[] {
        const raw = line?.selectedModifiers ?? line?.SelectedModifiers;
        if (!raw) {
            return [];
        }

        let parts: string[] = [];
        try {
            const parsed = typeof raw === 'string' ? JSON.parse(raw) : raw;
            if (Array.isArray(parsed)) {
                parts = parsed.map((m) => (typeof m === 'string' ? m : String(m)));
            } else {
                parts = [String(raw)];
            }
        } catch {
            parts = [String(raw)];
        }

        const result: string[] = [];
        for (const part of parts) {
            const text = (part || '').trim();
            if (!text) {
                continue;
            }
            if (/^DEAL:\s*/i.test(text)) {
                const summary = text.replace(/^DEAL:\s*/i, '').trim();
                result.push(...this.splitDealSummary(summary));
                continue;
            }
            // Deal summary sometimes stored without DEAL: prefix but with " • " separators.
            if (text.includes(' • ') || text.includes(' â€¢ ')) {
                result.push(...this.splitDealSummary(text));
                continue;
            }
            result.push(text);
        }
        return result.filter(Boolean);
    }

    private splitDealSummary(summary: string): string[] {
        if (!summary) {
            return [];
        }
        const sep = summary.includes(' • ')
            ? ' • '
            : summary.includes(' â€¢ ')
              ? ' â€¢ '
              : null;
        if (sep) {
            return summary
                .split(sep)
                .map((s) => s.trim())
                .filter(Boolean);
        }
        return [summary.trim()].filter(Boolean);
    }

    async markReadyToPick(order: RestaurantOrderDto): Promise<void> {
        if (!order?.id || this.statusBusy[order.id]) {
            return;
        }
        this.statusBusy[order.id] = true;
        try {
            const updated = await this.restaurantOrderService.markReadyToPick(order.id);
            this.patchOrderKitchenStatus(order.id, updated);
            this.messageService.add({
                severity: 'success',
                summary: 'Kitchen',
                detail: `${this.orderTitle(order)} marked Ready to Pick`,
            });
        } catch (err: any) {
            this.messageService.add({
                severity: 'error',
                summary: 'Kitchen',
                detail: err?.message || 'Failed to mark ready',
            });
        } finally {
            this.statusBusy[order.id] = false;
        }
    }

    async markInProgress(order: RestaurantOrderDto): Promise<void> {
        if (!order?.id || this.statusBusy[order.id]) {
            return;
        }
        this.statusBusy[order.id] = true;
        try {
            const updated = await this.restaurantOrderService.markKitchenInProgress(order.id);
            this.patchOrderKitchenStatus(order.id, updated);
        } catch (err: any) {
            this.messageService.add({
                severity: 'error',
                summary: 'Kitchen',
                detail: err?.message || 'Failed to update status',
            });
        } finally {
            this.statusBusy[order.id] = false;
        }
    }

    elapsedLabel(order: RestaurantOrderDto): string {
        const start = this.orderKitchenTime(order);
        if (!start) {
            return '';
        }
        const totalSec = Math.max(0, Math.floor((this.nowMs - start) / 1000));
        const mins = Math.floor(totalSec / 60);
        const secs = totalSec % 60;
        if (mins < 1) {
            return `${secs}s`;
        }
        if (mins < 60) {
            return `${mins}m ${secs.toString().padStart(2, '0')}s`;
        }
        const h = Math.floor(mins / 60);
        const m = mins % 60;
        return `${h}h ${m}m`;
    }

    isLate(order: RestaurantOrderDto): boolean {
        const start = this.orderKitchenTime(order);
        if (!start) return false;
        return this.nowMs - start > 20 * 60 * 1000;
    }

    trackByOrderId(_: number, order: RestaurantOrderDto): number {
        return order.id;
    }

    getLineImage(line: RestaurantOrderLineDto): string | null {
        const path = line?.imagePath;
        if (!path) {
            return null;
        }
        return this.productService.getImageUrl(path);
    }

    getLineInitials(line: RestaurantOrderLineDto): string {
        const name = (line?.productName || '').trim();
        if (!name) {
            return '?';
        }
        const parts = name.split(/\s+/).filter(Boolean);
        if (parts.length >= 2) {
            return (parts[0].charAt(0) + parts[1].charAt(0)).toUpperCase();
        }
        return name.substring(0, 2).toUpperCase();
    }

    private orderKitchenTime(order: RestaurantOrderDto): number {
        const sentTimes = (order.lines || [])
            .map((l) => (l.sentToKitchenTime ? new Date(l.sentToKitchenTime).getTime() : 0))
            .filter((t) => t > 0);
        if (sentTimes.length) {
            return Math.min(...sentTimes);
        }
        return order.creationTime ? new Date(order.creationTime).getTime() : 0;
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

    private patchOrderKitchenStatus(orderId: number, updated: RestaurantOrderDto): void {
        this.orders = (this.orders || []).map((o) => {
            if (o.id !== orderId) {
                return o;
            }
            return {
                ...o,
                kitchenStatus: updated?.kitchenStatus ?? o.kitchenStatus,
                kitchenStatusName: updated?.kitchenStatusName ?? o.kitchenStatusName,
                kitchenReadyTime: updated?.kitchenReadyTime ?? o.kitchenReadyTime,
            };
        });
    }
}
