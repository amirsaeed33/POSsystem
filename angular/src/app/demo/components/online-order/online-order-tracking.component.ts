import { Component, OnInit, OnDestroy } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { MessageService } from 'primeng/api';
import { CustomerOrderDto, CustomerOrderStatus } from 'src/app/demo/api/customer-order';
import { CustomerOrderService } from 'src/app/demo/service/customer-order.service';

@Component({
    selector: 'app-online-order-tracking',
    templateUrl: './online-order-tracking.component.html',
    styleUrls: ['./online-order-tracking.component.scss'],
    providers: [MessageService]
})
export class OnlineOrderTrackingComponent implements OnInit, OnDestroy {
    orderNoInput = '';
    currentOrderNo = '';
    order: CustomerOrderDto | null = null;
    loading = false;
    refreshing = false;
    errorMessage = '';
    lastUpdated: Date | null = null;
    copied = false;
    branchId: number | null = null;

    private pollTimer: any = null;
    private countdownInterval: any = null;
    autoRefreshSeconds = 8;
    countdown = 8;
    previousStatus: number | null = null;

    constructor(
        private route: ActivatedRoute,
        private router: Router,
        private customerOrderService: CustomerOrderService,
        private messageService: MessageService
    ) {}

    ngOnInit(): void {
        const qBranchId = this.route.snapshot.queryParamMap.get('branchId');
        if (qBranchId && !isNaN(Number(qBranchId))) {
            this.branchId = Number(qBranchId);
        }

        // Check route parameter: /online-order/track/:orderNo
        this.route.paramMap.subscribe(params => {
            const paramOrderNo = params.get('orderNo');
            if (paramOrderNo) {
                this.orderNoInput = paramOrderNo.trim();
                this.searchOrder(this.orderNoInput);
            } else {
                // Check query parameter: /online-order/track?orderNo=...
                this.route.queryParamMap.subscribe(qParams => {
                    const qOrderNo = qParams.get('orderNo');
                    if (qOrderNo) {
                        this.orderNoInput = qOrderNo.trim();
                        this.searchOrder(this.orderNoInput);
                    }
                });
            }
        });
    }

    ngOnDestroy(): void {
        this.stopPolling();
    }

    onSearchSubmit(): void {
        if (!this.orderNoInput || !this.orderNoInput.trim()) {
            this.errorMessage = 'Please enter an Order Number or Mobile Number.';
            return;
        }
        this.errorMessage = '';
        this.searchOrder(this.orderNoInput.trim());
    }

    async searchOrder(orderNo: string, silent = false): Promise<void> {
        if (!silent) {
            this.loading = true;
            this.errorMessage = '';
        } else {
            this.refreshing = true;
        }

        this.currentOrderNo = orderNo;

        try {
            const result = await this.customerOrderService.trackOrder(orderNo);
            if (!result) {
                this.errorMessage = `Order "${orderNo}" not found. Please double check the number and try again.`;
                this.order = null;
                this.stopPolling();
                return;
            }

            // Check if status changed to In Progress
            if (this.previousStatus !== null && this.previousStatus === CustomerOrderStatus.Pending && this.isInProgress(result)) {
                this.messageService.add({
                    severity: 'success',
                    summary: 'Order Accepted!',
                    detail: 'Great news! The store has accepted your order and it is now In Progress.',
                    life: 6000
                });
            }

            this.previousStatus = result.status;
            this.order = result;
            this.lastUpdated = new Date();
            this.errorMessage = '';

            // Update URL query param if not in paramMap
            if (!this.route.snapshot.paramMap.get('orderNo')) {
                this.router.navigate([], {
                    relativeTo: this.route,
                    queryParams: { orderNo: result.orderNo || orderNo },
                    queryParamsHandling: 'merge',
                    replaceUrl: true
                });
            }

            // Keep polling if order is still active (Pending or In Progress)
            if (this.isPending(result) || this.isInProgress(result)) {
                this.startPolling();
            } else {
                this.stopPolling();
            }
        } catch (err: any) {
            if (!silent) {
                this.order = null;
                this.errorMessage = err?.message || 'Could not find order. Please verify the order number.';
                this.stopPolling();
            }
        } finally {
            this.loading = false;
            this.refreshing = false;
        }
    }

    startPolling(): void {
        this.stopPolling();
        this.countdown = this.autoRefreshSeconds;

        this.countdownInterval = setInterval(() => {
            if (this.countdown > 1) {
                this.countdown--;
            } else {
                this.countdown = this.autoRefreshSeconds;
                if (this.currentOrderNo && !this.loading && !this.refreshing) {
                    this.searchOrder(this.currentOrderNo, true);
                }
            }
        }, 1000);
    }

    stopPolling(): void {
        if (this.countdownInterval) {
            clearInterval(this.countdownInterval);
            this.countdownInterval = null;
        }
        if (this.pollTimer) {
            clearTimeout(this.pollTimer);
            this.pollTimer = null;
        }
    }

    manualRefresh(): void {
        if (this.currentOrderNo) {
            this.searchOrder(this.currentOrderNo, false);
        }
    }

    isPending(order?: CustomerOrderDto | null): boolean {
        if (!order) return false;
        return order.status === CustomerOrderStatus.Pending;
    }

    isInProgress(order?: CustomerOrderDto | null): boolean {
        if (!order) return false;
        return order.status === CustomerOrderStatus.Approved
            || order.status === CustomerOrderStatus.InProgress
            || Boolean(order.statusName?.toLowerCase().includes('progress'));
    }

    isRejected(order?: CustomerOrderDto | null): boolean {
        if (!order) return false;
        return order.status === CustomerOrderStatus.Rejected;
    }

    copyOrderNo(val?: string): void {
        if (!val) return;
        navigator.clipboard.writeText(val).then(() => {
            this.copied = true;
            this.messageService.add({
                severity: 'info',
                summary: 'Copied',
                detail: `Order #${val} copied to clipboard!`,
                life: 2500
            });
            setTimeout(() => {
                this.copied = false;
            }, 2500);
        });
    }

    backToStore(): void {
        if (this.branchId || this.order?.branchId) {
            this.router.navigate(['/online-order'], {
                queryParams: { branchId: this.branchId || this.order?.branchId }
            });
        } else {
            this.router.navigate(['/online-order']);
        }
    }

    getGoogleMapsLink(notes?: string): string | null {
        if (!notes) return null;
        const match = notes.match(/https?:\/\/maps\.google\.com[^\s|]+/);
        return match ? match[0] : null;
    }
}
