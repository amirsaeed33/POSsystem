import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { MessageService } from 'primeng/api';
import {
    CustomerOrderDto,
    CustomerOrderStatus,
} from 'src/app/demo/api/customer-order';
import { CustomerOrderService } from 'src/app/demo/service/customer-order.service';
import { OnlineCartService } from 'src/app/demo/service/online-cart.service';
import { SessionService } from 'src/app/demo/service/session.service';
import { AppBrowserTitleService } from 'src/app/demo/service/app-browser-title.service';
import { environment } from 'src/environments/environment';

const PHONE_STORAGE_KEY = 'smartpos_customer_phone';

@Component({
    selector: 'app-online-order-history',
    templateUrl: './online-order-history.component.html',
    styleUrls: [
        './online-order.component.scss',
        './online-order-history.component.scss',
    ],
    providers: [MessageService],
})
export class OnlineOrderHistoryComponent implements OnInit, OnDestroy {
    phone = '';
    branchId: number | null = null;
    orders: CustomerOrderDto[] = [];
    loading = false;
    searched = false;
    errorMessage = '';
    reorderingId: number | null = null;

    /** Ticks every second so active order cards show a live elapsed timer. */
    nowMs = Date.now();
    private clockTimer: ReturnType<typeof setInterval> | null = null;

    // Order detail popup (+ live tracking for active orders)
    detailVisible = false;
    detailOrder: CustomerOrderDto | null = null;
    detailLoading = false;
    customerRatingDraft: number | null = null;
    customerCommentDraft = '';
    ratingSubmitting = false;
    private detailPollTimer: ReturnType<typeof setInterval> | null = null;
    private detailCountdownTimer: ReturnType<typeof setInterval> | null = null;
    autoRefreshSeconds = 8;
    detailCountdown = 8;
    private previousDetailStatus: number | null = null;
    private pendingOpenOrderNo: string | null = null;

    // Store chrome
    branchName = '';
    branchAddress = '';
    branchPhone = '';
    branchImagePath = '';
    branchLogoBroken = false;
    isRestaurant = false;
    facebookUrl = '';
    instagramUrl = '';
    youtubeUrl = '';
    isLoggedIn = false;
    userName = 'Guest';
    userInitials = 'GU';

    constructor(
        private route: ActivatedRoute,
        private router: Router,
        private customerOrderService: CustomerOrderService,
        public cartService: OnlineCartService,
        private messageService: MessageService,
        private sessionService: SessionService,
        private browserTitle: AppBrowserTitleService
    ) {}

    ngOnInit(): void {
        const qBranch = this.route.snapshot.queryParamMap.get('branchId');
        if (qBranch && !isNaN(Number(qBranch))) {
            this.branchId = Number(qBranch);
        }

        const qPhone = this.route.snapshot.queryParamMap.get('phone');
        const saved = this.readSavedPhone();
        this.phone = (qPhone || saved || '').trim();

        const qOrderNo = (this.route.snapshot.queryParamMap.get('orderNo') || '').trim();
        this.pendingOpenOrderNo = qOrderNo || null;

        this.refreshAuth();
        if (this.branchId) {
            void this.loadStoreHeader(this.branchId);
        }

        this.clockTimer = setInterval(() => {
            this.nowMs = Date.now();
        }, 1000);

        void (async () => {
            if (this.phone.length >= 7) {
                await this.search();
            }
            if (this.pendingOpenOrderNo) {
                await this.openOrderByNumber(this.pendingOpenOrderNo);
                this.pendingOpenOrderNo = null;
            }
        })();
    }

    ngOnDestroy(): void {
        if (this.clockTimer) {
            clearInterval(this.clockTimer);
            this.clockTimer = null;
        }
        this.stopDetailPolling();
        this.browserTitle.clearStorefrontBranchName();
    }

    get branchLogoUrl(): string {
        if (!this.branchImagePath || this.branchLogoBroken) {
            return '';
        }
        const path = this.branchImagePath;
        if (path.startsWith('http://') || path.startsWith('https://') || path.startsWith('data:')) {
            return path;
        }
        return `${environment.apiUrl}${path.startsWith('/') ? '' : '/'}${path}`;
    }

    get headerBrandLogoUrl(): string {
        return this.branchLogoUrl || AppBrowserTitleService.AJ_FAVICON_PNG;
    }

    onBranchLogoError(): void {
        if (!this.branchLogoBroken && this.branchImagePath) {
            this.branchLogoBroken = true;
            this.browserTitle.setStorefrontBranchLogoUrl(null);
        }
    }

    get socialFacebookHref(): string {
        return this.facebookUrl || 'https://www.facebook.com/';
    }

    get socialInstagramHref(): string {
        return this.instagramUrl || 'https://www.instagram.com/';
    }

    get socialYoutubeHref(): string {
        return this.youtubeUrl || 'https://www.youtube.com/';
    }

    goStore(section?: string): void {
        this.router.navigate(['/online-order'], {
            queryParams: this.branchId ? { branchId: this.branchId } : {},
            fragment: section || undefined,
        });
    }

    openProfile(): void {
        this.router.navigate(['/online-order'], {
            queryParams: {
                ...(this.branchId ? { branchId: this.branchId } : {}),
                openProfile: 1,
            },
        });
    }

    get historyProfilePhotoUrl(): string {
        const path = (localStorage.getItem('smartpos_customer_image') || '').trim();
        if (!path) {
            return '';
        }
        if (/^https?:\/\//i.test(path) || path.startsWith('data:')) {
            return path;
        }
        // Relative path served from API host — history uses same-origin proxy in local.
        return path.startsWith('/') ? path : `/${path}`;
    }

    get historyProfileInitials(): string {
        const name = (localStorage.getItem('smartpos_customer_name') || '').trim();
        if (!name) {
            return 'ME';
        }
        const parts = name.split(/\s+/).filter(Boolean);
        if (parts.length >= 2) {
            return (parts[0].charAt(0) + parts[1].charAt(0)).toUpperCase();
        }
        return name.substring(0, 2).toUpperCase();
    }

    openCart(): void {
        this.cartService.sidebarVisible = true;
        this.goStore();
    }

    /** Delivered order totals only (actual spend). */
    get totalMoneySpent(): number {
        return this.orders
            .filter(
                (o) =>
                    o.status === CustomerOrderStatus.Completed ||
                    o.status === CustomerOrderStatus.Delivered
            )
            .reduce((sum, o) => sum + (Number(o.totalAmount) || 0), 0);
    }

    get totalOrdersCount(): number {
        return this.orders.length;
    }

    /** Not finished yet: pending / approved / out for delivery. */
    get pendingOrdersCount(): number {
        return this.orders.filter(
            (o) =>
                o.status === CustomerOrderStatus.Pending ||
                o.status === CustomerOrderStatus.Approved ||
                o.status === CustomerOrderStatus.InProgress
        ).length;
    }

    async search(): Promise<void> {
        const phone = (this.phone || '').trim();
        if (phone.replace(/\D/g, '').length < 7) {
            this.errorMessage = 'Enter a valid phone number (at least 7 digits).';
            this.searched = false;
            this.orders = [];
            return;
        }

        this.loading = true;
        this.errorMessage = '';
        this.searched = true;

        try {
            this.orders = await this.customerOrderService.getOrdersByPhone({
                phone,
                branchId: this.branchId,
                maxResultCount: 30,
            });
            this.savePhone(phone);
        } catch (err: any) {
            this.orders = [];
            this.errorMessage =
                err?.message || 'Could not load your order history right now.';
            this.messageService.add({
                severity: 'error',
                summary: 'Lookup failed',
                detail: this.errorMessage,
            });
        } finally {
            this.loading = false;
        }
    }

    /** Stagger delay for order-card entrance animation. */
    cardStagger(index: number): string {
        const i = Math.max(0, Math.min(Number(index) || 0, 12));
        return `${i * 55}ms`;
    }

    openOrder(order: CustomerOrderDto): void {
        this.previousDetailStatus = order.status ?? null;
        this.detailOrder = { ...order, lines: [...(order.lines || [])] };
        this.customerRatingDraft = order.customerRating ?? null;
        this.customerCommentDraft = order.customerFeedbackComment || '';
        this.detailVisible = true;
        this.syncOrderNoInUrl(order.orderNo || null);
        void this.refreshDetailOrder(order.orderNo || String(order.id));
    }

    /** Open tracking detail from order number (My Orders / old /track links). */
    async openOrderByNumber(orderNo: string): Promise<void> {
        const key = (orderNo || '').trim();
        if (!key) {
            return;
        }

        const listed = this.orders.find(
            (o) =>
                (o.orderNo && o.orderNo.toLowerCase() === key.toLowerCase()) ||
                String(o.id) === key
        );
        if (listed) {
            this.openOrder(listed);
            return;
        }

        this.detailLoading = true;
        this.detailVisible = true;
        this.detailOrder = null;
        this.syncOrderNoInUrl(key);
        try {
            const live = await this.customerOrderService.trackOrder(key);
            if (!live) {
                this.detailVisible = false;
                this.messageService.add({
                    severity: 'warn',
                    summary: 'Order not found',
                    detail: 'This order could not be found. It may have been removed.',
                });
                this.syncOrderNoInUrl(null);
                return;
            }
            this.previousDetailStatus = live.status ?? null;
            this.detailOrder = live;
            this.customerRatingDraft = live.customerRating ?? null;
            this.customerCommentDraft = live.customerFeedbackComment || '';
            this.mergeOrderIntoList(live);
            this.syncDetailPolling(live);
        } catch (err: any) {
            this.detailVisible = false;
            this.messageService.add({
                severity: 'error',
                summary: 'Lookup failed',
                detail: err?.message || 'Could not load this order right now.',
            });
            this.syncOrderNoInUrl(null);
        } finally {
            this.detailLoading = false;
        }
    }

    closeDetail(): void {
        this.stopDetailPolling();
        this.detailVisible = false;
        this.detailOrder = null;
        this.customerRatingDraft = null;
        this.customerCommentDraft = '';
        this.ratingSubmitting = false;
        this.previousDetailStatus = null;
        this.syncOrderNoInUrl(null);
    }

    private syncOrderNoInUrl(orderNo: string | null): void {
        this.router.navigate([], {
            relativeTo: this.route,
            queryParams: { orderNo: orderNo || null },
            queryParamsHandling: 'merge',
            replaceUrl: true,
        });
    }

    private mergeOrderIntoList(live: CustomerOrderDto): void {
        const idx = this.orders.findIndex(
            (o) =>
                o.id === live.id ||
                (o.orderNo && live.orderNo && o.orderNo === live.orderNo)
        );
        if (idx >= 0) {
            this.orders[idx] = { ...this.orders[idx], ...live };
        } else {
            this.orders = [live, ...this.orders];
            this.searched = true;
        }
    }

    private isActiveTrackingStatus(order: CustomerOrderDto | null | undefined): boolean {
        if (!order) {
            return false;
        }
        return (
            order.status === CustomerOrderStatus.Pending ||
            order.status === CustomerOrderStatus.Approved ||
            order.status === CustomerOrderStatus.InProgress
        );
    }

    private syncDetailPolling(order: CustomerOrderDto | null): void {
        this.stopDetailPolling();
        if (!order?.orderNo || !this.isActiveTrackingStatus(order)) {
            return;
        }
        this.detailCountdown = this.autoRefreshSeconds;
        this.detailCountdownTimer = setInterval(() => {
            if (this.detailCountdown > 1) {
                this.detailCountdown--;
            } else {
                this.detailCountdown = this.autoRefreshSeconds;
            }
        }, 1000);
        this.detailPollTimer = setInterval(() => {
            if (this.detailVisible && this.detailOrder?.orderNo && !this.detailLoading) {
                void this.refreshDetailOrder(this.detailOrder.orderNo, true);
            }
        }, this.autoRefreshSeconds * 1000);
    }

    private stopDetailPolling(): void {
        if (this.detailPollTimer) {
            clearInterval(this.detailPollTimer);
            this.detailPollTimer = null;
        }
        if (this.detailCountdownTimer) {
            clearInterval(this.detailCountdownTimer);
            this.detailCountdownTimer = null;
        }
    }

    private async refreshDetailOrder(orderNo: string, silent = false): Promise<void> {
        if (!orderNo?.trim()) return;
        if (!silent) {
            this.detailLoading = true;
        }
        try {
            const live = await this.customerOrderService.trackOrder(orderNo.trim());
            if (live && this.detailVisible) {
                if (
                    this.previousDetailStatus !== null &&
                    this.previousDetailStatus === CustomerOrderStatus.Pending &&
                    (live.status === CustomerOrderStatus.Approved ||
                        live.status === CustomerOrderStatus.InProgress)
                ) {
                    this.messageService.add({
                        severity: 'success',
                        summary: 'Order Accepted!',
                        detail: 'The store accepted your order and is preparing it.',
                        life: 6000,
                    });
                } else if (
                    this.previousDetailStatus !== null &&
                    this.previousDetailStatus !== CustomerOrderStatus.Delivered &&
                    this.previousDetailStatus !== CustomerOrderStatus.Completed &&
                    (live.status === CustomerOrderStatus.Delivered ||
                        live.status === CustomerOrderStatus.Completed)
                ) {
                    this.messageService.add({
                        severity: 'success',
                        summary: 'Delivered!',
                        detail: 'Your order was delivered successfully. Enjoy!',
                        life: 8000,
                    });
                }
                this.previousDetailStatus = live.status ?? this.previousDetailStatus;
                this.detailOrder = live;
                this.customerRatingDraft = live.customerRating ?? this.customerRatingDraft;
                if (live.customerFeedbackComment) {
                    this.customerCommentDraft = live.customerFeedbackComment;
                }
                this.mergeOrderIntoList(live);
                this.syncDetailPolling(live);
            }
        } catch {
            /* keep snapshot from list */
        } finally {
            if (!silent) {
                this.detailLoading = false;
            }
        }
    }

    isDetailPending(order?: CustomerOrderDto | null): boolean {
        return !!order && order.status === CustomerOrderStatus.Pending;
    }

    isDetailInProgress(order?: CustomerOrderDto | null): boolean {
        if (!order) return false;
        return (
            order.status === CustomerOrderStatus.Approved ||
            order.status === CustomerOrderStatus.InProgress
        );
    }

    isDetailDelivered(order?: CustomerOrderDto | null): boolean {
        if (!order) return false;
        return (
            order.status === CustomerOrderStatus.Delivered ||
            order.status === CustomerOrderStatus.Completed
        );
    }

    isDetailRejected(order?: CustomerOrderDto | null): boolean {
        return !!order && order.status === CustomerOrderStatus.Rejected;
    }

    statusHint(order: CustomerOrderDto): string {
        if (order.status === CustomerOrderStatus.Rejected) {
            return 'This order was cancelled by the store.';
        }
        if (
            order.status === CustomerOrderStatus.Delivered ||
            order.status === CustomerOrderStatus.Completed
        ) {
            return 'Enjoy your meal — thank you for ordering!';
        }
        if (
            order.status === CustomerOrderStatus.Approved ||
            order.status === CustomerOrderStatus.InProgress
        ) {
            return 'The kitchen is preparing your food now.';
        }
        return 'Waiting for the store to confirm your order.';
    }

    getGoogleMapsLink(notes?: string): string | null {
        if (!notes) return null;
        const match =
            notes.match(/https?:\/\/(?:www\.)?google\.com\/maps[^\s|]+/i) ||
            notes.match(/https?:\/\/maps\.google\.com[^\s|]+/i);
        return match ? match[0].replace(/[)\],.]+$/, '') : null;
    }

    private stripMapsAndCoords(value?: string): string | undefined {
        if (!value) return undefined;
        const cleaned = value
            .replace(/https?:\/\/(?:www\.)?google\.com\/maps[^\s|]*/gi, '')
            .replace(/https?:\/\/maps\.google\.com[^\s|]*/gi, '')
            .replace(/(?:www\.)?google\.com\/maps[^\s|]*/gi, '')
            .replace(/maps\.google\.com[^\s|]*/gi, '')
            .replace(/Maps:\s*[^\s|]+/gi, '')
            .replace(/-?\d{1,3}\.\d{4,}\s*,\s*-?\d{1,3}\.\d{4,}/g, '')
            .replace(/\s+/g, ' ')
            .trim();
        return cleaned || undefined;
    }

    /** Clean delivery fields for the order detail popup (no raw pipe blob). */
    deliveryDisplay(order: CustomerOrderDto | null | undefined): {
        address?: string;
        landmark?: string;
        specialNote?: string;
        riderNote?: string;
        mapsUrl?: string | null;
    } {
        if (!order) {
            return {};
        }
        const parsed = this.parseDeliveryFromNotes(order.notes);
        const address =
            this.stripMapsAndCoords(parsed.address) ||
            this.stripMapsAndCoords((order.customerAddress || '').trim()) ||
            undefined;
        return {
            address,
            landmark: this.stripMapsAndCoords(parsed.landmark),
            specialNote: this.stripMapsAndCoords(parsed.specialNote),
            riderNote: this.stripMapsAndCoords(parsed.riderNote),
            mapsUrl: this.getGoogleMapsLink(order.notes),
        };
    }

    canSaveCustomerRating(order: CustomerOrderDto): boolean {
        if (!this.customerRatingDraft) {
            return false;
        }
        const draftComment = (this.customerCommentDraft || '').trim();
        const savedComment = (order.customerFeedbackComment || '').trim();
        const ratingChanged = this.customerRatingDraft !== (order.customerRating ?? null);
        const commentChanged = draftComment !== savedComment;
        return ratingChanged || commentChanged;
    }

    async submitCustomerRating(): Promise<void> {
        if (!this.detailOrder?.orderNo || !this.customerRatingDraft) return;
        if (!this.canSaveCustomerRating(this.detailOrder)) return;

        const isUpdate = !!this.detailOrder.customerRating;
        this.ratingSubmitting = true;
        try {
            const updated = await this.customerOrderService.rateOrder({
                orderNo: this.detailOrder.orderNo,
                rating: this.customerRatingDraft,
                comment: this.customerCommentDraft,
            });
            this.detailOrder = updated;
            this.customerRatingDraft = updated.customerRating ?? this.customerRatingDraft;
            this.customerCommentDraft = updated.customerFeedbackComment || '';
            const idx = this.orders.findIndex((o) => o.id === updated.id);
            if (idx >= 0) {
                this.orders[idx] = { ...this.orders[idx], ...updated };
            }
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

    isDelivered(order: CustomerOrderDto | null | undefined): boolean {
        return (
            order?.status === CustomerOrderStatus.Delivered ||
            order?.status === CustomerOrderStatus.Completed
        );
    }

    canReorder(order: CustomerOrderDto): boolean {
        return (
            this.isDelivered(order) &&
            (order.lines || []).some((l) => l.productId > 0 && l.quantity > 0)
        );
    }

    reorder(order: CustomerOrderDto, event?: Event): void {
        event?.stopPropagation();
        event?.preventDefault();

        if (!this.canReorder(order) || this.reorderingId) {
            return;
        }

        const lines = (order.lines || []).filter(
            (l) => l.productId > 0 && (l.quantity || 0) > 0
        );
        if (!lines.length) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Cannot reorder',
                detail: 'This order has no items to add again.',
            });
            return;
        }

        this.reorderingId = order.id;
        try {
            this.cartService.clearCart();
            for (const line of lines) {
                this.cartService.addToCart(
                    {
                        id: line.productId,
                        name: line.productName || `Product #${line.productId}`,
                        price: line.unitPrice || 0,
                        unitName: 'Pcs',
                    },
                    Math.max(1, Math.round(Number(line.quantity) || 1)),
                    { unitPrice: line.unitPrice || 0 }
                );
            }

            const fromNotes = this.parseDeliveryFromNotes(order.notes);
            const phone =
                (order.customerPhone || this.phone || this.readSavedPhone() || '').trim();
            const payment =
                order.paymentMethod === 'OnlinePayment' ? 'OnlinePayment' : 'CashOnDelivery';

            this.cartService.setCheckoutDraft({
                customerName: (order.customerName || '').trim() || undefined,
                customerMobile: phone || undefined,
                deliveryAddress:
                    fromNotes.address ||
                    (order.customerAddress || '').trim() ||
                    undefined,
                deliveryLandmark: fromNotes.landmark || undefined,
                paymentMethod: payment,
                deliveryAreaId: order.deliveryAreaId ?? null,
            });

            const targetBranch = this.branchId || order.branchId || null;
            this.messageService.add({
                severity: 'success',
                summary: 'Added to cart',
                detail: `${lines.length} item(s) from #${order.orderNo || order.id} are ready to checkout.`,
                life: 3500,
            });

            this.cartService.sidebarVisible = true;
            this.router.navigate(['/online-order'], {
                queryParams: targetBranch ? { branchId: targetBranch } : {},
            });
        } finally {
            this.reorderingId = null;
        }
    }

    private parseDeliveryFromNotes(notes?: string): {
        address?: string;
        landmark?: string;
        specialNote?: string;
        riderNote?: string;
    } {
        const text = (notes || '').trim();
        if (!text) {
            return {};
        }

        const clean = (value?: string | null) => {
            const v = (value || '')
                .replace(/^[📍🏛️🗺️📝?\s]+/u, '')
                .replace(/\s+/g, ' ')
                .trim();
            return v || undefined;
        };

        const addrMatch = text.match(/(?:📍\s*)?Addr:\s*([^|]+)/i);
        const landmarkMatch = text.match(/(?:🏛️\s*)?Landmark:\s*([^|]+)/i);
        const notesMatch = text.match(/(?:📝\s*)?Notes:\s*([^|]+)/i);
        const riderMatch =
            text.match(/\[Rider[^\]]*delivery note:\s*([^\]]+)\]/i) ||
            text.match(/Rider\s+[^\]]+delivery note:\s*([^\]]+)/i);

        let address = clean(addrMatch?.[1]);
        let landmark = clean(landmarkMatch?.[1]);
        let specialNote = clean(notesMatch?.[1]);
        const riderNote = clean(riderMatch?.[1]);

        // Strip structured / duplicate noise from any leftover free text.
        if (!address || !landmark || !specialNote) {
            const leftovers = text
                .replace(/https?:\/\/(?:www\.)?google\.com\/maps[^\s|]*/gi, '')
                .replace(/https?:\/\/maps\.google\.com[^\s|]*/gi, '')
                .replace(/(?:www\.)?google\.com\/maps[^\s|]*/gi, '')
                .replace(/maps\.google\.com[^\s|]*/gi, '')
                .replace(/(?:📍\s*)?Addr:\s*[^|]+/gi, '')
                .replace(/(?:🏛️\s*)?Landmark:\s*[^|]+/gi, '')
                .replace(/(?:🗺️\s*)?Maps:\s*[^|]+/gi, '')
                .replace(/(?:📝\s*)?Notes:\s*[^|]+/gi, '')
                .replace(/\[Rider[^\]]*\]/gi, '')
                .replace(/(?:💳\s*)?Payment:\s*[^|]+/gi, '')
                .replace(/-?\d{1,3}\.\d{4,}\s*,\s*-?\d{1,3}\.\d{4,}/g, '')
                .replace(/\|\s*/g, ' ')
                .replace(/[?？]+/g, ' ')
                .replace(/\s+/g, ' ')
                .trim();

            if (leftovers) {
                // Avoid repeating the same address/landmark already captured.
                const lower = leftovers.toLowerCase();
                if (
                    !address &&
                    leftovers.length > 8 &&
                    !lower.startsWith('payment')
                ) {
                    address = leftovers;
                } else if (
                    !specialNote &&
                    (!address || lower !== address.toLowerCase()) &&
                    (!landmark || lower !== landmark.toLowerCase())
                ) {
                    specialNote = leftovers;
                }
            }
        }

        // Drop special note if it only duplicates address/landmark/payment.
        if (specialNote) {
            const s = specialNote.toLowerCase();
            if (
                (address && s === address.toLowerCase()) ||
                (landmark && s === landmark.toLowerCase()) ||
                s.startsWith('payment:')
            ) {
                specialNote = undefined;
            }
        }

        return { address, landmark, specialNote, riderNote };
    }

    statusLabel(order: CustomerOrderDto): string {
        if (order.status === CustomerOrderStatus.Rejected) return 'Cancelled';
        if (order.status === CustomerOrderStatus.Completed) return 'Completed';
        if (order.status === CustomerOrderStatus.Delivered) return 'Delivered';
        if (
            order.status === CustomerOrderStatus.Approved ||
            order.status === CustomerOrderStatus.InProgress
        ) {
            return 'In progress';
        }
        return order.statusName || 'Pending';
    }

    statusClass(order: CustomerOrderDto): string {
        if (order.status === CustomerOrderStatus.Rejected) return 'is-rejected';
        if (
            order.status === CustomerOrderStatus.Delivered ||
            order.status === CustomerOrderStatus.Completed
        ) {
            return 'is-done';
        }
        if (
            order.status === CustomerOrderStatus.Approved ||
            order.status === CustomerOrderStatus.InProgress
        ) {
            return 'is-progress';
        }
        return 'is-pending';
    }

    /** Live elapsed time for open orders; empty for delivered/completed/cancelled. */
    showRunningTimer(order: CustomerOrderDto): boolean {
        return (
            order.status !== CustomerOrderStatus.Delivered &&
            order.status !== CustomerOrderStatus.Completed &&
            order.status !== CustomerOrderStatus.Rejected
        );
    }

    elapsedLabel(order: CustomerOrderDto): string {
        const start = this.parseOrderDate(order);
        if (!start) {
            return '';
        }
        // Touch nowMs so Angular refreshes the label each second.
        const totalSec = Math.max(0, Math.floor((this.nowMs - start.getTime()) / 1000));
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

    isElapsedLate(order: CustomerOrderDto): boolean {
        const start = this.parseOrderDate(order);
        if (!start) {
            return false;
        }
        return this.nowMs - start.getTime() > 45 * 60 * 1000;
    }

    itemSummary(order: CustomerOrderDto): string {
        const lines = order.lines || [];
        if (!lines.length) return 'No items listed';
        const first = lines[0];
        const head = `${first.quantity}× ${first.productName || 'Item'}`;
        if (lines.length === 1) return head;
        return `${head}, +${lines.length - 1} more`;
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
        // Local PK mobiles like 03xxxxxxxxx → 923xxxxxxxxx
        if (digits.startsWith('0') && digits.length >= 10) {
            digits = `92${digits.substring(1)}`;
        }
        return digits;
    }

    /** Friendly label: Today / Yesterday / weekday name. */
    orderDayLabel(order: CustomerOrderDto): string {
        const orderDate = this.parseOrderDate(order);
        if (!orderDate) return '';

        const dayDiff = this.dayDiffFromToday(orderDate);
        if (dayDiff === 0) return 'Today';
        if (dayDiff === 1) return 'Yesterday';
        return orderDate.toLocaleDateString(undefined, { weekday: 'long' });
    }

    orderWeekdayShort(order: CustomerOrderDto): string {
        const orderDate = this.parseOrderDate(order);
        if (!orderDate) return '';
        return orderDate.toLocaleDateString(undefined, { weekday: 'short' }).toUpperCase();
    }

    orderDayNumber(order: CustomerOrderDto): string {
        const orderDate = this.parseOrderDate(order);
        if (!orderDate) return '';
        return String(orderDate.getDate()).padStart(2, '0');
    }

    /** Color tone for the calendar badge. */
    orderDayTone(order: CustomerOrderDto): string {
        const orderDate = this.parseOrderDate(order);
        if (!orderDate) return 'is-weekday';
        const dayDiff = this.dayDiffFromToday(orderDate);
        if (dayDiff === 0) return 'is-today';
        if (dayDiff === 1) return 'is-yesterday';
        return 'is-weekday';
    }

    private parseOrderDate(order: CustomerOrderDto): Date | null {
        if (!order?.orderDate) return null;
        const orderDate = new Date(order.orderDate);
        return isNaN(orderDate.getTime()) ? null : orderDate;
    }

    private dayDiffFromToday(orderDate: Date): number {
        const startOfDay = (d: Date) =>
            new Date(d.getFullYear(), d.getMonth(), d.getDate()).getTime();
        return Math.round((startOfDay(new Date()) - startOfDay(orderDate)) / 86400000);
    }

    private async loadStoreHeader(branchId: number): Promise<void> {
        try {
            this.customerOrderService.clearHeaderCache(branchId);
            const info = await this.customerOrderService.getOnlineStoreHeader(branchId);
            if (!info) return;
            this.branchName = info.branchName || info.BranchName || '';
            this.branchAddress = info.address || info.Address || '';
            this.branchPhone = info.phone || info.Phone || '';
            this.branchImagePath = (info.imagePath || info.ImagePath || '').trim();
            this.branchLogoBroken = false;
            this.facebookUrl = info.facebookUrl || info.FacebookUrl || '';
            this.instagramUrl = info.instagramUrl || info.InstagramUrl || '';
            this.youtubeUrl = info.youtubeUrl || info.YoutubeUrl || '';
            this.isRestaurant =
                !!(info.isRestaurant ?? info.IsRestaurant) ||
                info.businessType === 2 ||
                info.BusinessType === 2;
            this.browserTitle.setStorefrontBranchName(this.branchName);
            this.browserTitle.setStorefrontBranchLogoUrl(this.branchLogoUrl || null);
        } catch {
            /* keep defaults */
        }
    }

    private refreshAuth(): void {
        this.sessionService
            .getCurrentLoginInformations()
            .then((res) => {
                if (res?.user) {
                    this.isLoggedIn = true;
                    this.userName = res.user.name || res.user.userName || 'Customer';
                    const parts = this.userName.trim().split(' ');
                    this.userInitials =
                        parts.length > 1
                            ? (parts[0][0] + parts[1][0]).toUpperCase()
                            : this.userName.substring(0, 2).toUpperCase();
                } else {
                    this.isLoggedIn = false;
                }
            })
            .catch(() => {
                this.isLoggedIn = false;
            });
    }

    private readSavedPhone(): string {
        try {
            return localStorage.getItem(PHONE_STORAGE_KEY) || '';
        } catch {
            return '';
        }
    }

    private savePhone(phone: string): void {
        try {
            localStorage.setItem(PHONE_STORAGE_KEY, phone);
        } catch {
            /* ignore */
        }
    }
}
