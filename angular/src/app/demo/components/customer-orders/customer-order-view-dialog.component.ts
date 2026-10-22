import { AppFormatService } from 'src/app/demo/service/app-format.service';
import {
    Component,
    EventEmitter,
    Input,
    OnChanges,
    OnInit,
    Output,
    SimpleChanges,
} from '@angular/core';
import { MessageService } from 'primeng/api';
import {
    CustomerOrderDto,
    CustomerOrderStatus,
} from 'src/app/demo/api/customer-order';
import { CustomerOrderService } from 'src/app/demo/service/customer-order.service';
import { PermissionService } from 'src/app/demo/service/permission.service';
import { PermissionNames } from 'src/app/demo/api/permission-names';

@Component({
    selector: 'app-customer-order-view-dialog',
    templateUrl: './customer-order-view-dialog.component.html',
    styles: [`
        .override-audit-box {
            background-color: #fff9db;
            border-left: 4px solid #f59f00;
            padding: 0.75rem 1rem;
            border-radius: 4px;
        }
    `]
})
export class CustomerOrderViewDialogComponent implements OnInit, OnChanges {
    @Input() visible = false;
    @Input() orderId: number | null = null;
    @Output() visibleChange = new EventEmitter<boolean>();
    @Output() orderUpdated = new EventEmitter<CustomerOrderDto>();

    order: CustomerOrderDto | null = null;
    loading = false;

    // Delivery Fee Override Modal
    overrideDialogVisible = false;
    overrideFee = 0;
    overrideReason = '';
    overriding = false;
    canOverrideDeliveryFee = false;

    constructor(
        public formatService: AppFormatService,
        private customerOrderService: CustomerOrderService,
        private messageService: MessageService,
        private permissionService: PermissionService
    ) {}

    ngOnInit(): void {
        this.canOverrideDeliveryFee = this.permissionService.isGranted(
            PermissionNames.CustomerOrdersOverrideDeliveryFee
        );
    }

    ngOnChanges(changes: SimpleChanges): void {
        if (changes['visible'] && this.visible && this.orderId) {
            this.loadOrder(this.orderId);
        }
    }

    onVisibleChange(visible: boolean): void {
        this.visible = visible;
        this.visibleChange.emit(visible);
        if (!visible) {
            this.order = null;
            this.overrideDialogVisible = false;
        }
    }

    onHide(): void {
        this.onVisibleChange(false);
    }

    statusLabel(status: number): string {
        switch (status) {
            case CustomerOrderStatus.Pending:
                return 'Pending';
            case CustomerOrderStatus.Approved:
            case CustomerOrderStatus.InProgress:
                return 'In Progress';
            case CustomerOrderStatus.Delivered:
                return 'Delivered';
            case CustomerOrderStatus.Rejected:
                return 'Rejected';
            default:
                return String(status);
        }
    }

    getGoogleMapsLink(notes?: string): string | null {
        if (!notes) return null;
        const match = notes.match(/https?:\/\/maps\.google\.com[^\s|]+/);
        return match ? match[0] : null;
    }

    openOverrideDialog(): void {
        if (!this.order) return;
        this.overrideFee = this.order.deliveryFee ?? 0;
        this.overrideReason = '';
        this.overrideDialogVisible = true;
    }

    async submitOverride(): Promise<void> {
        if (!this.order) return;

        if (!this.overrideReason || this.overrideReason.trim().length < 3) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Reason Required',
                detail: 'Please provide a clear reason (at least 3 characters) for adjusting delivery charges.',
            });
            return;
        }

        if (this.overrideFee < 0) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Invalid Fee',
                detail: 'Delivery fee cannot be negative.',
            });
            return;
        }

        this.overriding = true;
        try {
            const updated = await this.customerOrderService.overrideDeliveryFee({
                orderId: this.order.id,
                newDeliveryFee: this.overrideFee,
                reason: this.overrideReason.trim(),
            });

            this.order = updated;
            this.overrideDialogVisible = false;
            this.messageService.add({
                severity: 'success',
                summary: 'Delivery Charges Updated',
                detail: `Delivery fee adjusted to PKR ${(updated.deliveryFee ?? 0).toFixed(2)}.`,
            });
            this.orderUpdated.emit(updated);
        } catch (error: any) {
            this.messageService.add({
                severity: 'error',
                summary: 'Adjustment Failed',
                detail: error?.message || 'Failed to update delivery fee',
            });
        } finally {
            this.overriding = false;
        }
    }

    private loadOrder(id: number): void {
        this.loading = true;
        this.order = null;
        this.customerOrderService
            .get(id)
            .then((order) => {
                this.order = order;
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to load customer order',
                });
                this.onHide();
            })
            .finally(() => {
                this.loading = false;
            });
    }
}
