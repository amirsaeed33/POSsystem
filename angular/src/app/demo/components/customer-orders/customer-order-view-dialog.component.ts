import { AppFormatService } from 'src/app/demo/service/app-format.service';
import {
    Component,
    EventEmitter,
    Input,
    OnChanges,
    Output,
    SimpleChanges,
} from '@angular/core';
import { MessageService } from 'primeng/api';
import {
    CustomerOrderDto,
    CustomerOrderStatus,
} from 'src/app/demo/api/customer-order';
import { CustomerOrderService } from 'src/app/demo/service/customer-order.service';

@Component({
    selector: 'app-customer-order-view-dialog',
    templateUrl: './customer-order-view-dialog.component.html',
})
export class CustomerOrderViewDialogComponent implements OnChanges {
    @Input() visible = false;
    @Input() orderId: number | null = null;
    @Output() visibleChange = new EventEmitter<boolean>();

    order: CustomerOrderDto | null = null;
    loading = false;

    constructor(public formatService: AppFormatService, 
        private customerOrderService: CustomerOrderService,
        private messageService: MessageService
    ) {}

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
                return 'Approved';
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

    getOrderSubtotal(): number {
        if (!this.order) return 0;
        if (this.order.subtotal && this.order.subtotal > 0) {
            return this.order.subtotal;
        }
        if (this.order.lines && this.order.lines.length > 0) {
            return this.order.lines.reduce((acc, l) => acc + (l.lineTotal || (l.quantity * l.unitPrice)), 0);
        }
        return this.order.totalAmount || 0;
    }

    getOrderDeliveryCharges(): number {
        if (!this.order) return 0;
        if (this.order.deliveryCharges !== undefined && this.order.deliveryCharges !== null) {
            return this.order.deliveryCharges;
        }
        if (this.order.delivery_charges !== undefined && this.order.delivery_charges !== null) {
            return this.order.delivery_charges;
        }
        const subtotal = this.getOrderSubtotal();
        const diff = (this.order.totalAmount || 0) - subtotal;
        return diff > 0 ? diff : 0;
    }

    getDeliveryAreaName(): string | null {
        if (!this.order) return null;
        if (this.order.deliveryArea) return this.order.deliveryArea;
        if (this.order.delivery_area) return this.order.delivery_area;
        if (this.order.notes) {
            const match = this.order.notes.match(/🛵 Delivery:[^|(]+\(([^)]+)\)/);
            if (match) return match[1];
        }
        return null;
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
