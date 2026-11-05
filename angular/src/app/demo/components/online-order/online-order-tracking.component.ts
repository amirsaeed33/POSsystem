import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { MessageService } from 'primeng/api';
import { CustomerOrderDto, CustomerOrderStatus } from 'src/app/demo/api/customer-order';
import { CustomerOrderService } from 'src/app/demo/service/customer-order.service';
import {
    MyOnlineOrderRef,
    MyOnlineOrdersService,
} from 'src/app/demo/service/my-online-orders.service';

@Component({
    selector: 'app-online-order-tracking',
    templateUrl: './online-order-tracking.component.html',
    styleUrls: ['./online-order-tracking.component.scss'],
    providers: [MessageService],
})
export class OnlineOrderTrackingComponent implements OnInit, OnDestroy {
    myOrders: MyOnlineOrderRef[] = [];
    currentOrderNo = '';
    order: CustomerOrderDto | null = null;
    loading = false;
    refreshing = false;
    errorMessage = '';
    lastUpdated: Date | null = null;
    branchId: number | null = null;
    view: 'list' | 'detail' = 'list';

    private countdownInterval: any = null;
    autoRefreshSeconds = 8;
    countdown = 8;
    previousStatus: number | null = null;
    customerRatingDraft: number | null = null;
    customerCommentDraft = '';
    ratingSubmitting = false;

    constructor(
        private route: ActivatedRoute,
        private router: Router,
        private customerOrderService: CustomerOrderService,
        private myOnlineOrders: MyOnlineOrdersService,
        private messageService: MessageService
    ) {}

    ngOnInit(): void {
        const qBranchId = this.route.snapshot.queryParamMap.get('branchId');
        if (qBranchId && !isNaN(Number(qBranchId))) {
            this.branchId = Number(qBranchId);
        }

        this.reloadMyOrders();

        this.route.paramMap.subscribe((params) => {
            const paramOrderNo = params.get('orderNo');
            if (paramOrderNo) {
                void this.selectOrder(paramOrderNo.trim());
                return;
            }

            const qOrderNo = this.route.snapshot.queryParamMap.get('orderNo');
            if (qOrderNo) {
                void this.selectOrder(qOrderNo.trim());
                return;
            }

            // No order selected — send customer back to the menu (active orders live there).
            this.backToStore();
        });
    }

    ngOnDestroy(): void {
        this.stopPolling();
    }

    reloadMyOrders(): void {
        this.myOrders = this.myOnlineOrders.getForBranch(this.branchId);
    }

    async selectOrder(orderNo: string): Promise<void> {
        if (!orderNo?.trim()) {
            return;
        }
        this.errorMessage = '';
        this.view = 'detail';
        await this.loadOrder(orderNo.trim());
    }

    showMyOrders(): void {
        this.stopPolling();
        this.backToStore();
    }

    hasRiderPhone(order?: CustomerOrderDto | null): boolean {
        return !!(order?.riderPhone || '').trim();
    }

    riderCallHref(order?: CustomerOrderDto | null): string {
        const dial = this.normalizeDialNumber(order?.riderPhone || '');
        return dial ? `tel:${dial}` : '#';
    }

    riderWhatsAppHref(order?: CustomerOrderDto | null): string {
        const phone = this.toWhatsAppNumber(order?.riderPhone || '');
        if (!phone) {
            return '#';
        }
        const orderNo = order?.orderNo || order?.id || '';
        const text = encodeURIComponent(`Hi, regarding order ${orderNo}`);
        return `https://wa.me/${phone}?text=${text}`;
    }

    private normalizeDialNumber(phone: string): string {
        return (phone || '').replace(/[^\d+]/g, '');
    }

    private toWhatsAppNumber(phone: string): string {
        let digits = (phone || '').replace(/\D/g, '');
        if (!digits) {
            return '';
        }
        if (digits.startsWith('0') && digits.length >= 10) {
            digits = `92${digits.substring(1)}`;
        }
        return digits;
    }

    async loadOrder(orderNo: string, silent = false): Promise<void> {
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
                this.errorMessage =
                    'This order could not be found. It may have been removed.';
                this.order = null;
                this.stopPolling();
                return;
            }

            if (
                this.previousStatus !== null &&
                this.previousStatus === CustomerOrderStatus.Pending &&
                this.isInProgress(result)
            ) {
                this.messageService.add({
                    severity: 'success',
                    summary: 'Order Accepted!',
                    detail: 'The store accepted your order and is preparing it.',
                    life: 6000,
                });
            } else if (
                this.previousStatus !== null &&
                this.previousStatus !== CustomerOrderStatus.Delivered &&
                this.isDelivered(result)
            ) {
                this.messageService.add({
                    severity: 'success',
                    summary: 'Delivered!',
                    detail: 'Your order was delivered successfully. Enjoy!',
                    life: 8000,
                });
            }

            this.previousStatus = result.status;
            this.order = result;
            this.customerRatingDraft = result.customerRating ?? null;
            this.customerCommentDraft = result.customerFeedbackComment || this.customerCommentDraft || '';
            this.lastUpdated = new Date();
            this.errorMessage = '';
            this.view = 'detail';

            if (!this.route.snapshot.paramMap.get('orderNo')) {
                this.router.navigate([], {
                    relativeTo: this.route,
                    queryParams: {
                        orderNo: result.orderNo || orderNo,
                        ...(this.branchId ? { branchId: this.branchId } : {}),
                    },
                    queryParamsHandling: 'merge',
                    replaceUrl: true,
                });
            }

            if (this.isPending(result) || this.isInProgress(result)) {
                this.startPolling();
            } else {
                this.stopPolling();
            }
        } catch (err: any) {
            if (!silent) {
                this.order = null;
                this.errorMessage =
                    err?.message || 'Could not load this order right now.';
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
                    void this.loadOrder(this.currentOrderNo, true);
                }
            }
        }, 1000);
    }

    stopPolling(): void {
        if (this.countdownInterval) {
            clearInterval(this.countdownInterval);
            this.countdownInterval = null;
        }
    }

    manualRefresh(): void {
        if (this.currentOrderNo) {
            void this.loadOrder(this.currentOrderNo, false);
        }
    }

    isPending(order?: CustomerOrderDto | null): boolean {
        if (!order) return false;
        return order.status === CustomerOrderStatus.Pending;
    }

    isInProgress(order?: CustomerOrderDto | null): boolean {
        if (!order) return false;
        return (
            order.status === CustomerOrderStatus.Approved ||
            order.status === CustomerOrderStatus.InProgress ||
            Boolean(order.statusName?.toLowerCase().includes('progress'))
        );
    }

    isDelivered(order?: CustomerOrderDto | null): boolean {
        if (!order) return false;
        return (
            order.status === CustomerOrderStatus.Delivered ||
            order.status === CustomerOrderStatus.Completed ||
            Boolean(order.statusName?.toLowerCase().includes('deliver')) ||
            Boolean(order.statusName?.toLowerCase().includes('complete'))
        );
    }

    isRejected(order?: CustomerOrderDto | null): boolean {
        if (!order) return false;
        return order.status === CustomerOrderStatus.Rejected;
    }

    statusLabel(order: CustomerOrderDto): string {
        if (this.isRejected(order)) return 'Cancelled';
        if (this.isDelivered(order)) return 'Delivered';
        if (this.isInProgress(order)) return 'Preparing';
        return 'Received';
    }

    statusHint(order: CustomerOrderDto): string {
        if (this.isRejected(order)) return 'This order was cancelled by the store.';
        if (this.isDelivered(order)) return 'Enjoy your meal — thank you for ordering!';
        if (this.isInProgress(order)) return 'The kitchen is preparing your food now.';
        return 'Waiting for the store to confirm your order.';
    }

    backToStore(): void {
        if (this.branchId || this.order?.branchId) {
            this.router.navigate(['/online-order'], {
                queryParams: {
                    branchId: this.branchId || this.order?.branchId,
                },
            });
        } else {
            this.router.navigate(['/online-order']);
        }
    }

    canSaveCustomerRating(): boolean {
        if (!this.order || !this.customerRatingDraft) {
            return false;
        }
        const draftComment = (this.customerCommentDraft || '').trim();
        const savedComment = (this.order.customerFeedbackComment || '').trim();
        const ratingChanged = this.customerRatingDraft !== (this.order.customerRating ?? null);
        const commentChanged = draftComment !== savedComment;
        return ratingChanged || commentChanged;
    }

    async submitCustomerRating(): Promise<void> {
        if (!this.order?.orderNo || !this.customerRatingDraft) {
            return;
        }
        if (!this.canSaveCustomerRating()) {
            return;
        }

        const isUpdate = !!this.order.customerRating;
        this.ratingSubmitting = true;
        try {
            const updated = await this.customerOrderService.rateOrder({
                orderNo: this.order.orderNo,
                rating: this.customerRatingDraft,
                comment: this.customerCommentDraft,
            });
            this.order = updated;
            this.customerRatingDraft = updated.customerRating ?? this.customerRatingDraft;
            this.customerCommentDraft = updated.customerFeedbackComment || '';
            this.messageService.add({
                severity: 'success',
                summary: 'Thanks!',
                detail: isUpdate ? 'Your feedback was updated.' : 'Your feedback was saved.',
                life: 4000,
            });
        } catch (err: any) {
            this.messageService.add({
                severity: 'error',
                summary: 'Rating failed',
                detail: err?.message || 'Could not save your rating. Please try again.',
            });
        } finally {
            this.ratingSubmitting = false;
        }
    }

    getGoogleMapsLink(notes?: string): string | null {
        if (!notes) return null;
        const match = notes.match(/https?:\/\/maps\.google\.com[^\s|]+/);
        return match ? match[0] : null;
    }
}
