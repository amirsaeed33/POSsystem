import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

export interface CartItem {
    productId: number;
    name: string;
    unitName: string;
    unitPrice: number;
    quantity: number;
    imagePath?: string;
    optionsText?: string;
    selectedOptionIds?: number[];
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
        return this.items
            .filter((c) => c.productId === productId)
            .reduce((sum, i) => sum + i.quantity, 0);
    }

    addToCart(
        product: any,
        qty: number = 1,
        options?: {
            unitPrice?: number;
            optionsText?: string;
            selectedOptionIds?: number[];
        }
    ): void {
        const countToAdd = Math.max(1, qty || 1);
        const unitPrice =
            options?.unitPrice != null ? options.unitPrice : (product.price || 0);
        const optionsText = options?.optionsText || '';
        const current = [...this.items];
        const existing = current.find(
            (c) =>
                c.productId === product.id &&
                (c.optionsText || '') === optionsText
        );
        if (existing) {
            existing.quantity += countToAdd;
        } else {
            current.push({
                productId: product.id,
                name: product.name,
                unitName: product.unitName || 'Pcs',
                unitPrice: unitPrice,
                quantity: countToAdd,
                imagePath: product.imagePath,
                optionsText: optionsText || undefined,
                selectedOptionIds: options?.selectedOptionIds,
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
        const filtered = this.items.filter((c) => c !== item);
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
