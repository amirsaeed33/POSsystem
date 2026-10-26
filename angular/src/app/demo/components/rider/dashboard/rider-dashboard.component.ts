import { Component, OnDestroy, OnInit } from '@angular/core';
import { MessageService, ConfirmationService } from 'primeng/api';
import {
    RiderDashboardStatsDto,
    RiderOrderDto,
    RiderOrderStatus,
} from 'src/app/demo/api/rider';
import { RiderService } from 'src/app/demo/service/rider.service';

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

    // Auto-refresh interval
    private pollInterval: any;
    autoRefreshEnabled = true;

    readonly OrderStatus = RiderOrderStatus;

    constructor(
        private riderService: RiderService,
        private messageService: MessageService,
        private confirmationService: ConfirmationService
    ) {}

    ngOnInit(): void {
        this.loadData();
        this.pollInterval = setInterval(() => {
            if (this.autoRefreshEnabled && !this.loading) {
                this.loadData(true);
            }
        }, 25000);
    }

    ngOnDestroy(): void {
        if (this.pollInterval) {
            clearInterval(this.pollInterval);
        }
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
        this.confirmationService.confirm({
            message: `Are you sure you want to mark Order ${order.orderNo} as DELIVERED?`,
            header: 'Confirm Delivery Completion',
            icon: 'pi pi-check-circle',
            acceptLabel: 'Yes, Delivered',
            rejectLabel: 'Cancel',
            acceptButtonStyleClass: 'p-button-success',
            accept: async () => {
                await this.completeDelivery(order);
            },
        });
    }

    private async completeDelivery(order: RiderOrderDto): Promise<void> {
        this.actionInProgress[order.id] = true;
        try {
            await this.riderService.completeDelivery(order.id);
            this.messageService.add({
                severity: 'success',
                summary: 'Delivery Completed',
                detail: `Order ${order.orderNo} marked as Delivered!`,
            });
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
}
