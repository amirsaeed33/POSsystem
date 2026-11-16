import { Component, OnDestroy, OnInit } from '@angular/core';
import { MessageService, ConfirmationService } from 'primeng/api';
import { Subscription } from 'rxjs';
import {
    RiderDashboardStatsDto,
    RiderOrderDto,
    RiderOrderStatus,
} from 'src/app/demo/api/rider';
import { RiderService } from 'src/app/demo/service/rider.service';
import { BranchContextService } from 'src/app/demo/service/branch-context.service';
import { BranchService } from 'src/app/demo/service/branch.service';
import { BranchDto } from 'src/app/demo/api/branch';

@Component({
    selector: 'app-rider-dashboard',
    templateUrl: './rider-dashboard.component.html',
    styleUrls: ['./rider-dashboard.component.scss'],
    providers: [MessageService, ConfirmationService],
})
export class RiderDashboardComponent implements OnInit, OnDestroy {
    stats: RiderDashboardStatsDto | null = null;
    orders: RiderOrderDto[] = [];
    loading = false;
    actionInProgress: { [key: number]: boolean } = {};
    currentBranch: BranchDto | null = null;
    branchLogoFailed = false;

    // Reject Dialog State
    rejectDialogVisible = false;
    selectedOrderForReject: RiderOrderDto | null = null;
    rejectReason = '';
    rejectPresets = [
        'Bike Breakdown / Flat Tyre',
        'Customer Address Too Far / Out of Reach',
        'Emergency / Unable to Deliver',
        'Order Package Damaged / Issue',
        'Other Reason',
    ];

    // Complete Delivery Dialog State
    completeDialogVisible = false;
    selectedOrderForComplete: RiderOrderDto | null = null;
    deliveryNotes = '';
    deliveryRating: number | null = null;

    // Auto-refresh interval
    private pollInterval: any;
    private clockTimer: ReturnType<typeof setInterval> | null = null;
    private branchSub?: Subscription;
    autoRefreshEnabled = true;
    nowMs = Date.now();

    readonly OrderStatus = RiderOrderStatus;

    constructor(
        private riderService: RiderService,
        private branchContext: BranchContextService,
        private branchService: BranchService,
        private messageService: MessageService,
        private confirmationService: ConfirmationService
    ) {}

    get branchDisplayName(): string {
        return this.currentBranch?.name || this.stats?.branchName || 'Delivery Portal';
    }

    get branchLogoUrl(): string | null {
        if (this.branchLogoFailed || !this.currentBranch?.imagePath) {
            return null;
        }
        return this.branchService.getImageUrl(this.currentBranch.imagePath) || null;
    }

    ngOnInit(): void {
        this.currentBranch = this.branchContext.getCurrentBranch();
        this.branchSub = this.branchContext.currentBranch$.subscribe((branch) => {
            this.currentBranch = branch;
            this.branchLogoFailed = false;
        });
        this.loadData();
        this.pollInterval = setInterval(() => {
            if (this.autoRefreshEnabled && !this.loading) {
                this.loadData(true);
            }
        }, 25000);
        this.clockTimer = setInterval(() => {
            this.nowMs = Date.now();
        }, 1000);
    }

    ngOnDestroy(): void {
        this.branchSub?.unsubscribe();
        if (this.pollInterval) {
            clearInterval(this.pollInterval);
        }
        if (this.clockTimer) {
            clearInterval(this.clockTimer);
            this.clockTimer = null;
        }
    }

    onBranchLogoError(): void {
        this.branchLogoFailed = true;
    }

    async loadData(silent = false): Promise<void> {
        if (!silent) {
            this.loading = true;
        }

        try {
            const [statsRes, ordersRes] = await Promise.all([
                this.riderService.getDashboardStats(),
                this.riderService.getAssignedOrders(),
            ]);
            this.stats = statsRes;
            this.orders = ordersRes;
        } catch (error: any) {
            if (!silent) {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Failed to load',
                    detail:
                        error?.error?.error?.message ||
                        'Unable to load assigned orders.',
                });
            }
        } finally {
            this.loading = false;
        }
    }

    async acceptOrder(order: RiderOrderDto): Promise<void> {
        this.actionInProgress[order.id] = true;
        try {
            await this.riderService.acceptOrder(order.id);
            this.messageService.add({
                severity: 'success',
                summary: 'Order Accepted',
                detail: `Order ${order.orderNo} accepted. Ready for delivery!`,
            });
            await this.loadData(true);
        } catch (error: any) {
            this.messageService.add({
                severity: 'error',
                summary: 'Action Failed',
                detail:
                    error?.error?.error?.message ||
                    'Could not accept this order.',
            });
        } finally {
            this.actionInProgress[order.id] = false;
        }
    }

    openRejectDialog(order: RiderOrderDto): void {
        this.selectedOrderForReject = order;
        this.rejectReason = '';
        this.rejectDialogVisible = true;
    }

    selectPresetReason(preset: string): void {
        this.rejectReason = preset;
    }

    async confirmReject(): Promise<void> {
        if (!this.selectedOrderForReject) return;

        const orderId = this.selectedOrderForReject.id;
        const reason = (this.rejectReason || '').trim() || 'Declined by rider';

        this.actionInProgress[orderId] = true;
        this.rejectDialogVisible = false;

        try {
            await this.riderService.rejectOrder({ orderId, reason });
            this.messageService.add({
                severity: 'warn',
                summary: 'Assignment Declined',
                detail: `Order assignment has been declined and returned to admin.`,
            });
            await this.loadData(true);
        } catch (error: any) {
            this.messageService.add({
                severity: 'error',
                summary: 'Action Failed',
                detail:
                    error?.error?.error?.message ||
                    'Could not decline assignment.',
            });
        } finally {
            this.actionInProgress[orderId] = false;
            this.selectedOrderForReject = null;
        }
    }

    confirmCompleteDelivery(order: RiderOrderDto): void {
        this.selectedOrderForComplete = order;
        this.deliveryNotes = '';
        this.deliveryRating = null;
        this.completeDialogVisible = true;
    }

    cancelCompleteDelivery(): void {
        if (this.actionInProgress[this.selectedOrderForComplete?.id || 0]) {
            return;
        }
        this.completeDialogVisible = false;
        this.selectedOrderForComplete = null;
        this.deliveryNotes = '';
        this.deliveryRating = null;
    }

    async submitCompleteDelivery(): Promise<void> {
        const order = this.selectedOrderForComplete;
        if (!order) {
            return;
        }

        this.actionInProgress[order.id] = true;
        try {
            await this.riderService.completeDelivery(
                order.id,
                this.deliveryNotes,
                this.deliveryRating
            );
            this.messageService.add({
                severity: 'success',
                summary: 'Delivered',
                detail: `Order ${order.orderNo} marked as Delivered. Admin will complete payment.`,
            });
            this.completeDialogVisible = false;
            this.selectedOrderForComplete = null;
            this.deliveryNotes = '';
            this.deliveryRating = null;
            await this.loadData(true);
        } catch (error: any) {
            this.messageService.add({
                severity: 'error',
                summary: 'Action Failed',
                detail:
                    error?.error?.error?.message ||
                    'Could not mark order as delivered.',
            });
        } finally {
            this.actionInProgress[order.id] = false;
        }
    }

    navigateToLocation(order: RiderOrderDto): void {
        let destination = '';
        const coords = this.getOrderCoords(order);
        if (coords) {
            destination = coords;
        } else {
            const d = this.deliveryDisplay(order);
            const addr = [d.area, d.address, d.landmark].filter(Boolean).join(', ');
            if (!addr.trim()) {
                this.messageService.add({
                    severity: 'warn',
                    summary: 'Location Missing',
                    detail: 'No address or coordinates specified for this order.',
                });
                return;
            }
            destination = encodeURIComponent(addr.trim());
        }

        // Try getting current location to pass origin
        if (navigator.geolocation) {
            navigator.geolocation.getCurrentPosition(
                (pos) => {
                    const origin = `${pos.coords.latitude},${pos.coords.longitude}`;
                    const url = `https://www.google.com/maps/dir/?api=1&origin=${origin}&destination=${destination}&travelmode=driving`;
                    window.open(url, '_blank');
                },
                () => {
                    const url = `https://www.google.com/maps/dir/?api=1&destination=${destination}&travelmode=driving`;
                    window.open(url, '_blank');
                },
                { timeout: 4000, enableHighAccuracy: true }
            );
        } else {
            const url = `https://www.google.com/maps/dir/?api=1&destination=${destination}&travelmode=driving`;
            window.open(url, '_blank');
        }
    }

    getDealDescriptions(item: any): string[] {
        if (!item?.dealItemDescriptions) return [];
        if (Array.isArray(item.dealItemDescriptions)) return item.dealItemDescriptions;
        if (typeof item.dealItemDescriptions === 'string') {
            return item.dealItemDescriptions
                .split(',')
                .map((s: string) => s.trim())
                .filter((s: string) => s.length > 0);
        }
        return [];
    }

    getTimeAgo(order: RiderOrderDto): string {
        if (order.riderAssignedTimeText) {
            return order.riderAssignedTimeText;
        }
        if (!order.riderAssignedTime) return 'Recently';
        const d = new Date(order.riderAssignedTime);
        if (isNaN(d.getTime())) return 'Recently';
        const diffMs = Date.now() - d.getTime();
        if (diffMs < 0) return 'Just now';
        const diffMins = Math.floor(diffMs / 60000);
        if (diffMins < 1) return 'Just now';
        if (diffMins < 60) return `${diffMins}m ago`;
        const diffHours = Math.floor(diffMins / 60);
        if (diffHours < 24) return `${diffHours}h ago`;
        return d.toLocaleDateString();
    }

    paymentLabel(order: RiderOrderDto): string {
        const raw = (order.paymentMethod || '').trim();
        if (!raw) return 'Payment';
        if (/online/i.test(raw)) return 'Online Payment';
        if (/cash/i.test(raw)) return 'Cash on Delivery';
        return raw;
    }

    deliveryDisplay(order: RiderOrderDto | null | undefined): {
        address?: string;
        landmark?: string;
        specialNote?: string;
        riderNote?: string;
        area?: string;
        coords?: string | null;
    } {
        if (!order) {
            return {};
        }
        const parsed = this.parseDeliveryFromNotes(order.notes);
        const rawAddr = (order.customerAddress || '').trim();
        const addressLooksLikeBlob =
            /Addr:|Landmark:|Maps:|Options:|Payment:|google\.com\/maps|maps\.google|\d{1,3}\.\d{3,},\s*-?\d{1,3}\.\d{3,}/i.test(
                rawAddr
            );
        const address = this.stripCoordsAndMaps(
            parsed.address || (!addressLooksLikeBlob ? rawAddr : undefined)
        );
        return {
            address,
            landmark: this.stripCoordsAndMaps(parsed.landmark),
            specialNote: this.stripCoordsAndMaps(parsed.specialNote),
            riderNote: this.stripCoordsAndMaps(parsed.riderNote),
            area: (order.deliveryAreaName || '').trim() || undefined,
            coords: this.getOrderCoords(order),
        };
    }

    /** Compact address line for the card (area · address · landmark). Never shows lat/lng. */
    shortAddress(order: RiderOrderDto): string {
        const d = this.deliveryDisplay(order);
        const parts = [d.area, d.address, d.landmark].filter(Boolean) as string[];
        if (!parts.length) {
            return 'Address not specified';
        }
        const line = parts.join(' · ');
        return line.length > 72 ? `${line.slice(0, 70)}…` : line;
    }

    hasDeliveryLocation(order: RiderOrderDto): boolean {
        const d = this.deliveryDisplay(order);
        return !!(d.area || d.address || d.landmark);
    }

    getOrderCoords(order: RiderOrderDto | null | undefined): string | null {
        if (!order) {
            return null;
        }
        if (order.latitude != null && order.longitude != null) {
            return `${order.latitude},${order.longitude}`;
        }
        const fromNotes = (order.notes || '').match(
            /(?:maps\.google\.com|(?:www\.)?google\.com\/maps)\/?\?q=(-?\d+(?:\.\d+)?),(-?\d+(?:\.\d+)?)/i
        );
        if (fromNotes) {
            return `${fromNotes[1]},${fromNotes[2]}`;
        }
        const bare = (order.notes || order.customerAddress || '').match(
            /(-?\d{1,3}\.\d{4,})\s*,\s*(-?\d{1,3}\.\d{4,})/
        );
        return bare ? `${bare[1]},${bare[2]}` : null;
    }

    async copyOrderCoords(order: RiderOrderDto, event?: Event): Promise<void> {
        event?.stopPropagation();
        event?.preventDefault();
        const coords = this.getOrderCoords(order);
        if (!coords) {
            this.messageService.add({
                severity: 'warn',
                summary: 'No coordinates',
                detail: 'This order has no GPS location to copy.',
            });
            return;
        }
        try {
            await navigator.clipboard.writeText(coords);
            this.messageService.add({
                severity: 'success',
                summary: 'Copied',
                detail: 'Coordinates copied to clipboard.',
                life: 2000,
            });
        } catch {
            this.messageService.add({
                severity: 'error',
                summary: 'Copy failed',
                detail: 'Could not copy coordinates.',
            });
        }
    }

    private stripCoordsAndMaps(value?: string): string | undefined {
        if (!value) {
            return undefined;
        }
        const cleaned = value
            .replace(/https?:\/\/(?:www\.)?google\.com\/maps[^\s|]*/gi, '')
            .replace(/https?:\/\/maps\.google\.com[^\s|]*/gi, '')
            .replace(/(?:www\.)?google\.com\/maps[^\s|]*/gi, '')
            .replace(/maps\.google\.com[^\s|]*/gi, '')
            .replace(/Maps:\s*[^\s|]+/gi, '')
            .replace(/-?\d{1,3}\.\d{4,}\s*,\s*-?\d{1,3}\.\d{4,}/g, '')
            .replace(/\|\s*/g, ' ')
            .replace(/[·•]+/g, ' ')
            .replace(/\s+/g, ' ')
            .trim();
        return cleaned || undefined;
    }

    /** Only customer/rider special notes — never Options / Payment / maps clutter. */
    displayNotes(order: RiderOrderDto): string {
        const d = this.deliveryDisplay(order);
        const parts = [d.specialNote, d.riderNote].filter(Boolean) as string[];
        if (!parts.length) {
            return '';
        }
        const line = parts.join(' · ');
        return line.length > 120 ? `${line.slice(0, 118)}…` : line;
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

        // Drop Options / Payment / Maps / duplicates from leftovers.
        const leftovers = text
            .replace(/https?:\/\/(?:www\.)?google\.com\/maps[^\s|]*/gi, '')
            .replace(/https?:\/\/maps\.google\.com[^\s|]*/gi, '')
            .replace(/(?:www\.)?google\.com\/maps[^\s|]*/gi, '')
            .replace(/maps\.google\.com[^\s|]*/gi, '')
            .replace(/(?:📍\s*)?Addr:\s*[^|]+/gi, '')
            .replace(/(?:🏛️\s*)?Landmark:\s*[^|]+/gi, '')
            .replace(/(?:🗺️\s*)?Maps:\s*[^|]+/gi, '')
            .replace(/(?:📝\s*)?Notes:\s*[^|]+/gi, '')
            .replace(/Options:\s*[^|]*/gi, '')
            .replace(/\[Rider[^\]]*\]/gi, '')
            .replace(/(?:💳\s*)?Payment:\s*[^|]*/gi, '')
            .replace(/-?\d{1,3}\.\d{4,}\s*,\s*-?\d{1,3}\.\d{4,}/g, '')
            .replace(/\|\s*/g, ' ')
            .replace(/[·•]+/g, ' ')
            .replace(/[?？]+/g, ' ')
            .replace(/\s+/g, ' ')
            .trim();

        if (leftovers) {
            const lower = leftovers.toLowerCase();
            if (!address && leftovers.length > 8 && !lower.startsWith('payment')) {
                // Prefer leftover as address only if it looks like a place, not item options.
                if (!/\(\+?pkr/i.test(leftovers) && !/\d+x\s/i.test(leftovers)) {
                    address = leftovers;
                }
            } else if (
                !specialNote &&
                (!address || lower !== address.toLowerCase()) &&
                (!landmark || lower !== landmark.toLowerCase()) &&
                !/\(\+?pkr/i.test(leftovers) &&
                !/\d+x\s/i.test(leftovers)
            ) {
                specialNote = leftovers;
            }
        }

        if (specialNote) {
            const s = specialNote.toLowerCase();
            if (
                (address && s === address.toLowerCase()) ||
                (landmark && s === landmark.toLowerCase()) ||
                s.startsWith('payment:') ||
                s.startsWith('options:')
            ) {
                specialNote = undefined;
            }
        }

        return { address, landmark, specialNote, riderNote };
    }

    elapsedLabel(order: RiderOrderDto): string {
        const start = this.orderStartMs(order);
        if (!start) return this.getTimeAgo(order);
        const totalSec = Math.max(0, Math.floor((this.nowMs - start) / 1000));
        const mins = Math.floor(totalSec / 60);
        const secs = totalSec % 60;
        if (mins < 1) return `${secs}s`;
        if (mins < 60) return `${mins}m ${secs.toString().padStart(2, '0')}s`;
        const h = Math.floor(mins / 60);
        const m = mins % 60;
        return `${h}h ${m}m`;
    }

    isElapsedLate(order: RiderOrderDto): boolean {
        const start = this.orderStartMs(order);
        if (!start) return false;
        return this.nowMs - start > 45 * 60 * 1000;
    }

    orderDayLabel(order: RiderOrderDto): string {
        const d = this.parseOrderDate(order);
        if (!d) return '';
        const diff = this.dayDiffFromToday(d);
        if (diff === 0) return 'Today';
        if (diff === 1) return 'Yesterday';
        return d.toLocaleDateString(undefined, { weekday: 'long' });
    }

    orderWeekdayShort(order: RiderOrderDto): string {
        const d = this.parseOrderDate(order);
        if (!d) return '';
        return d.toLocaleDateString(undefined, { weekday: 'short' });
    }

    orderDayNumber(order: RiderOrderDto): string {
        const d = this.parseOrderDate(order);
        if (!d) return '';
        return String(d.getDate());
    }

    orderDayTone(order: RiderOrderDto): string {
        const d = this.parseOrderDate(order);
        if (!d) return 'is-weekday';
        const diff = this.dayDiffFromToday(d);
        if (diff === 0) return 'is-today';
        if (diff === 1) return 'is-yesterday';
        return 'is-weekday';
    }

    private orderStartMs(order: RiderOrderDto): number | null {
        const assigned = order.riderAssignedTime
            ? new Date(order.riderAssignedTime)
            : null;
        if (assigned && !isNaN(assigned.getTime())) {
            return assigned.getTime();
        }
        const ordered = this.parseOrderDate(order);
        return ordered ? ordered.getTime() : null;
    }

    private parseOrderDate(order: RiderOrderDto): Date | null {
        if (!order?.orderDate) return null;
        const d = new Date(order.orderDate);
        return isNaN(d.getTime()) ? null : d;
    }

    private dayDiffFromToday(date: Date): number {
        const today = new Date();
        const a = new Date(today.getFullYear(), today.getMonth(), today.getDate());
        const b = new Date(date.getFullYear(), date.getMonth(), date.getDate());
        return Math.round((a.getTime() - b.getTime()) / 86400000);
    }
}
