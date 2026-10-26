import { Component, OnInit } from '@angular/core';
import { MessageService } from 'primeng/api';
import {
    RiderOrderDto,
    RiderOrderHistoryInput,
    RiderOrderStatus,
} from 'src/app/demo/api/rider';
import { RiderService } from 'src/app/demo/service/rider.service';

@Component({
    selector: 'app-rider-history',
    templateUrl: './rider-history.component.html',
    styleUrls: ['./rider-history.component.scss'],
    providers: [MessageService],
})
export class RiderHistoryComponent implements OnInit {
    orders: RiderOrderDto[] = [];
    totalCount = 0;
    loading = false;

    // Filters
    activeFilter: 'today' | 'yesterday' | 'week' | 'month' | 'all' = 'today';
    searchKeyword = '';

    // Summary of current view
    totalDeliveredRevenue = 0;

    // Paging
    skipCount = 0;
    maxResultCount = 20;

    // Detail dialog
    detailDialogVisible = false;
    selectedOrder: RiderOrderDto | null = null;

    constructor(
        private riderService: RiderService,
        private messageService: MessageService
    ) {}

    ngOnInit(): void {
        this.applyPresetFilter('today');
    }

    applyPresetFilter(preset: 'today' | 'yesterday' | 'week' | 'month' | 'all'): void {
        this.activeFilter = preset;
        this.skipCount = 0;
        this.loadHistory();
    }

    async loadHistory(): Promise<void> {
        this.loading = true;

        const input: RiderOrderHistoryInput = {
            keyword: (this.searchKeyword || '').trim() || undefined,
            status: RiderOrderStatus.Delivered,
            skipCount: this.skipCount,
            maxResultCount: this.maxResultCount,
        };

        const now = new Date();
        if (this.activeFilter === 'today') {
            const todayStr = this.toDateString(now);
            input.fromDate = todayStr;
            input.toDate = todayStr;
        } else if (this.activeFilter === 'yesterday') {
            const yesterday = new Date();
            yesterday.setDate(now.getDate() - 1);
            const yestStr = this.toDateString(yesterday);
            input.fromDate = yestStr;
            input.toDate = yestStr;
        } else if (this.activeFilter === 'week') {
            const weekAgo = new Date();
            weekAgo.setDate(now.getDate() - 7);
            input.fromDate = this.toDateString(weekAgo);
            input.toDate = this.toDateString(now);
        } else if (this.activeFilter === 'month') {
            const monthStart = new Date(now.getFullYear(), now.getMonth(), 1);
            input.fromDate = this.toDateString(monthStart);
            input.toDate = this.toDateString(now);
        }

        try {
            const res = await this.riderService.getOrderHistory(input);
            this.orders = res.items;
            this.totalCount = res.totalCount;

            this.totalDeliveredRevenue = this.orders.reduce(
                (sum, o) => sum + (o.totalAmount || 0),
                0
            );
        } catch (error: any) {
            this.messageService.add({
                severity: 'error',
                summary: 'Load Failed',
                detail:
                    error?.error?.error?.message ||
                    'Failed to load delivery history.',
            });
        } finally {
            this.loading = false;
        }
    }

    onSearch(): void {
        this.skipCount = 0;
        this.loadHistory();
    }

    onPageChange(event: any): void {
        this.skipCount = event.first ?? 0;
        this.maxResultCount = event.rows ?? 20;
        this.loadHistory();
    }

    openDetails(order: RiderOrderDto): void {
        this.selectedOrder = order;
        this.detailDialogVisible = true;
    }

    private toDateString(d: Date): string {
        const year = d.getFullYear();
        const month = String(d.getMonth() + 1).padStart(2, '0');
        const day = String(d.getDate()).padStart(2, '0');
        return `${year}-${month}-${day}`;
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
