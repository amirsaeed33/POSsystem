import { Injectable } from '@angular/core';

export interface MyOnlineOrderRef {
    orderNo: string;
    branchId?: number | null;
    branchName?: string;
    totalAmount?: number;
    customerName?: string;
    customerMobile?: string;
    placedAt: string;
    status?: number | null;
    statusName?: string;
}

const STORAGE_KEY = 'smartpos_my_online_orders';
const MAX_ORDERS = 12;

@Injectable({ providedIn: 'root' })
export class MyOnlineOrdersService {
    remember(order: MyOnlineOrderRef): void {
        if (!order?.orderNo) {
            return;
        }
        const list = this.getAll().filter(
            (o) =>
                (o.orderNo || '').toLowerCase() !==
                order.orderNo.trim().toLowerCase()
        );
        list.unshift({
            orderNo: order.orderNo.trim(),
            branchId: order.branchId ?? null,
            branchName: order.branchName || '',
            totalAmount: order.totalAmount ?? 0,
            customerName: order.customerName || '',
            customerMobile: order.customerMobile || '',
            placedAt: order.placedAt || new Date().toISOString(),
            status: order.status ?? 0,
            statusName: order.statusName || 'Pending',
        });
        localStorage.setItem(
            STORAGE_KEY,
            JSON.stringify(list.slice(0, MAX_ORDERS))
        );
    }

    updateStatus(
        orderNo: string,
        status: number,
        statusName?: string
    ): void {
        const key = (orderNo || '').trim().toLowerCase();
        if (!key) {
            return;
        }
        const list = this.getAll().map((o) =>
            (o.orderNo || '').toLowerCase() === key
                ? {
                      ...o,
                      status,
                      statusName: statusName || o.statusName,
                  }
                : o
        );
        localStorage.setItem(STORAGE_KEY, JSON.stringify(list));
    }

    isActiveStatus(status?: number | null): boolean {
        // Pending / Approved / InProgress — still running for the customer.
        return status === 0 || status === 1 || status === 3 || status == null;
    }

    getAll(): MyOnlineOrderRef[] {
        try {
            const raw = localStorage.getItem(STORAGE_KEY);
            if (!raw) {
                return [];
            }
            const parsed = JSON.parse(raw);
            return Array.isArray(parsed) ? parsed : [];
        } catch {
            return [];
        }
    }

    getForBranch(branchId?: number | null): MyOnlineOrderRef[] {
        const all = this.getAll();
        if (branchId == null || !branchId) {
            return all;
        }
        const matched = all.filter((o) => o.branchId === branchId);
        return matched.length ? matched : all;
    }

    clear(): void {
        localStorage.removeItem(STORAGE_KEY);
    }
}
