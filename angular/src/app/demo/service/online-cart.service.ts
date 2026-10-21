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

@Injectable({ providedIn: 'root' })
export class OnlineCartService {
    private readonly STORAGE_KEY = 'pos_online_cart_items';
    private cartSubject = new BehaviorSubject<CartItem[]>(this.loadCart());
    cart$ = this.cartSubject.asObservable();
    sidebarVisible = false;

    get items(): CartItem[] {
        return this.cartSubject.value;
    }

    get totalItemsCount(): number {
        return this.items.reduce((sum, i) => sum + i.quantity, 0);
    }

    get cartSubtotal(): number {
        return this.items.reduce((sum, i) => sum + i.quantity * i.unitPrice, 0);
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
