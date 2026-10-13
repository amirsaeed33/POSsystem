import { AppFormatService } from 'src/app/demo/service/app-format.service';
import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { CustomerOrderService } from 'src/app/demo/service/customer-order.service';
import { OnlineCartService, CartItem } from 'src/app/demo/service/online-cart.service';
import { CreateCustomerOrderDto } from 'src/app/demo/api/customer-order';
import { BranchContextService } from 'src/app/demo/service/branch-context.service';
import { BranchService } from 'src/app/demo/service/branch.service';
import { MessageService } from 'primeng/api';
import { environment } from 'src/environments/environment';

@Component({
    selector: 'app-online-order',
    templateUrl: './online-order.component.html',
    providers: [MessageService],
})
export class OnlineOrderComponent implements OnInit {
    branchId: number | null = null;
    branchName = '';
    currencyFormat = 'PKR';
    dateFormat = 'dd/MM/yyyy';
    branchMissingError = false;
    availableBranches: any[] = [];
    selectedBranchIdForModal: number | null = null;

    products: any[] = [];
    filteredProducts: any[] = [];
    categories: string[] = [];
    selectedCategory = 'All';

    searchKeyword = '';

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

    loading = false;

    constructor(
        public formatService: AppFormatService,
        public cartService: OnlineCartService,
        private customerOrderService: CustomerOrderService,
        private branchContext: BranchContextService,
        private branchService: BranchService,
        private route: ActivatedRoute,
        private router: Router,
        private messageService: MessageService
    ) {}

    ngOnInit(): void {
        this.route.queryParams.subscribe((params) => {
            const paramBranchId = params['branchId'];
            const paramProductId = params['productId'];
            if (paramBranchId && !isNaN(Number(paramBranchId))) {
                this.branchId = Number(paramBranchId);
                this.branchMissingError = false;
                this.resolveBranchName(this.branchId);
                this.loadCatalog(this.branchId, paramProductId ? Number(paramProductId) : undefined);
            } else {
                this.handleMissingBranchId();
            }
        });
    }

    async resolveBranchName(bId: number): Promise<void> {
        try {
            const info = await this.customerOrderService.getOnlineStoreHeader(bId);
            if (info && info.branchName) {
                this.branchName = info.branchName;
                return;
            }
        } catch {
            // Ignore error
        }
        this.branchName = '';
    }

    async handleMissingBranchId(): Promise<void> {
        // 1. Check if BranchContext has an active branch
        const ctxBranchId = this.branchContext.getBranchId();
        if (ctxBranchId) {
            this.setBranchInUrl(ctxBranchId);
            return;
        }

        // 2. Otherwise try loading available branches
        try {
            this.loading = true;
            await this.branchContext.ensureLoaded();
            const allowed = this.branchContext.getAllowedBranches();
            if (allowed && allowed.length > 0) {
                this.setBranchInUrl(allowed[0].id);
                return;
            }

            const branches = await this.branchService.getLookup();
            this.availableBranches = branches || [];
            if (this.availableBranches.length === 1) {
                this.setBranchInUrl(this.availableBranches[0].id);
                return;
            } else if (this.availableBranches.length > 1) {
                this.branchMissingError = true;
                return;
            }
        } catch {
            // Error loading branches
        } finally {
            this.loading = false;
        }

        this.branchMissingError = true;
    }

    setBranchInUrl(bId: number): void {
        this.router.navigate([], {
            relativeTo: this.route,
            queryParams: { branchId: bId },
            queryParamsHandling: 'merge',
        });
    }

    onSelectBranchFromModal(): void {
        if (!this.selectedBranchIdForModal) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Branch Required',
                detail: 'Please select a branch to view the online store.',
            });
            return;
        }
        this.setBranchInUrl(this.selectedBranchIdForModal);
    }

    async loadCatalog(bId?: number, selectProductId?: number): Promise<void> {
        try {
            this.loading = true;
            this.products = await this.customerOrderService.getOnlineCatalog(bId || undefined);
            this.extractCategories();
            this.applyFilter();

            if (selectProductId) {
                const target = this.products.find((p) => p.id === selectProductId);
                if (target) {
                    this.openProductDetails(target);
                }
            }
        } catch (error: any) {
            this.messageService.add({
                severity: 'error',
                summary: 'Error',
                detail: error?.message || 'Failed to load catalog products',
            });
        } finally {
            this.loading = false;
        }
    }

    extractCategories(): void {
        const set = new Set<string>();
        this.products.forEach((p) => {
            if (p.categoryName) {
                set.add(p.categoryName);
            }
        });
        this.categories = ['All', ...Array.from(set)];
    }

    selectCategory(cat: string): void {
        this.selectedCategory = cat;
        this.applyFilter();
    }

    applyFilter(): void {
        const query = (this.searchKeyword || '').toLowerCase().trim();
        this.filteredProducts = this.products.filter((p) => {
            const matchesCat =
                this.selectedCategory === 'All' ||
                p.categoryName === this.selectedCategory;
            const matchesSearch =
                !query ||
                p.name.toLowerCase().includes(query) ||
                (p.categoryName || '').toLowerCase().includes(query);
            return matchesCat && matchesSearch;
        });
    }

    openProductDetails(product: any): void {
        this.router.navigate(['/online-order/product', product.id], {
            queryParams: this.branchId ? { branchId: this.branchId } : {},
        });
    }

    getCartQuantityForProduct(productId: number): number {
        return this.cartService.getItemQuantity(productId);
    }

    addToCart(product: any, qty: number = 1, event?: Event): void {
        if (event) {
            event.stopPropagation();
        }
        if (this.branchMissingError || !this.branchId) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Branch Required',
                detail: 'Please select a branch before adding items to cart.',
            });
            return;
        }

        const countToAdd = Math.max(1, qty || 1);
        this.cartService.addToCart(product, countToAdd);
        this.messageService.add({
            severity: 'success',
            summary: 'Added to Cart',
            detail: `${countToAdd > 1 ? countToAdd + 'x ' : ''}${product.name} added to your order`,
            life: 1800,
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
