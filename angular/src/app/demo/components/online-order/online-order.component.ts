import { AppFormatService } from 'src/app/demo/service/app-format.service';
import { Component, HostListener, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { CustomerOrderService } from 'src/app/demo/service/customer-order.service';
import { OnlineCartService, CartItem } from 'src/app/demo/service/online-cart.service';
import { CreateCustomerOrderDto } from 'src/app/demo/api/customer-order';
import { BranchContextService } from 'src/app/demo/service/branch-context.service';
import { BranchService } from 'src/app/demo/service/branch.service';
import { SessionService } from 'src/app/demo/service/session.service';
import { MessageService } from 'primeng/api';
import { environment } from 'src/environments/environment';

export interface CategorySection {
    category: string;
    items: any[];
}

@Component({
    selector: 'app-online-order',
    templateUrl: './online-order.component.html',
    styleUrls: ['./online-order.component.scss'],
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

    // FoodiesPakistan Curated Section Carousels
    trendingDeals: any[] = [];
    popularItems: any[] = [];
    categorySections: CategorySection[] = [];

    // View mode: 'carousel' (horizontal swipeable rows) or 'grid'
    viewMode: 'carousel' | 'grid' = 'carousel';

    // Search query
    searchKeyword = '';

    // Scroll state for sticky navbar shadow
    isScrolled = false;
    isMobile = false;

    // User authentication / profile state
    isLoggedIn = false;
    userName = 'Guest';
    userInitials = 'GU';

    // Foodies Pakistan Product Detail Modal / Bottom-Sheet
    detailModalVisible = false;
    selectedProduct: any | null = null;
    detailQuantity = 1;
    detailSelectedOption = 'Standard';
    detailOptions = ['Standard', 'Large / Deal Pack', 'Double Delight'];

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

    // Curated high quality imagery for Solar Panels, Inverters, Batteries and general retail
    private solarPanelImages = [
        'https://images.unsplash.com/photo-1509391365360-2e959784a276?auto=format&fit=crop&w=720&q=80',
        'https://images.unsplash.com/photo-1508514177221-188b1cf16e9d?auto=format&fit=crop&w=720&q=80',
        'https://images.unsplash.com/photo-1558449028-b53a39d100fc?auto=format&fit=crop&w=720&q=80',
        'https://images.unsplash.com/photo-1613665813446-82a78c468a1d?auto=format&fit=crop&w=720&q=80',
        'https://images.unsplash.com/photo-1545209584-6330a1334888?auto=format&fit=crop&w=720&q=80',
        'https://images.unsplash.com/photo-1497440001374-f26997328c1b?auto=format&fit=crop&w=720&q=80',
    ];

    private inverterImages = [
        'https://images.unsplash.com/photo-1581092160607-ee22621dd758?auto=format&fit=crop&w=720&q=80',
        'https://images.unsplash.com/photo-1581092335397-9583fe92d232?auto=format&fit=crop&w=720&q=80',
        'https://images.unsplash.com/photo-1581092580497-e0d23cbdf1dc?auto=format&fit=crop&w=720&q=80',
        'https://images.unsplash.com/photo-1581092162384-8987c1d64718?auto=format&fit=crop&w=720&q=80',
    ];

    private batteryImages = [
        'https://images.unsplash.com/photo-1617788138017-80ad40651399?auto=format&fit=crop&w=720&q=80',
        'https://images.unsplash.com/photo-1513836279014-a89f7a76ae86?auto=format&fit=crop&w=720&q=80',
        'https://images.unsplash.com/photo-1619642751034-765dfdf7c58e?auto=format&fit=crop&w=720&q=80',
        'https://images.unsplash.com/photo-1593941707882-a5bba14938c7?auto=format&fit=crop&w=720&q=80',
    ];

    private solarAccessoryImages = [
        'https://images.unsplash.com/photo-1581094794329-c8112a89af12?auto=format&fit=crop&w=720&q=80',
        'https://images.unsplash.com/photo-1473341304170-971dccb5ac1e?auto=format&fit=crop&w=720&q=80',
        'https://images.unsplash.com/photo-1466611653911-95081537e5b7?auto=format&fit=crop&w=720&q=80',
    ];

    private foodImages = [
        'https://images.unsplash.com/photo-1546069901-ba9599a7e63c?auto=format&fit=crop&w=720&q=80',
        'https://images.unsplash.com/photo-1565299624946-b28f40a0ae38?auto=format&fit=crop&w=720&q=80',
        'https://images.unsplash.com/photo-1568901346375-23c9450c58cd?auto=format&fit=crop&w=720&q=80',
        'https://images.unsplash.com/photo-1555396273-367ea4eb4db5?auto=format&fit=crop&w=720&q=80',
        'https://images.unsplash.com/photo-1565299585323-38d6b0865b47?auto=format&fit=crop&w=720&q=80',
    ];

    constructor(
        public formatService: AppFormatService,
        public cartService: OnlineCartService,
        private customerOrderService: CustomerOrderService,
        private branchContext: BranchContextService,
        private branchService: BranchService,
        private sessionService: SessionService,
        private route: ActivatedRoute,
        private router: Router,
        private messageService: MessageService
    ) {}

    ngOnInit(): void {
        this.checkScreenSize();
        this.checkAuthSession();

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

    @HostListener('window:resize')
    onResize(): void {
        this.checkScreenSize();
    }

    @HostListener('window:scroll')
    onWindowScroll(): void {
        this.isScrolled = window.scrollY > 15;
    }

    private checkScreenSize(): void {
        this.isMobile = window.innerWidth < 768;
    }

    private checkAuthSession(): void {
        this.sessionService
            .getCurrentLoginInformations()
            .then((res) => {
                if (res?.user) {
                    this.isLoggedIn = true;
                    this.userName = res.user.name || res.user.userName || 'Customer';
                    const parts = this.userName.trim().split(' ');
                    this.userInitials =
                        parts.length > 1
                            ? (parts[0][0] + parts[1][0]).toUpperCase()
                            : this.userName.substring(0, 2).toUpperCase();
                }
            })
            .catch(() => {
                this.isLoggedIn = false;
            });
    }

    async resolveBranchName(bId: number): Promise<void> {
        try {
            const info = await this.customerOrderService.getOnlineStoreHeader(bId);
            if (info) {
                if (info.branchName) this.branchName = info.branchName;
                if (info.currencyFormat) this.currencyFormat = info.currencyFormat;
                if (info.dateFormat) this.dateFormat = info.dateFormat;
                return;
            }
        } catch {
            // Ignore error
        }
        this.branchName = '';
    }

    async handleMissingBranchId(): Promise<void> {
        const ctxBranchId = this.branchContext.getBranchId();
        if (ctxBranchId) {
            this.setBranchInUrl(ctxBranchId);
            return;
        }

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
            // Fallback
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
            if (this.products && this.products.length > 0 && this.products[0].currencyFormat) {
                this.currencyFormat = this.products[0].currencyFormat;
            }
            this.extractCategories();
            this.organizeCarousels();
            this.applyFilter();

            if (selectProductId) {
                const target = this.products.find((p) => p.id === selectProductId);
                if (target) {
                    this.openProductModal(target);
                }
            }
        } catch (error: any) {
            this.messageService.add({
                severity: 'error',
                summary: 'Catalog Error',
                detail: error?.message || 'Failed to load catalog products',
            });
        } finally {
            this.loading = false;
        }
    }

    extractCategories(): void {
        const set = new Set<string>();
        this.products.forEach((p) => {
            if (p.categoryName && p.categoryName.trim()) {
                set.add(p.categoryName.trim());
            }
        });
        this.categories = ['All', '🔥 Deals', '✨ Popular', ...Array.from(set)];
    }

    private organizeCarousels(): void {
        // Trending Deals: top 6 items
        this.trendingDeals = this.products.slice(0, 8);
        // Popular: items 4 to 12
        this.popularItems = this.products.length > 4 ? this.products.slice(3, 11) : this.products;

        // Group by category for dedicated rows
        const catMap = new Map<string, any[]>();
        this.products.forEach((p) => {
            const cat = (p.categoryName && p.categoryName.trim()) || 'Featured Products';
            if (!catMap.has(cat)) {
                catMap.set(cat, []);
            }
            catMap.get(cat)!.push(p);
        });

        this.categorySections = [];
        catMap.forEach((items, category) => {
            this.categorySections.push({ category, items });
        });
    }

    selectCategory(cat: string): void {
        if (this.selectedCategory === cat && cat !== 'All') {
            this.selectedCategory = 'All';
        } else {
            this.selectedCategory = cat;
        }
        this.applyFilter();
    }

    clearFilter(): void {
        this.selectedCategory = 'All';
        this.searchKeyword = '';
        this.applyFilter();
    }

    applyFilter(): void {
        const query = (this.searchKeyword || '').toLowerCase().trim();
        const selectedCat = (this.selectedCategory || 'All').trim();
        const targetCatLower = selectedCat.toLowerCase();

        const dealIds = new Set((this.trendingDeals || []).map((d) => d.id));
        const popIds = new Set((this.popularItems || []).map((d) => d.id));

        this.filteredProducts = (this.products || []).filter((p) => {
            let matchesCat = true;
            if (selectedCat === 'All') {
                matchesCat = true;
            } else if (selectedCat === '🔥 Deals') {
                matchesCat = dealIds.has(p.id) || (p.discountPercent && p.discountPercent > 0);
            } else if (selectedCat === '✨ Popular') {
                matchesCat = popIds.has(p.id);
            } else {
                const pCatLower = (p.categoryName || '').trim().toLowerCase();
                matchesCat = pCatLower === targetCatLower;
            }

            const matchesSearch =
                !query ||
                (p.name || '').toLowerCase().includes(query) ||
                (p.categoryName || '').toLowerCase().includes(query) ||
                (p.brandName || '').toLowerCase().includes(query) ||
                (p.code || '').toLowerCase().includes(query);

            return matchesCat && matchesSearch;
        });
    }

    // Horizontal Scrolling Controls
    scrollFilterBar(direction: 'left' | 'right'): void {
        const container = document.getElementById('fpStoreFilterTrack');
        if (container) {
            const distance = direction === 'left' ? -220 : 220;
            container.scrollBy({ left: distance, behavior: 'smooth' });
        }
    }

    scrollCarousel(trackId: string, direction: 'left' | 'right'): void {
        const container = document.getElementById(trackId);
        if (container) {
            const distance = direction === 'left' ? -340 : 340;
            container.scrollBy({ left: distance, behavior: 'smooth' });
        }
    }

    // Contextual Domain Detection & Foodies Visual Helpers
    isSolarStore(): boolean {
        const b = (this.branchName || '').toLowerCase();
        return b.includes('solar') || b.includes('energy') || b.includes('power');
    }

    getProductDomain(product: any): 'solar-panel' | 'inverter' | 'battery' | 'solar-accessory' | 'food' | 'general' {
        const text = `${product?.name || ''} ${product?.categoryName || ''} ${product?.brandName || ''} ${product?.description || ''} ${this.branchName || ''}`.toLowerCase();

        // Batteries
        if (
            text.includes('battery') ||
            text.includes('batteries') ||
            text.includes('lithium') ||
            text.includes('tubular') ||
            text.includes('gel battery') ||
            text.includes('narada') ||
            text.includes('phoenix') ||
            text.includes('osaka') ||
            text.includes('daewoo') ||
            text.includes('volta')
        ) {
            return 'battery';
        }

        // Inverters
        if (
            text.includes('inverter') ||
            text.includes('ups') ||
            text.includes('inverex') ||
            text.includes('growatt') ||
            text.includes('fronus') ||
            text.includes('knox') ||
            text.includes('crown') ||
            text.includes('hybrid') ||
            text.includes('on-grid') ||
            text.includes('off-grid')
        ) {
            return 'inverter';
        }

        // Solar Panels
        if (
            text.includes('panel') ||
            text.includes('solar') ||
            text.includes('jinko') ||
            text.includes('longi') ||
            text.includes('canadian') ||
            text.includes('mono') ||
            text.includes('poly') ||
            text.includes('bifacial') ||
            text.includes('watt') ||
            text.includes('photovoltaic')
        ) {
            return 'solar-panel';
        }

        // Solar Accessories / Structures / Cables
        if (
            text.includes('cable') ||
            text.includes('wire') ||
            text.includes('breaker') ||
            text.includes('structure') ||
            text.includes('stand') ||
            text.includes('charge controller') ||
            text.includes('mc4') ||
            text.includes('db box')
        ) {
            return 'solar-accessory';
        }

        // Food / Dining
        if (
            text.includes('sweet') ||
            text.includes('mithai') ||
            text.includes('burger') ||
            text.includes('pizza') ||
            text.includes('cake') ||
            text.includes('food') ||
            text.includes('chicken') ||
            text.includes('tea') ||
            text.includes('bakery')
        ) {
            return 'food';
        }

        if (this.isSolarStore()) {
            return 'solar-panel';
        }

        return 'general';
    }

    getProductImage(product: any, index: number): string {
        if (product?.imagePath && !product.imagePath.includes('placeholder.png')) {
            return this.getImageUrl(product.imagePath);
        }
        const domain = this.getProductDomain(product);
        const idx = Math.abs((product?.id || index || 0));

        switch (domain) {
            case 'solar-panel':
                return this.solarPanelImages[idx % this.solarPanelImages.length];
            case 'inverter':
                return this.inverterImages[idx % this.inverterImages.length];
            case 'battery':
                return this.batteryImages[idx % this.batteryImages.length];
            case 'solar-accessory':
                return this.solarAccessoryImages[idx % this.solarAccessoryImages.length];
            case 'food':
                return this.foodImages[idx % this.foodImages.length];
            default:
                return this.isSolarStore()
                    ? this.solarPanelImages[idx % this.solarPanelImages.length]
                    : this.solarPanelImages[idx % this.solarPanelImages.length];
        }
    }

    getProductRating(product: any): string {
        const val = 4.7 + ((Math.abs(product?.id || 1) % 4) * 0.1);
        return val.toFixed(1);
    }

    getProductTag(product: any, index: number): string {
        const domain = this.getProductDomain(product);
        const idx = Math.abs(product?.id || index || 0);

        if (domain === 'solar-panel') {
            const tags = ['⭐ Tier-1 Module', '⚡ 25Y Warranty', '🔥 High Efficiency', '☀️ Bifacial Glass', '✨ A-Grade'];
            return tags[idx % tags.length];
        }
        if (domain === 'inverter') {
            const tags = ['⚡ Pure Sine Wave', '🔥 Smart MPPT', '⭐ 5Y Warranty', '📱 WiFi Monitored', '✨ Dual Output'];
            return tags[idx % tags.length];
        }
        if (domain === 'battery') {
            const tags = ['🔋 Deep Cycle', '⚡ Long Backup', '🔥 Fast Charging', '⭐ Heavy Duty', '✨ High Cycles'];
            return tags[idx % tags.length];
        }
        if (domain === 'solar-accessory') {
            const tags = ['⚡ UV Resistant', '⭐ Pure Copper', '🛡️ Surge Protected', '🔩 Rust Free'];
            return tags[idx % tags.length];
        }
        const tags = ['🔥 Hot Deal', '⭐ Best Seller', '🎉 Special Deal', '⚡ Verified'];
        return tags[idx % tags.length];
    }

    getProductDescription(product: any): string {
        if (product?.description && product.description.trim()) {
            return product.description;
        }
        const domain = this.getProductDomain(product);
        if (domain === 'solar-panel') {
            return 'High-efficiency Tier-1 Monocrystalline solar module engineered with advanced half-cut multi-busbar technology for maximum energy yield even in low-light and high-temperature environments. Certified with 25-year linear performance warranty.';
        }
        if (domain === 'inverter') {
            return 'Smart hybrid solar inverter featuring high-efficiency MPPT charge controller, pure sine wave AC output, zero-transfer time UPS functionality, and real-time WiFi mobile app monitoring.';
        }
        if (domain === 'battery') {
            return 'Heavy-duty deep-cycle energy storage battery built with thick reinforced plates for exceptional cycle life, deep discharge recovery, and consistent high-power backup.';
        }
        if (domain === 'solar-accessory') {
            return 'Certified high-grade solar infrastructure equipment designed for extreme outdoor weather conditions, electrical safety, and seamless system integration.';
        }
        return 'Authentic, premium-quality branded product with official manufacturer warranty and guaranteed durability.';
    }

    getDetailOptions(product: any): string[] {
        const domain = this.getProductDomain(product);
        if (domain === 'solar-panel') {
            return ['Single Panel', 'Pallet Pack (31 Panels)', 'Complete System Deal'];
        }
        if (domain === 'inverter') {
            return ['Standard Unit', 'With WiFi Monitoring Module', 'Complete Installation Kit'];
        }
        if (domain === 'battery') {
            return ['1x Battery Unit', '2x Battery Bank (24V)', '4x Battery Bank (48V)'];
        }
        if (domain === 'food') {
            return ['Standard Portion', 'Large / Family Pack', 'Double Delight'];
        }
        return ['Standard Unit', 'Commercial Pack', 'Complete Deal'];
    }

    getOriginalPrice(product: any): number {
        const price = product.price || 0;
        return Math.round(price * 1.15);
    }

    // FoodiesPakistan Product Detail Modal / Bottom Sheet
    openProductModal(product: any, event?: Event): void {
        if (event) {
            event.stopPropagation();
        }
        this.selectedProduct = product;
        this.detailQuantity = 1;
        this.detailOptions = this.getDetailOptions(product);
        this.detailSelectedOption = this.detailOptions[0];
        this.detailModalVisible = true;
    }

    closeProductModal(): void {
        this.detailModalVisible = false;
        this.selectedProduct = null;
    }

    updateDetailQuantity(change: number): void {
        const next = this.detailQuantity + change;
        if (next >= 1) {
            this.detailQuantity = next;
        }
    }

    getDetailTotalPrice(): number {
        if (!this.selectedProduct) return 0;
        let base = this.selectedProduct.price || 0;
        if (this.detailSelectedOption.includes('Pallet') || this.detailSelectedOption.includes('4x')) {
            base = base * 4;
        } else if (this.detailSelectedOption.includes('2x') || this.detailSelectedOption.includes('Installation') || this.detailSelectedOption.includes('Large')) {
            base = Math.round(base * 1.5);
        }
        return base * this.detailQuantity;
    }

    addDetailToCart(): void {
        if (!this.selectedProduct) return;
        this.addToCart(this.selectedProduct, this.detailQuantity);
        this.closeProductModal();
    }

    // Cart Actions
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

            // Show success confirmation
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

    getQuickSpecs(raw?: string): { key: string; value: string }[] {
        if (!raw) return [];
        try {
            const parsed = JSON.parse(raw);
            if (Array.isArray(parsed)) {
                return parsed
                    .map((item: any) => ({
                        key: item.key || item.name || '',
                        value: item.value || item.val || '',
                    }))
                    .filter((i) => i.key && i.value)
                    .slice(0, 3);
            } else if (typeof parsed === 'object') {
                return Object.keys(parsed)
                    .map((k) => ({
                        key: k,
                        value: String(parsed[k] ?? ''),
                    }))
                    .filter((i) => i.key && i.value)
                    .slice(0, 3);
            }
        } catch {
            return [];
        }
        return [];
    }
}
