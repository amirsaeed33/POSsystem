import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { MessageService } from 'primeng/api';
import {
    CustomerOrderDto,
    CustomerOrderStatus,
} from 'src/app/demo/api/customer-order';
import { CustomerOrderService } from 'src/app/demo/service/customer-order.service';
import { OnlineCartService } from 'src/app/demo/service/online-cart.service';
import { SessionService } from 'src/app/demo/service/session.service';

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
export class OnlineOrderHistoryComponent implements OnInit {
    phone = '';
    branchId: number | null = null;
    orders: CustomerOrderDto[] = [];
    loading = false;
    searched = false;
    errorMessage = '';
    reorderingId: number | null = null;

    // Order detail popup
    detailVisible = false;
    detailOrder: CustomerOrderDto | null = null;
    detailLoading = false;
    customerRatingDraft: number | null = null;
    customerCommentDraft = '';
    ratingSubmitting = false;

    // Store chrome
    branchName = '';
    branchAddress = '';
    branchPhone = '';
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
        private sessionService: SessionService
    ) {}

    ngOnInit(): void {
        const qBranch = this.route.snapshot.queryParamMap.get('branchId');
        if (qBranch && !isNaN(Number(qBranch))) {
            this.branchId = Number(qBranch);
        }

        const qPhone = this.route.snapshot.queryParamMap.get('phone');
        const saved = this.readSavedPhone();
        this.phone = (qPhone || saved || '').trim();

        this.refreshAuth();
        if (this.branchId) {
            void this.loadStoreHeader(this.branchId);
        }

        if (this.phone.length >= 7) {
            void this.search();
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
            .filter((o) => o.status === CustomerOrderStatus.Delivered)
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

    openOrder(order: CustomerOrderDto): void {
        this.detailOrder = { ...order, lines: [...(order.lines || [])] };
        this.customerRatingDraft = order.customerRating ?? null;
        this.customerCommentDraft = order.customerFeedbackComment || '';
        this.detailVisible = true;
        void this.refreshDetailOrder(order.orderNo || String(order.id));
    }

    closeDetail(): void {
        this.detailVisible = false;
        this.detailOrder = null;
        this.customerRatingDraft = null;
        this.customerCommentDraft = '';
        this.ratingSubmitting = false;
    }

    private async refreshDetailOrder(orderNo: string): Promise<void> {
        if (!orderNo?.trim()) return;
        this.detailLoading = true;
        try {
            const live = await this.customerOrderService.trackOrder(orderNo.trim());
            if (live && this.detailVisible) {
                this.detailOrder = live;
                this.customerRatingDraft = live.customerRating ?? this.customerRatingDraft;
                if (live.customerFeedbackComment) {
                    this.customerCommentDraft = live.customerFeedbackComment;
                }
                // Keep list card in sync with latest status/total.
                const idx = this.orders.findIndex(
                    (o) =>
                        o.id === live.id ||
                        (o.orderNo && live.orderNo && o.orderNo === live.orderNo)
                );
                if (idx >= 0) {
                    this.orders[idx] = { ...this.orders[idx], ...live };
                }
            }
        } catch {
            /* keep snapshot from list */
        } finally {
            this.detailLoading = false;
        }
    }

    statusHint(order: CustomerOrderDto): string {
        if (order.status === CustomerOrderStatus.Rejected) {
            return 'This order was cancelled by the store.';
        }
        if (order.status === CustomerOrderStatus.Delivered) {
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
        const match = notes.match(/https?:\/\/maps\.google\.com[^\s|]+/);
        return match ? match[0] : null;
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
        return order?.status === CustomerOrderStatus.Delivered;
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
    } {
        const text = notes || '';
        const addrMatch = text.match(/📍\s*Addr:\s*([^|]+)/i);
        const landmarkMatch = text.match(/🏛️\s*Landmark:\s*([^|]+)/i);
        return {
            address: addrMatch?.[1]?.trim() || undefined,
            landmark: landmarkMatch?.[1]?.trim() || undefined,
        };
    }

    statusLabel(order: CustomerOrderDto): string {
        if (order.status === CustomerOrderStatus.Rejected) return 'Cancelled';
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
        if (order.status === CustomerOrderStatus.Delivered) return 'is-done';
        if (
            order.status === CustomerOrderStatus.Approved ||
            order.status === CustomerOrderStatus.InProgress
        ) {
            return 'is-progress';
        }
        return 'is-pending';
    }

    itemSummary(order: CustomerOrderDto): string {
        const lines = order.lines || [];
        if (!lines.length) return 'No items listed';
        const first = lines[0];
        const head = `${first.quantity}× ${first.productName || 'Item'}`;
        if (lines.length === 1) return head;
        return `${head}, +${lines.length - 1} more`;
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
            const info = await this.customerOrderService.getOnlineStoreHeader(branchId);
            if (!info) return;
            this.branchName = info.branchName || info.BranchName || '';
            this.branchAddress = info.address || info.Address || '';
            this.branchPhone = info.phone || info.Phone || '';
            this.facebookUrl = info.facebookUrl || info.FacebookUrl || '';
            this.instagramUrl = info.instagramUrl || info.InstagramUrl || '';
            this.youtubeUrl = info.youtubeUrl || info.YoutubeUrl || '';
            this.isRestaurant =
                !!(info.isRestaurant ?? info.IsRestaurant) ||
                info.businessType === 2 ||
                info.BusinessType === 2;
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
