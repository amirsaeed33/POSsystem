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
    private branchSub?: Subscription;
    autoRefreshEnabled = true;

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
    }

    ngOnDestroy(): void {
        this.branchSub?.unsubscribe();
        if (this.pollInterval) {
            clearInterval(this.pollInterval);
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
                summary: 'Delivery Completed',
                detail: `Order ${order.orderNo} marked as Delivered!`,
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
                    'Could not complete delivery.',
            });
        } finally {
            this.actionInProgress[order.id] = false;
        }
    }

    navigateToLocation(order: RiderOrderDto): void {
        let destination = '';
        if (order.latitude && order.longitude) {
            destination = `${order.latitude},${order.longitude}`;
        } else {
            const addr =
                order.customerAddress || order.deliveryAreaName || '';
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
}
