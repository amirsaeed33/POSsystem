import { AppFormatService } from 'src/app/demo/service/app-format.service';
import { Component, HostListener, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { CustomerOrderService } from 'src/app/demo/service/customer-order.service';
import { DeliveryAreaService, DeliveryAreaDto } from 'src/app/demo/service/delivery-area.service';
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
    isRestaurant = false;
    availableBranches: any[] = [];
    selectedBranchIdForModal: number | null = null;
    mobileBranchModal = false;

    products: any[] = [];
    filteredProducts: any[] = [];
    categories: string[] = [];
    selectedCategory = 'All';

    // FoodiesPakistan Mobile Tab Bar & Filter Pills State
    activeMobileTab: 'home' | 'deals' | 'cart' | 'favorites' | 'account' = 'home';
    selectedPill = '';
    pillFilters: string[] = ['📍 Near Me', '🔥 Top Deals', '⭐ Highly Rated', '⚡ Fast Delivery', '🏷️ Budget Picks'];
    favorites: Set<number> = new Set<number>();
    mobileSearchOpen = false;

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
    deliveryAddress = '';
    deliveryLandmark = '';
    notes = '';
    submitting = false;

    // Delivery Location & Live GPS state
    locating = false;
    locationCaptured = false;
    latitude: number | null = null;
    longitude: number | null = null;
    googleMapsLink: string | null = null;
    locationAccuracy: number | null = null;
    locationError: string | null = null;

    // Branch GPS Coordinates & Distance Calculation State
    branchLatitude: number | null = null;
    branchLongitude: number | null = null;
    distanceKm: number | null = null;
    estimatedDeliveryMins: string | null = null;

    // Delivery Areas & Free Delivery Engine
    deliveryAreas: DeliveryAreaDto[] = [];
    selectedDeliveryAreaId: number | null = null;
    selectedDeliveryArea: DeliveryAreaDto | null = null;
    baseDeliveryFee = 100;
    freeDeliveryThreshold = 1500;

    // Success confirmation modal
    successModalVisible = false;
    confirmedOrderNo = '';
    confirmedTotal = 0;
    orderCopied = false;

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
        'https://images.unsplash.com/photo-1568901346375-23c9450c58cd?auto=format&fit=crop&w=720&q=80', // juicy burger
        'https://images.unsplash.com/photo-1565299624946-b28f40a0ae38?auto=format&fit=crop&w=720&q=80', // pizza
        'https://images.unsplash.com/photo-1621996346565-e3d5d628169e?auto=format&fit=crop&w=720&q=80', // creamy pasta
        'https://images.unsplash.com/photo-1563379091339-03b21ab4a4f8?auto=format&fit=crop&w=720&q=80', // biryani
        'https://images.unsplash.com/photo-1527477378698-4797598e5a66?auto=format&fit=crop&w=720&q=80', // spicy hot wings
        'https://images.unsplash.com/photo-1576107232684-1279f3908594?auto=format&fit=crop&w=720&q=80', // cheesy loaded fries
        'https://images.unsplash.com/photo-1544787219-7f47ccb76574?auto=format&fit=crop&w=720&q=80', // karak tea
        'https://images.unsplash.com/photo-1517701604599-bb29b565090c?auto=format&fit=crop&w=720&q=80', // iced coffee
        'https://images.unsplash.com/photo-1513558161293-cdaf765ed2fd?auto=format&fit=crop&w=720&q=80', // mint margarita
    ];

    private retailImages = [
        'https://images.unsplash.com/photo-1622483767028-3f66f32aef97?auto=format&fit=crop&w=720&q=80', // cold beverage / cola
        'https://images.unsplash.com/photo-1550989460-0adc9554f529?auto=format&fit=crop&w=720&q=80', // supermarket shelf
        'https://images.unsplash.com/photo-1527842891421-42eec6e703ea?auto=format&fit=crop&w=720&q=80', // tea & coffee packet
        'https://images.unsplash.com/photo-1563729784474-d77dbb933a9e?auto=format&fit=crop&w=720&q=80', // cookies / snacks
        'https://images.unsplash.com/photo-1584308666744-24d5c474f2ae?auto=format&fit=crop&w=720&q=80', // personal care
        'https://images.unsplash.com/photo-1542838132-92c53300491e?auto=format&fit=crop&w=720&q=80', // grocery store
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
        private messageService: MessageService,
        private deliveryAreaService: DeliveryAreaService
    ) {}

    ngOnInit(): void {
        this.checkScreenSize();
        this.checkAuthSession();
        this.loadFavorites();

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
                this.isRestaurant = !!info.isRestaurant || info.businessType === 2;
                if (info.latitude != null) this.branchLatitude = Number(info.latitude);
                if (info.longitude != null) this.branchLongitude = Number(info.longitude);
                if (this.locationCaptured) {
                    this.calculateDistance();
                }
                return;
            }
        } catch {
            // Ignore error
        }
        this.branchName = '';
        this.isRestaurant = false;
        this.branchLatitude = null;
        this.branchLongitude = null;
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
            if (bId) {
                this.loadDeliveryAreas(bId);
            }
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
        if (this.activeMobileTab === 'favorites') {
            this.activeMobileTab = 'home';
        }
        this.applyFilter();
        setTimeout(() => {
            const activeChip = document.querySelector('.fp-cat-chip.active, .fp-circle-cat-item.active');
            if (activeChip) {
                activeChip.scrollIntoView({ behavior: 'smooth', inline: 'center', block: 'nearest' });
            }
        }, 60);
    }

    selectPill(pill: string): void {
        if (this.selectedPill === pill) {
            this.selectedPill = '';
        } else {
            this.selectedPill = pill;
        }
        if (this.activeMobileTab === 'favorites') {
            this.activeMobileTab = 'home';
        }
        this.applyFilter();
        setTimeout(() => {
            const activePill = document.querySelector('.fp-pill-badge.active');
            if (activePill) {
                activePill.scrollIntoView({ behavior: 'smooth', inline: 'center', block: 'nearest' });
            }
        }, 60);
    }

    clearFilter(): void {
        this.selectedCategory = 'All';
        this.selectedPill = '';
        this.searchKeyword = '';
        if (this.activeMobileTab === 'favorites') {
            this.activeMobileTab = 'home';
        }
        this.applyFilter();
    }

    toggleMobileSearch(): void {
        this.mobileSearchOpen = !this.mobileSearchOpen;
        if (this.mobileSearchOpen) {
            setTimeout(() => {
                const input = document.getElementById('fpMobileSearchInput') as HTMLInputElement;
                if (input) {
                    input.focus();
                }
            }, 100);
        }
    }

    applyFilter(): void {
        const query = (this.searchKeyword || '').toLowerCase().trim();
        const selectedCat = (this.selectedCategory || 'All').trim();
        const targetCatLower = selectedCat.toLowerCase();
        const pill = this.selectedPill;

        const dealIds = new Set((this.trendingDeals || []).map((d) => d.id));
        const popIds = new Set((this.popularItems || []).map((d) => d.id));

        this.filteredProducts = (this.products || []).filter((p) => {
            // Favorites tab restriction
            if (this.activeMobileTab === 'favorites') {
                if (!this.favorites.has(p.id)) {
                    return false;
                }
            }

            // Category filtering
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

            // Pill filtering (Foodies Pakistan pills)
            let matchesPill = true;
            if (pill === '🔥 Top Deals') {
                matchesPill = dealIds.has(p.id) || (p.discountPercent && p.discountPercent > 0);
            } else if (pill === '⭐ Highly Rated') {
                const rating = parseFloat(this.getProductRating(p));
                matchesPill = rating >= 4.8;
            } else if (pill === '🏷️ Budget Picks') {
                matchesPill = !p.price || p.price <= 25000;
            } else if (pill === '⚡ Fast Delivery') {
                matchesPill = true;
            } else if (pill === '📍 Near Me') {
                matchesPill = true;
            }

            // Keyword search
            const matchesSearch =
                !query ||
                (p.name || '').toLowerCase().includes(query) ||
                (p.categoryName || '').toLowerCase().includes(query) ||
                (p.brandName || '').toLowerCase().includes(query) ||
                (p.code || '').toLowerCase().includes(query);

            return matchesCat && matchesPill && matchesSearch;
        });
    }

    // 5-Tab Navigation Dock Actions
    onMobileTabSelect(tab: 'home' | 'deals' | 'cart' | 'favorites' | 'account'): void {
        this.activeMobileTab = tab;
        if (tab === 'home') {
            this.selectedCategory = 'All';
            this.selectedPill = '';
            this.searchKeyword = '';
            this.applyFilter();
            window.scrollTo({ top: 0, behavior: 'smooth' });
        } else if (tab === 'deals') {
            this.selectedCategory = '🔥 Deals';
            this.selectedPill = '🔥 Top Deals';
            this.applyFilter();
            window.scrollTo({ top: 0, behavior: 'smooth' });
        } else if (tab === 'cart') {
            this.cartService.sidebarVisible = true;
        } else if (tab === 'favorites') {
            this.selectedCategory = 'All';
            this.selectedPill = '';
            this.applyFilter();
            window.scrollTo({ top: 0, behavior: 'smooth' });
        } else if (tab === 'account') {
            if (this.isLoggedIn) {
                this.messageService.add({
                    severity: 'info',
                    summary: 'Profile',
                    detail: `Signed in as ${this.userName}`,
                    life: 2500,
                });
            } else {
                this.router.navigate(['/auth/login']);
            }
        }
    }

    // Favorites persistence & toggling
    loadFavorites(): void {
        try {
            const raw = localStorage.getItem('smartpos_store_favorites');
            if (raw) {
                const arr = JSON.parse(raw);
                if (Array.isArray(arr)) {
                    this.favorites = new Set(arr);
                }
            }
        } catch {}
    }

    saveFavorites(): void {
        try {
            localStorage.setItem('smartpos_store_favorites', JSON.stringify(Array.from(this.favorites)));
        } catch {}
    }

    toggleFavorite(productId: number, event?: Event): void {
        if (event) {
            event.stopPropagation();
        }
        if (this.favorites.has(productId)) {
            this.favorites.delete(productId);
            this.messageService.add({
                severity: 'info',
                summary: 'Removed',
                detail: 'Item removed from favorites',
                life: 1400,
            });
        } else {
            this.favorites.add(productId);
            this.messageService.add({
                severity: 'success',
                summary: 'Saved',
                detail: 'Item added to your favorites',
                life: 1400,
            });
        }
        this.saveFavorites();
        if (this.activeMobileTab === 'favorites') {
            this.applyFilter();
        }
    }

    isFavorite(productId: number): boolean {
        return this.favorites.has(productId);
    }

    get favoriteCount(): number {
        return this.favorites.size;
    }

    // Circular Category Visual Resolver
    getCategoryVisual(cat: string): { icon: string; image?: string; color: string; label: string } {
        const c = cat.toLowerCase();
        if (c === 'all') {
            return { icon: 'pi pi-compass', color: '#e23744', label: 'All' };
        }
        if (c.includes('deal') || c.includes('🔥')) {
            return {
                icon: 'pi pi-bolt',
                color: '#ef4444',
                label: 'Deals',
                image: 'https://images.unsplash.com/photo-1546069901-ba9599a7e63c?auto=format&fit=crop&w=150&q=80',
            };
        }
        if (c.includes('popular') || c.includes('✨') || c.includes('top')) {
            return {
                icon: 'pi pi-star-fill',
                color: '#f59e0b',
                label: 'Popular',
                image: 'https://images.unsplash.com/photo-1568901346375-23c9450c58cd?auto=format&fit=crop&w=150&q=80',
            };
        }
        if (c.includes('burger') || c.includes('fast')) {
            return {
                icon: 'pi pi-check',
                color: '#f97316',
                label: cat,
                image: 'https://images.unsplash.com/photo-1568901346375-23c9450c58cd?auto=format&fit=crop&w=150&q=80',
            };
        }
        if (c.includes('pizza')) {
            return {
                icon: 'pi pi-circle',
                color: '#ea580c',
                label: cat,
                image: 'https://images.unsplash.com/photo-1565299624946-b28f40a0ae38?auto=format&fit=crop&w=150&q=80',
            };
        }
        if (c.includes('desi') || c.includes('karahi') || c.includes('handi') || c.includes('biryani')) {
            return {
                icon: 'pi pi-sun',
                color: '#b91c1c',
                label: cat,
                image: 'https://images.unsplash.com/photo-1546833999-b9f581a1996d?auto=format&fit=crop&w=150&q=80',
            };
        }
        if (c.includes('chinese') || c.includes('soup') || c.includes('noodle')) {
            return {
                icon: 'pi pi-th-large',
                color: '#dc2626',
                label: cat,
                image: 'https://images.unsplash.com/photo-1552611052-33e04de081de?auto=format&fit=crop&w=150&q=80',
            };
        }
        if (c.includes('cafe') || c.includes('coffee') || c.includes('tea') || c.includes('drink')) {
            return {
                icon: 'pi pi-heart',
                color: '#78350f',
                label: cat,
                image: 'https://images.unsplash.com/photo-1509042239860-f550ce710b93?auto=format&fit=crop&w=150&q=80',
            };
        }
        if (c.includes('sweet') || c.includes('mithai') || c.includes('cake') || c.includes('bakery')) {
            return {
                icon: 'pi pi-heart-fill',
                color: '#db2777',
                label: cat,
                image: 'https://images.unsplash.com/photo-1578985545062-69928b1d9587?auto=format&fit=crop&w=150&q=80',
            };
        }
        if (c.includes('panel') || c.includes('solar') || c.includes('mono') || c.includes('bifacial')) {
            return {
                icon: 'pi pi-sun',
                color: '#2563eb',
                label: cat,
                image: 'https://images.unsplash.com/photo-1509391365360-2e959784a276?auto=format&fit=crop&w=150&q=80',
            };
        }
        if (c.includes('inverter') || c.includes('ups') || c.includes('hybrid')) {
            return {
                icon: 'pi pi-bolt',
                color: '#059669',
                label: cat,
                image: 'https://images.unsplash.com/photo-1581092160607-ee22621dd758?auto=format&fit=crop&w=150&q=80',
            };
        }
        if (c.includes('battery') || c.includes('lithium') || c.includes('tubular')) {
            return {
                icon: 'pi pi-box',
                color: '#7c3aed',
                label: cat,
                image: 'https://images.unsplash.com/photo-1617788138017-80ad40651399?auto=format&fit=crop&w=150&q=80',
            };
        }
        if (c.includes('accessory') || c.includes('cable') || c.includes('wire') || c.includes('structure')) {
            return {
                icon: 'pi pi-cog',
                color: '#4b5563',
                label: cat,
                image: 'https://images.unsplash.com/photo-1581094794329-c8112a89af12?auto=format&fit=crop&w=150&q=80',
            };
        }
        return {
            icon: 'pi pi-tag',
            color: '#e23744',
            label: cat,
            image: 'https://images.unsplash.com/photo-1546069901-ba9599a7e63c?auto=format&fit=crop&w=150&q=80',
        };
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

    isRestaurantStore(): boolean {
        if (this.isRestaurant) {
            return true;
        }
        const b = (this.branchName || '').toLowerCase();
        return (
            b.includes('cafe') ||
            b.includes('restaurant') ||
            b.includes('food') ||
            b.includes('burger') ||
            b.includes('pizza') ||
            b.includes('dining') ||
            b.includes('kitchen') ||
            b.includes('bite') ||
            b.includes('bbq') ||
            b.includes('fast food') ||
            b.includes('dhaba') ||
            b.includes('hotel')
        );
    }

    getProductDomain(product: any): 'solar-panel' | 'inverter' | 'battery' | 'solar-accessory' | 'food' | 'retail' | 'general' {
        const text = `${product?.name || ''} ${product?.categoryName || ''} ${product?.brandName || ''} ${product?.description || ''} ${this.branchName || ''}`.toLowerCase();

        // Food & Beverages (Burgers, Pizzas, Pasta, Biryani, Wings, Fries, Karak Chai, Coffee, Drinks)
        if (
            text.includes('burger') ||
            text.includes('pizza') ||
            text.includes('pasta') ||
            text.includes('biryani') ||
            text.includes('tikka') ||
            text.includes('wings') ||
            text.includes('fries') ||
            text.includes('chai') ||
            text.includes('tea') ||
            text.includes('coffee') ||
            text.includes('latte') ||
            text.includes('margarita') ||
            text.includes('sweet') ||
            text.includes('mithai') ||
            text.includes('food') ||
            text.includes('fast food') ||
            text.includes('bakery') ||
            text.includes('dessert') ||
            text.includes('chicken')
        ) {
            return 'food';
        }

        // Retail & Supermarket items (Cold drinks, Mineral water, Packaged tea, Milk, Ketchup, Chips, Cookies, Soaps, Shampoo, Detergent)
        if (
            text.includes('coke') ||
            text.includes('cola') ||
            text.includes('pepsi') ||
            text.includes('nestle') ||
            text.includes('pure life') ||
            text.includes('mineral water') ||
            text.includes('water bottle') ||
            text.includes('lipton') ||
            text.includes('olper') ||
            text.includes('milk') ||
            text.includes('ketchup') ||
            text.includes('lays') ||
            text.includes('chips') ||
            text.includes('oreo') ||
            text.includes('biscuit') ||
            text.includes('cookies') ||
            text.includes('dettol') ||
            text.includes('soap') ||
            text.includes('shampoo') ||
            text.includes('ariel') ||
            text.includes('detergent') ||
            text.includes('grocery')
        ) {
            return 'retail';
        }

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

        if (this.isRestaurantStore()) {
            return 'food';
        }

        if (this.isSolarStore()) {
            return 'solar-panel';
        }

        return 'retail';
    }

    getProductImage(product: any, index: number): string {
        if (product?.imagePath && !product.imagePath.includes('placeholder.png')) {
            return this.getImageUrl(product.imagePath);
        }
        const domain = this.getProductDomain(product);
        const idx = Math.abs(product?.id || index || 0);

        switch (domain) {
            case 'food':
                return this.foodImages[idx % this.foodImages.length];
            case 'retail':
                return this.retailImages[idx % this.retailImages.length];
            case 'solar-panel':
                return this.solarPanelImages[idx % this.solarPanelImages.length];
            case 'inverter':
                return this.inverterImages[idx % this.inverterImages.length];
            case 'battery':
                return this.batteryImages[idx % this.batteryImages.length];
            case 'solar-accessory':
                return this.solarAccessoryImages[idx % this.solarAccessoryImages.length];
            default:
                if (this.isRestaurantStore()) {
                    return this.foodImages[idx % this.foodImages.length];
                }
                if (this.isSolarStore()) {
                    return this.solarPanelImages[idx % this.solarPanelImages.length];
                }
                return this.retailImages[idx % this.retailImages.length];
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

    captureCurrentLocation(): void {
        if (!navigator.geolocation) {
            this.locationError = 'Geolocation is not supported by your browser.';
            this.messageService.add({
                severity: 'error',
                summary: 'GPS Not Supported',
                detail: 'Geolocation is not supported by your browser. Please enter your address manually.',
            });
            return;
        }

        this.locating = true;
        this.locationError = null;

        navigator.geolocation.getCurrentPosition(
            (pos) => {
                this.latitude = Number(pos.coords.latitude.toFixed(6));
                this.longitude = Number(pos.coords.longitude.toFixed(6));
                this.locationAccuracy = Math.round(pos.coords.accuracy);
                this.googleMapsLink = `https://maps.google.com/?q=${this.latitude},${this.longitude}`;
                this.locationCaptured = true;
                this.locating = false;

                this.messageService.add({
                    severity: 'success',
                    summary: 'Location Captured',
                    detail: '✓ Live GPS location captured successfully',
                    life: 2500,
                });

                this.reverseGeocode(this.latitude, this.longitude);
                this.calculateDistance();
            },
            (err) => {
                this.locating = false;
                let msg = 'Location access denied. Please enter your address manually.';
                if (err.code === err.TIMEOUT) {
                    msg = 'GPS signal timed out. Please enter your address manually.';
                } else if (err.code === err.POSITION_UNAVAILABLE) {
                    msg = 'GPS position unavailable. Please enter your address manually.';
                }
                this.locationError = msg;
                this.messageService.add({
                    severity: 'warn',
                    summary: 'GPS Location',
                    detail: msg,
                    life: 4000,
                });
            },
            {
                enableHighAccuracy: true,
                timeout: 10000,
                maximumAge: 0,
            }
        );
    }

    clearLocation(): void {
        this.locationCaptured = false;
        this.latitude = null;
        this.longitude = null;
        this.googleMapsLink = null;
        this.locationAccuracy = null;
        this.locationError = null;
        this.distanceKm = null;
        this.estimatedDeliveryMins = null;
    }

    calculateDistance(): void {
        if (
            this.latitude != null &&
            this.longitude != null &&
            this.branchLatitude != null &&
            this.branchLongitude != null
        ) {
            const R = 6371; // Earth radius in km
            const lat1 = (this.branchLatitude * Math.PI) / 180;
            const lat2 = (this.latitude * Math.PI) / 180;
            const dLat = ((this.latitude - this.branchLatitude) * Math.PI) / 180;
            const dLon = ((this.longitude - this.branchLongitude) * Math.PI) / 180;

            const a =
                Math.sin(dLat / 2) * Math.sin(dLat / 2) +
                Math.cos(lat1) * Math.cos(lat2) * Math.sin(dLon / 2) * Math.sin(dLon / 2);
            const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
            const km = R * c;

            this.distanceKm = Number(km.toFixed(1));

            if (this.distanceKm <= 3) {
                this.estimatedDeliveryMins = '20 - 30 mins';
            } else if (this.distanceKm <= 7) {
                this.estimatedDeliveryMins = '30 - 45 mins';
            } else if (this.distanceKm <= 15) {
                this.estimatedDeliveryMins = '45 - 60 mins';
            } else {
                this.estimatedDeliveryMins = '60 - 90 mins';
            }
        } else {
            this.distanceKm = null;
            this.estimatedDeliveryMins = null;
        }
    }

    get routeDirectionsLink(): string | null {
        if (
            this.latitude != null &&
            this.longitude != null &&
            this.branchLatitude != null &&
            this.branchLongitude != null
        ) {
            return `https://www.google.com/maps/dir/?api=1&origin=${this.branchLatitude},${this.branchLongitude}&destination=${this.latitude},${this.longitude}`;
        }
        return this.googleMapsLink;
    }

    private async reverseGeocode(lat: number, lng: number): Promise<void> {
        try {
            const res = await fetch(`https://nominatim.openstreetmap.org/reverse?format=json&lat=${lat}&lon=${lng}&zoom=18&addressdetails=1`, {
                headers: { Accept: 'application/json' },
            });
            if (res.ok) {
                const data = await res.json();
                if (data?.display_name && !this.deliveryAddress.trim()) {
                    const parts = data.display_name.split(',').slice(0, 3).join(', ');
                    this.deliveryAddress = parts.trim();
                }
            }
        } catch {
            // Best effort reverse geocode
        }
    }

    async loadDeliveryAreas(bId: number): Promise<void> {
        try {
            const [areas, header] = await Promise.all([
                this.deliveryAreaService.getActiveAreasForBranch(bId),
                this.customerOrderService.getOnlineStoreHeader(bId),
            ]);
            this.deliveryAreas = areas || [];
            if (header) {
                this.baseDeliveryFee = header.baseDeliveryFee ?? 100;
                this.freeDeliveryThreshold = header.freeDeliveryThreshold ?? 1500;
            }
            if (this.deliveryAreas.length > 0 && !this.selectedDeliveryAreaId) {
                this.selectedDeliveryAreaId = this.deliveryAreas[0].id;
                this.selectedDeliveryArea = this.deliveryAreas[0];
            }
        } catch (e) {
            console.error('Failed to load delivery areas/header info', e);
        }
    }

    getDeliveryFee(): number {
        if (this.isFreeDeliveryEligible()) {
            return 0;
        }
        if (this.selectedDeliveryArea) {
            return this.selectedDeliveryArea.deliveryFee ?? 0;
        }
        return this.deliveryAreas.length > 0 ? this.deliveryAreas[0].deliveryFee : this.baseDeliveryFee;
    }

    isFreeDeliveryEligible(): boolean {
        const subtotal = this.cartService.cartSubtotal;
        if (subtotal >= this.freeDeliveryThreshold) {
            return true;
        }
        if (this.cartService.items.some((item) => {
            const product = this.products.find((p) => p.id === item.productId);
            return (product as any)?.isFreeDelivery;
        })) {
            return true;
        }
        return false;
    }

    getGrandTotal(): number {
        return Math.max(0, this.cartService.cartSubtotal + this.getDeliveryFee());
    }

    onDeliveryAreaChange(areaId: number): void {
        this.selectedDeliveryAreaId = areaId;
        this.selectedDeliveryArea = this.deliveryAreas.find((a) => a.id === areaId) || null;
    }

    isMinOrderSatisfied(): boolean {
        if (!this.selectedDeliveryArea || !this.selectedDeliveryArea.minimumOrderAmount) {
            return true;
        }
        return this.cartService.cartSubtotal >= this.selectedDeliveryArea.minimumOrderAmount;
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

            // Compile delivery address, landmarks, GPS, and special instructions into formatted notes for POS
            const addressParts: string[] = [];
            if (this.deliveryAddress.trim()) {
                addressParts.push(`📍 Addr: ${this.deliveryAddress.trim()}`);
            }
            if (this.deliveryLandmark.trim()) {
                addressParts.push(`🏛️ Landmark: ${this.deliveryLandmark.trim()}`);
            }
            if (this.googleMapsLink) {
                addressParts.push(`🗺️ Maps: ${this.googleMapsLink}`);
            }
            if (this.distanceKm != null) {
                addressParts.push(`📏 Distance: ${this.distanceKm} km from store`);
            }
            if (this.notes.trim()) {
                addressParts.push(`📝 Notes: ${this.notes.trim()}`);
            }

            const formattedNotes = addressParts.length > 0 ? addressParts.join(' | ').substring(0, 510) : this.notes.trim();

            const input: CreateCustomerOrderDto = {
                customerName: this.customerName.trim(),
                customerMobile: this.customerMobile.trim(),
                branchId: this.branchId || undefined,
                notes: formattedNotes,
                latitude: this.latitude ?? undefined,
                longitude: this.longitude ?? undefined,
                distanceKm: this.distanceKm ?? undefined,
                googleMapsLink: this.googleMapsLink ?? undefined,
                google_maps_link: this.googleMapsLink ?? undefined,
                deliveryAddress: this.deliveryAddress.trim() || undefined,
                deliveryLandmark: this.deliveryLandmark.trim() || undefined,
                deliveryAreaId: this.selectedDeliveryAreaId ?? (this.selectedDeliveryArea?.id || undefined),
                lines: this.cartService.items.map((c) => ({
                    productId: c.productId,
                    quantity: c.quantity,
                    unitPrice: c.unitPrice,
                })),
            };

            const createdOrder = await this.customerOrderService.createOnlineOrder(input);

            this.confirmedOrderNo = createdOrder.orderNo || `ORD-${createdOrder.id}`;
            this.confirmedTotal = createdOrder.totalAmount;

            // Reset cart & checkout modal state
            this.cartService.clearCart();
            this.customerName = '';
            this.customerMobile = '';
            this.deliveryAddress = '';
            this.deliveryLandmark = '';
            this.notes = '';
            this.clearLocation();
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

    copyOrderNo(orderNo: string): void {
        if (!orderNo) return;
        if (navigator?.clipboard?.writeText) {
            navigator.clipboard.writeText(orderNo).then(() => {
                this.orderCopied = true;
                this.messageService?.add({
                    severity: 'success',
                    summary: 'Copied',
                    detail: `Order number ${orderNo} copied to clipboard!`,
                    life: 2500,
                });
                setTimeout(() => {
                    this.orderCopied = false;
                }, 2500);
            }).catch(() => {
                this.fallbackCopy(orderNo);
            });
        } else {
            this.fallbackCopy(orderNo);
        }
    }

    private fallbackCopy(text: string): void {
        try {
            const textArea = document.createElement('textarea');
            textArea.value = text;
            textArea.style.position = 'fixed';
            textArea.style.left = '-999999px';
            document.body.appendChild(textArea);
            textArea.focus();
            textArea.select();
            document.execCommand('copy');
            document.body.removeChild(textArea);
            this.orderCopied = true;
            this.messageService?.add({
                severity: 'success',
                summary: 'Copied',
                detail: `Order number ${text} copied to clipboard!`,
                life: 2500,
            });
            setTimeout(() => {
                this.orderCopied = false;
            }, 2500);
        } catch {
            // Ignore
        }
    }

    goToTracking(orderNo?: string): void {
        this.successModalVisible = false;
        if (orderNo) {
            this.router.navigate(['/online-order/track', orderNo], {
                queryParams: this.branchId ? { branchId: this.branchId } : {}
            });
        } else {
            this.router.navigate(['/online-order/track'], {
                queryParams: this.branchId ? { branchId: this.branchId } : {}
            });
        }
    }
}
