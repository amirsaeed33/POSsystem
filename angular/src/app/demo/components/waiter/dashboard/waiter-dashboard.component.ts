import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { MessageService } from 'primeng/api';
import { Subscription } from 'rxjs';
import {
    OrderTypes,
    RestaurantKitchenStatuses,
    RestaurantOrderDto,
    RestaurantOrderStatuses,
    WaiterDashboardDto,
} from 'src/app/demo/api/restaurant-order';
import { AuthService } from 'src/app/demo/service/auth.service';
import { BranchContextService } from 'src/app/demo/service/branch-context.service';
import { RestaurantOrderService } from 'src/app/demo/service/restaurant-order.service';

type WaiterOrderFilter = 'all' | 'pending' | 'ready' | 'billed';

@Component({
    selector: 'app-waiter-dashboard',
    templateUrl: './waiter-dashboard.component.html',
    styleUrls: ['./waiter-dashboard.component.scss'],
    providers: [MessageService],
})
export class WaiterDashboardComponent implements OnInit, OnDestroy {
    loading = false;
    data: WaiterDashboardDto | null = null;
    filter: WaiterOrderFilter = 'all';
    nowMs = Date.now();

    private branchSub?: Subscription;
    private clockTimer: ReturnType<typeof setInterval> | null = null;
    private pollTimer: ReturnType<typeof setInterval> | null = null;

    constructor(
        private restaurantOrderService: RestaurantOrderService,
        private branchContext: BranchContextService,
        private authService: AuthService,
        private router: Router,
        private messageService: MessageService,
        private cdr: ChangeDetectorRef
    ) {}

    ngOnInit(): void {
        void this.branchContext.ensureLoaded().then(() => this.loadData());
        this.branchSub = this.branchContext.currentBranch$.subscribe((b) => {
            if (b?.id) {
                void this.loadData();
            }
        });
        this.clockTimer = setInterval(() => {
            this.nowMs = Date.now();
            this.cdr.detectChanges();
        }, 1000);
        this.pollTimer = setInterval(() => void this.loadData(true), 15000);
    }

    ngOnDestroy(): void {
        this.branchSub?.unsubscribe();
        if (this.clockTimer) {
            clearInterval(this.clockTimer);
        }
        if (this.pollTimer) {
            clearInterval(this.pollTimer);
        }
    }

    get waiterName(): string {
        return (
            this.data?.waiterName ||
            this.authService.getPrinterIdentity().name ||
            'Waiter'
        );
    }

    get branchDisplayName(): string {
        return this.branchContext.getCurrentBranch()?.name || 'Branch';
    }

    get filteredOrders(): RestaurantOrderDto[] {
        const list = this.data?.activeOrders || [];
        if (this.filter === 'pending') {
            return list.filter((o) => this.isPending(o));
        }
        if (this.filter === 'ready') {
            return list.filter((o) => this.isReady(o));
        }
        if (this.filter === 'billed') {
            return list.filter((o) => o.status === RestaurantOrderStatuses.Billed);
        }
        return list;
    }

    async loadData(silent = false): Promise<void> {
        const branchId = this.branchContext.getBranchId();
        if (!branchId) {
            return;
        }
        if (!silent) {
            this.loading = true;
        }
        try {
            this.data = await this.restaurantOrderService.getWaiterDashboard(branchId);
        } catch (err: any) {
            if (!silent) {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Dashboard',
                    detail: err?.message || 'Failed to load waiter dashboard',
                });
            }
        } finally {
            this.loading = false;
        }
    }

    setFilter(filter: WaiterOrderFilter): void {
        this.filter = filter;
    }

    isPending(order: RestaurantOrderDto): boolean {
        return (
            order.status === RestaurantOrderStatuses.Open &&
            order.kitchenStatus !== RestaurantKitchenStatuses.ReadyToPick
        );
    }

    isReady(order: RestaurantOrderDto): boolean {
        return (
            order.status === RestaurantOrderStatuses.Open &&
            order.kitchenStatus === RestaurantKitchenStatuses.ReadyToPick
        );
    }

    statusLabel(order: RestaurantOrderDto): string {
        if (order.status === RestaurantOrderStatuses.Billed) {
            return 'Billed';
        }
        if (this.isReady(order)) {
            return 'Ready to Serve';
        }
        if (order.kitchenStatus === RestaurantKitchenStatuses.InProgress) {
            return 'In Kitchen';
        }
        return order.statusName || 'Pending';
    }

    statusClass(order: RestaurantOrderDto): string {
        if (order.status === RestaurantOrderStatuses.Billed) {
            return 'is-billed';
        }
        if (this.isReady(order)) {
            return 'is-ready';
        }
        if (order.kitchenStatus === RestaurantKitchenStatuses.InProgress) {
            return 'is-kitchen';
        }
        return 'is-pending';
    }

    orderTitle(order: RestaurantOrderDto): string {
        if (order.orderType === OrderTypes.CarService || order.carNumber) {
            const car = [order.carName, order.carNumber].filter(Boolean).join(' · ');
            return car || order.tableNo || 'Car Service';
        }
        if (order.orderType === OrderTypes.Takeaway) {
            return order.tokenNo != null ? `Takeaway #${order.tokenNo}` : 'Takeaway';
        }
        return order.tableNo ? `Table ${order.tableNo}` : order.orderNo || 'Order';
    }

    itemCount(order: RestaurantOrderDto): number {
        return (order.lines || []).length;
    }

    elapsedLabel(order: RestaurantOrderDto): string {
        void this.nowMs;
        const start = this.getOrderStartMs(order);
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

    private getOrderStartMs(order: RestaurantOrderDto): number {
        if (!order) {
            return 0;
        }
        const candidates: number[] = [];
        const created = this.parseOrderDateMs(
            (order as any).creationTime ?? (order as any).CreationTime
        );
        if (created) {
            candidates.push(created);
        }
        for (const line of order.lines || []) {
            const sent = this.parseOrderDateMs(
                (line as any).sentToKitchenTime ?? (line as any).SentToKitchenTime
            );
            if (sent) {
                candidates.push(sent);
            }
        }
        return candidates.length ? Math.min(...candidates) : 0;
    }

    /**
     * API datetimes are often server wall-clock values serialized with a trailing Z,
     * which can look "in the future" in PKT and freeze the timer at 0s.
     */
    private parseOrderDateMs(value: any): number {
        if (value == null || value === '') {
            return 0;
        }
        if (value instanceof Date) {
            const t = value.getTime();
            return Number.isNaN(t) ? 0 : t;
        }
        const raw = String(value).trim();
        if (!raw) {
            return 0;
        }
        const asIs = Date.parse(raw);
        const localWall = Date.parse(raw.replace(/([zZ]|[+-]\d{2}:\d{2})$/, ''));
        const asUtc = /([zZ]|[+-]\d{2}:\d{2})$/.test(raw)
            ? asIs
            : Date.parse(raw.includes('T') ? `${raw}Z` : `${raw}T00:00:00Z`);

        const now = this.nowMs || Date.now();
        const options: number[] = [asIs, localWall, asUtc].filter(
            (t) => !!t && !Number.isNaN(t)
        );
        if (!Number.isNaN(asIs) && asIs > now + 2000) {
            for (let h = 1; h <= 5; h++) {
                options.push(asIs - h * 3600_000);
            }
        }
        if (!options.length) {
            return 0;
        }
        const notFuture = options.filter((t) => t <= now + 2000);
        if (notFuture.length) {
            return Math.max(...notFuture);
        }
        return Math.min(...options);
    }

    openInPos(order: RestaurantOrderDto): void {
        void this.router.navigate(['/pos'], {
            queryParams: { restaurantOrderId: order.id },
        });
    }

    goToPos(): void {
        void this.router.navigate(['/pos']);
    }

    trackByOrderId(_: number, order: RestaurantOrderDto): number {
        return order.id;
    }
}
