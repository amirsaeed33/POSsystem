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

    /** Compact one-line address for history cards. */
    shortAddress(order: RiderOrderDto): string {
        const d = this.deliveryDisplay(order);
        const parts = [d.area, d.address, d.landmark].filter(Boolean) as string[];
        if (!parts.length) {
            return 'Address not listed';
        }
        const line = parts.join(' · ');
        return line.length > 72 ? `${line.slice(0, 70)}…` : line;
    }

    deliveryDisplay(order: RiderOrderDto | null | undefined): {
        address?: string;
        landmark?: string;
        specialNote?: string;
        riderNote?: string;
        area?: string;
        mapsUrl?: string | null;
        coords?: string | null;
    } {
        if (!order) {
            return {};
        }
        const parsed = this.parseDeliveryFromNotes(order.notes);
        const rawAddr = (order.customerAddress || '').trim();
        // If customerAddress itself is the notes blob, prefer parsed address.
        const addressLooksLikeBlob =
            /Addr:|Landmark:|Maps:|google\.com\/maps|maps\.google/i.test(rawAddr);
        const strip = (value?: string) => {
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
        };
        const address = strip(
            parsed.address || (!addressLooksLikeBlob ? rawAddr : undefined)
        );
        return {
            address,
            landmark: strip(parsed.landmark),
            specialNote: strip(parsed.specialNote),
            riderNote: strip(parsed.riderNote),
            area: (order.deliveryAreaName || '').trim() || undefined,
            mapsUrl: this.getGoogleMapsLink(order.notes, order),
            coords: this.getOrderCoords(order),
        };
    }

    getOrderCoords(order: RiderOrderDto | null | undefined): string | null {
        if (!order) return null;
        if (order.latitude != null && order.longitude != null) {
            return `${order.latitude},${order.longitude}`;
        }
        const fromNotes = (order.notes || '').match(
            /(?:maps\.google\.com|(?:www\.)?google\.com\/maps)\/?\?q=(-?\d+(?:\.\d+)?),(-?\d+(?:\.\d+)?)/i
        );
        if (fromNotes) return `${fromNotes[1]},${fromNotes[2]}`;
        return null;
    }

    async copyOrderCoords(order: RiderOrderDto, event?: Event): Promise<void> {
        event?.stopPropagation();
        const coords = this.getOrderCoords(order);
        if (!coords) return;
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

    getGoogleMapsLink(
        notes?: string,
        order?: RiderOrderDto | null
    ): string | null {
        if (order?.latitude != null && order?.longitude != null) {
            return `https://maps.google.com/?q=${order.latitude},${order.longitude}`;
        }
        if (!notes) return null;
        const match =
            notes.match(/https?:\/\/(?:www\.)?google\.com\/maps[^\s|]+/i) ||
            notes.match(/https?:\/\/maps\.google\.com[^\s|]+/i);
        return match ? match[0].replace(/[)\],.]+$/, '') : null;
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
                const lower = leftovers.toLowerCase();
                if (!address && leftovers.length > 8 && !lower.startsWith('payment')) {
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
