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

    constructor(
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
