import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { MessageService } from 'primeng/api';
import { CustomerOrderService } from 'src/app/demo/service/customer-order.service';
import { OnlineCartService, CartItem } from 'src/app/demo/service/online-cart.service';
import { CreateCustomerOrderDto } from 'src/app/demo/api/customer-order';
import { environment } from 'src/environments/environment';

@Component({
    selector: 'app-online-product-detail',
    templateUrl: './online-product-detail.component.html',
    providers: [MessageService],
})
export class OnlineProductDetailComponent implements OnInit {
    productId: number | null = null;
    branchId: number | null = null;
    branchName = '';
    currencyFormat = 'PKR';
    dateFormat = 'dd/MM/yyyy';

    product: any = null;
    relatedProducts: any[] = [];
    loading = false;
    quantity = 1;

    // Checkout modal & state
    checkoutModalVisible = false;
    customerName = '';
    customerMobile = '';
    notes = '';
    submitting = false;

    // Success confirmation modal
    successModalVisible = false;
    confirmedOrderNo = '';
    confirmedTotal = 0;

    constructor(
        public cartService: OnlineCartService,
        private customerOrderService: CustomerOrderService,
        private route: ActivatedRoute,
        private router: Router,
        private messageService: MessageService
    ) {}

    ngOnInit(): void {
        this.route.paramMap.subscribe((params) => {
            const idParam = params.get('id');
            if (idParam && !isNaN(Number(idParam))) {
                this.productId = Number(idParam);
                this.quantity = 1;
                this.checkAndLoad();
            }
        });

        this.route.queryParamMap.subscribe((qParams) => {
            const bId = qParams.get('branchId');
            if (bId && !isNaN(Number(bId))) {
                this.branchId = Number(bId);
                this.resolveBranchInfo(this.branchId);
            }
            if (this.productId) {
                this.checkAndLoad();
            }
        });
    }

    async resolveBranchInfo(bId: number): Promise<void> {
        try {
            const info = await this.customerOrderService.getOnlineStoreHeader(bId);
            if (info && info.branchName) {
                this.branchName = info.branchName;
                this.currencyFormat = info.currencyFormat || 'PKR';
                this.dateFormat = info.dateFormat || 'dd/MM/yyyy';
            }
        } catch {
            this.branchName = '';
        }
    }

    async checkAndLoad(): Promise<void> {
        if (!this.productId) return;
        try {
            this.loading = true;
            this.product = await this.customerOrderService.getOnlineProduct(
                this.productId,
                this.branchId || undefined
            );

            // Load related products from catalog
            this.loadRelatedProducts();
        } catch (error: any) {
            this.messageService.add({
                severity: 'error',
                summary: 'Error',
                detail: error?.message || 'Failed to load product details',
            });
        } finally {
            this.loading = false;
        }
    }

    async loadRelatedProducts(): Promise<void> {
        try {
            const catalog = await this.customerOrderService.getOnlineCatalog(
                this.branchId || undefined
            );
            if (catalog && catalog.length > 0) {
                this.relatedProducts = catalog
                    .filter((p) => p.id !== this.productId)
                    .slice(0, 4);
            }
        } catch {
            this.relatedProducts = [];
        }
    }

    incrementQuantity(): void {
        this.quantity = (this.quantity || 1) + 1;
    }

    decrementQuantity(): void {
        if (this.quantity > 1) {
            this.quantity--;
        }
    }

    onQuantityChange(): void {
        if (!this.quantity || this.quantity < 1) {
            this.quantity = 1;
        } else {
            this.quantity = Math.floor(this.quantity);
        }
    }

    addToCart(item?: any, qty?: number): void {
        const prod = item || this.product;
        if (!prod) return;

        const count = qty || this.quantity || 1;
        this.cartService.addToCart(prod, count);
        this.messageService.add({
            severity: 'success',
            summary: 'Added to Cart',
            detail: `${count > 1 ? count + 'x ' : ''}${prod.name} added to your order`,
            life: 2000,
        });
    }

    navigateToProduct(targetId: number): void {
        this.router.navigate(['/online-order/product', targetId], {
            queryParams: this.branchId ? { branchId: this.branchId } : {},
        });
    }

    backToStore(): void {
        this.router.navigate(['/online-order'], {
            queryParams: this.branchId ? { branchId: this.branchId } : {},
        });
    }

    openCheckout(): void {
        if (!this.cartService.items.length) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Cart Empty',
                detail: 'Please add at least one product to your order.',
            });
            return;
        }
        this.cartService.sidebarVisible = false;
        this.checkoutModalVisible = true;
    }

    async submitOrder(): Promise<void> {
        if (!this.customerName.trim()) {
            this.messageService.add({
                severity: 'error',
                summary: 'Validation Error',
                detail: 'Please enter your Full Name',
            });
            return;
        }
        if (!this.customerMobile.trim()) {
            this.messageService.add({
                severity: 'error',
                summary: 'Validation Error',
                detail: 'Please enter your Mobile/Phone Number',
            });
            return;
        }

        try {
            this.submitting = true;
            const input: CreateCustomerOrderDto = {
                customerName: this.customerName.trim(),
                customerMobile: this.customerMobile.trim(),
                branchId: this.branchId || undefined,
                notes: this.notes.trim(),
                lines: this.cartService.items.map((c) => ({
                    productId: c.productId,
                    quantity: c.quantity,
                    unitPrice: c.unitPrice,
                })),
            };

            const createdOrder = await this.customerOrderService.createOnlineOrder(input);

            this.confirmedOrderNo = createdOrder.orderNo || `ORD-${createdOrder.id}`;
            this.confirmedTotal = createdOrder.totalAmount;

            // Reset cart & checkout modal
            this.cartService.clearCart();
            this.customerName = '';
            this.customerMobile = '';
            this.notes = '';
            this.checkoutModalVisible = false;
            this.cartService.sidebarVisible = false;

            // Show success modal
            this.successModalVisible = true;
        } catch (error: any) {
            this.messageService.add({
                severity: 'error',
                summary: 'Order Failed',
                detail: error?.message || 'Failed to place order. Please try again.',
            });
        } finally {
            this.submitting = false;
        }
    }

    getImageUrl(path?: string): string {
        if (!path) return 'assets/demo/images/product/placeholder.png';
        if (path.startsWith('http://') || path.startsWith('https://')) return path;
        return `${environment.apiUrl}${path.startsWith('/') ? '' : '/'}${path}`;
    }

    getInitials(name: string): string {
        if (!name) return 'PR';
        const parts = name.trim().split(' ');
        if (parts.length >= 2) {
            return (parts[0][0] + parts[1][0]).toUpperCase();
        }
        return name.slice(0, 2).toUpperCase();
    }
}
