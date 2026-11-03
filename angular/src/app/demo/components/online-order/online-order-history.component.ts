import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { MessageService } from 'primeng/api';
import {
    CustomerOrderDto,
    CustomerOrderStatus,
} from 'src/app/demo/api/customer-order';
import { CustomerOrderService } from 'src/app/demo/service/customer-order.service';

const PHONE_STORAGE_KEY = 'smartpos_customer_phone';

@Component({
    selector: 'app-online-order-history',
    templateUrl: './online-order-history.component.html',
    styleUrls: ['./online-order-history.component.scss'],
    providers: [MessageService],
})
export class OnlineOrderHistoryComponent implements OnInit {
    phone = '';
    branchId: number | null = null;
    orders: CustomerOrderDto[] = [];
    loading = false;
    searched = false;
    errorMessage = '';

    constructor(
        private route: ActivatedRoute,
        private router: Router,
        private customerOrderService: CustomerOrderService,
        private messageService: MessageService
    ) {}

    ngOnInit(): void {
        const qBranch = this.route.snapshot.queryParamMap.get('branchId');
        if (qBranch && !isNaN(Number(qBranch))) {
            this.branchId = Number(qBranch);
        }

        const qPhone = this.route.snapshot.queryParamMap.get('phone');
        const saved = this.readSavedPhone();
        this.phone = (qPhone || saved || '').trim();

        if (this.phone.length >= 7) {
            void this.search();
        }
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
        const orderNo = order.orderNo || String(order.id);
        this.router.navigate(['/online-order/track', orderNo], {
            queryParams: {
                ...(this.branchId || order.branchId
                    ? { branchId: this.branchId || order.branchId }
                    : {}),
            },
        });
    }

    backToStore(): void {
        this.router.navigate(['/online-order'], {
            queryParams: this.branchId ? { branchId: this.branchId } : {},
        });
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
