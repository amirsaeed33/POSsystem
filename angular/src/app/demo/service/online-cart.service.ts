import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

export interface CartItem {
    productId: number;
    name: string;
    unitName: string;
    unitPrice: number;
    quantity: number;
    imagePath?: string;
}

export interface DeliveryAreaZone {
    id: string;
    name: string;
    fee: number;
    estimatedTime: string;
    badge?: string;
}

export const DEFAULT_DELIVERY_AREAS: DeliveryAreaZone[] = [
    { id: 'standard', name: 'Nearby Area / Standard (within 5 km)', fee: 100, estimatedTime: '20-35 mins', badge: 'Popular' },
    { id: 'city_center', name: 'City Center / Commercial Area (5 - 10 km)', fee: 150, estimatedTime: '30-45 mins' },
    { id: 'outskirts', name: 'Outskirts / Suburbs (10 - 15 km)', fee: 220, estimatedTime: '45-60 mins' },
    { id: 'express', name: '⚡ Priority / Express Rush Delivery', fee: 280, estimatedTime: '15-25 mins', badge: 'Fastest' },
];

@Injectable({ providedIn: 'root' })
export class OnlineCartService {
    private readonly STORAGE_KEY = 'pos_online_cart_items';
    private readonly ZONE_KEY = 'pos_online_delivery_zone';
    private cartSubject = new BehaviorSubject<CartItem[]>(this.loadCart());
    cart$ = this.cartSubject.asObservable();
    sidebarVisible = false;

    // Delivery Fee Configuration
    freeDeliveryThreshold = 1500; // Free delivery for orders >= PKR 1,500
    baseDeliveryFee = 100;
    deliveryAreas: DeliveryAreaZone[] = DEFAULT_DELIVERY_AREAS;
    selectedAreaZoneId: string = this.loadSelectedZone();

    get items(): CartItem[] {
        return this.cartSubject.value;
    }

    get totalItemsCount(): number {
        return this.items.reduce((sum, i) => sum + i.quantity, 0);
    }

    get cartSubtotal(): number {
        return this.items.reduce((sum, i) => sum + i.quantity * i.unitPrice, 0);
    }

    get selectedAreaZone(): DeliveryAreaZone {
        return this.deliveryAreas.find((z) => z.id === this.selectedAreaZoneId) || this.deliveryAreas[0];
    }

    get isFreeDeliveryEligible(): boolean {
        return this.items.length > 0 && this.cartSubtotal >= this.freeDeliveryThreshold;
    }

    get amountNeededForFreeDelivery(): number {
        return Math.max(0, this.freeDeliveryThreshold - this.cartSubtotal);
    }

    get freeDeliveryProgressPercent(): number {
        if (this.freeDeliveryThreshold <= 0) return 100;
        return Math.min(100, Math.round((this.cartSubtotal / this.freeDeliveryThreshold) * 100));
    }

    get deliveryFee(): number {
        if (this.items.length === 0) return 0;
        if (this.isFreeDeliveryEligible) return 0;
        return this.selectedAreaZone ? this.selectedAreaZone.fee : this.baseDeliveryFee;
    }

    get cartGrandTotal(): number {
        return this.cartSubtotal + this.deliveryFee;
    }

    setDeliveryArea(zoneId: string): void {
        this.selectedAreaZoneId = zoneId;
        try {
            localStorage.setItem(this.ZONE_KEY, zoneId);
        } catch {}
    }

    private loadSelectedZone(): string {
        try {
            const saved = localStorage.getItem(this.ZONE_KEY);
            if (saved && this.deliveryAreas.some((a) => a.id === saved)) {
                return saved;
            }
        } catch {}
        return 'standard';
    }

    getItemQuantity(productId: number): number {
        const item = this.items.find((c) => c.productId === productId);
        return item ? item.quantity : 0;
    }

    addToCart(product: any, qty: number = 1): void {
        const countToAdd = Math.max(1, qty || 1);
        const current = [...this.items];
        const existing = current.find((c) => c.productId === product.id);
        if (existing) {
            existing.quantity += countToAdd;
        } else {
            current.push({
                productId: product.id,
                name: product.name,
                unitName: product.unitName || 'Pcs',
                unitPrice: product.price || 0,
                quantity: countToAdd,
                imagePath: product.imagePath,
            });
        }
        this.saveCart(current);
    }

    updateQuantity(item: CartItem, change: number): void {
        item.quantity += change;
        if (item.quantity <= 0) {
            this.removeFromCart(item);
        } else {
            this.saveCart([...this.items]);
        }
    }

    removeFromCart(item: CartItem): void {
        const filtered = this.items.filter((c) => c.productId !== item.productId);
        this.saveCart(filtered);
    }

    clearCart(): void {
        this.saveCart([]);
    }

    private loadCart(): CartItem[] {
        try {
            const raw = localStorage.getItem(this.STORAGE_KEY);
            return raw ? JSON.parse(raw) : [];
        } catch {
            return [];
        }
    }

    private saveCart(items: CartItem[]): void {
        try {
            localStorage.setItem(this.STORAGE_KEY, JSON.stringify(items));
        } catch {}
        this.cartSubject.next(items);
    }
}
