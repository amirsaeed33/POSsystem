import { AppFormatService } from 'src/app/demo/service/app-format.service';
import { Component, HostListener, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { CustomerOrderService } from 'src/app/demo/service/customer-order.service';
import { DeliveryAreaService, DeliveryAreaDto } from 'src/app/demo/service/delivery-area.service';
import { OnlineCartService, CartItem } from 'src/app/demo/service/online-cart.service';
import { CreateCustomerOrderDto } from 'src/app/demo/api/customer-order';
import { BranchContextService } from 'src/app/demo/service/branch-context.service';
import { BranchService } from 'src/app/demo/service/branch.service';
import { SessionService } from 'src/app/demo/service/session.service';
import { OnlineOrderHeroSlideService } from 'src/app/demo/service/online-order-hero-slide.service';
import { ProductModifierService } from 'src/app/demo/service/product-modifier.service';
import { ProductModifierGroupDto, ProductModifierOptionDto } from 'src/app/demo/api/product-modifier';
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
export class OnlineOrderComponent implements OnInit, OnDestroy {
    branchId: number | null = null;
    branchName = '';
    branchAddress = '';
    branchPhone = '';
    facebookUrl = '';
    instagramUrl = '';
    youtubeUrl = '';
    currencyFormat = 'PKR';
    dateFormat = 'dd/MM/yyyy';
    branchMissingError = false;
    isRestaurant = false;
    availableBranches: any[] = [];
    selectedBranchIdForModal: number | null = null;
    mobileBranchModal = false;
    mobileNavOpen = false;
    contactModalVisible = false;
    activeNav: 'home' | 'menu' | 'deals' | 'contact' = 'home';

    products: any[] = [];
    filteredProducts: any[] = [];
    categories: string[] = [];
    backendCategories: string[] = [];
    selectedCategory = 'All';

    get heroSpotlightItem(): any {
        if (this.dealsList && this.dealsList.length > 0) {
            return this.dealsList[0];
        }
        return (this.products && this.products.length > 0) ? this.products[0] : null;
    }

    // FoodiesPakistan Mobile Tab Bar & Filter Pills State
    activeMobileTab: 'home' | 'deals' | 'cart' | 'favorites' | 'account' = 'home';
    selectedPill = '';
    pillFilters: string[] = ['📍 Near Me', '🔥 Top Deals', '⭐ Highly Rated', '⚡ Fast Delivery', '🏷️ Budget Picks'];
    favorites: Set<number> = new Set<number>();
    mobileSearchOpen = false;

    // Curated Section Carousels & Deals
    dealsList: any[] = [];
    trendingDeals: any[] = [];
    popularItems: any[] = [];
    categorySections: CategorySection[] = [];

    // View mode: 'grid' (responsive multi-column wrapped grid)
    viewMode: 'grid' | 'carousel' = 'grid';

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
    productModifierGroups: ProductModifierGroupDto[] = [];
    selectedModifierOptions: Record<number, number[]> = {};
    loadingModifiers = false;

    // Store Chat Floating Drawer
    storeChatOpen = false;

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
        private deliveryAreaService: DeliveryAreaService,
        private onlineOrderHeroSlideService: OnlineOrderHeroSlideService,
        private productModifierService: ProductModifierService
    ) {}

    currentHeroSlide = 0;
    private heroSlideTimer: any = null;

    heroSlides: any[] = [];

    private readonly defaultHeroSlides = [
        {
            id: 1,
            badge: 'FRESH FROM OVEN • 30-45 MIN DELIVERY',
            badgeIcon: '🔥',
            titleMain: 'Craving Something',
            titleHighlight: 'Hot, Cheesy',
            titleEnd: '& Delicious?',
            description: 'Explore handcrafted pizzas loaded with 100% mozzarella, sizzling juicy burgers, crispy wings, and mouth-watering family deals. Cooked fresh on order!',
            offerPill: 'FLAT 25% OFF ON LARGE PIZZAS',
            primaryBtnText: 'Order Deals Now',
            primaryTarget: 'deals',
            secondaryBtnText: 'Explore Full Menu',
            secondaryTarget: 'menu',
            imageUrl: 'https://images.unsplash.com/photo-1513104890138-7c749659a591?auto=format&fit=crop&w=800&h=600&q=80',
            bgImageUrl: 'https://images.unsplash.com/photo-1565299624946-b28f40a0ae38?auto=format&fit=crop&w=1920&q=85',
            cardTag: '🔥 CROWN CRUST PIZZA',
            cardTitle: 'Cheesy Crown Supreme Pizza',
            cardPrice: 1650,
            cardOriginalPrice: 2199,
            rating: '4.9★'
        },
        {
            id: 2,
            badge: 'SMASHED & GRILLED TO PERFECTION',
            badgeIcon: '🍔',
            titleMain: 'Bite Into Pure',
            titleHighlight: 'Juicy & Crispy',
            titleEnd: 'Gourmet Burgers',
            description: 'Double smashed beef patties, crispy tender chicken fillets, secret spicy mayo, and melted golden cheddar packed inside freshly toasted brioche buns.',
            offerPill: 'FREE CRISPY FRIES + DIP WITH COMBOS',
            primaryBtnText: 'Grab Burger Deal',
            primaryTarget: 'deals',
            secondaryBtnText: 'View All Burgers',
            secondaryTarget: 'menu',
            imageUrl: 'https://images.unsplash.com/photo-1568901346375-23c9450c58cd?auto=format&fit=crop&w=800&h=600&q=80',
            bgImageUrl: 'https://images.unsplash.com/photo-1572802419224-296b0aeee0d9?auto=format&fit=crop&w=1920&q=85',
            cardTag: '⭐ BEST SELLER',
            cardTitle: 'Double Smashed Monster Burger',
            cardPrice: 890,
            cardOriginalPrice: 1150,
            rating: '4.8★'
        },
        {
            id: 3,
            badge: 'SMOKY CHARCOAL & AUTHENTIC TASTE',
            badgeIcon: '🍢',
            titleMain: 'Sizzling Live',
            titleHighlight: 'Smoky BBQ',
            titleEnd: '& Kebab Platters',
            description: 'Tender chicken boti, melt-in-mouth seekh kebabs, and grilled reshmi tikka marinated in rich spices and served with piping-hot kulchas and mint raita.',
            offerPill: 'FAMILY PLATTER SPECIAL SAVE PKR 800',
            primaryBtnText: 'Taste The BBQ',
            primaryTarget: 'menu',
            secondaryBtnText: 'View BBQ Platters',
            secondaryTarget: 'menu',
            imageUrl: 'https://images.unsplash.com/photo-1555939594-58d7cb561ad1?auto=format&fit=crop&w=800&h=600&q=80',
            bgImageUrl: 'https://images.unsplash.com/photo-1555939594-58d7cb561ad1?auto=format&fit=crop&w=1920&q=85',
            cardTag: '👑 SIGNATURE SPECIAL',
            cardTitle: 'Royal Charcoal Mixed BBQ Platter',
            cardPrice: 2450,
            cardOriginalPrice: 3250,
            rating: '4.9★'
        },
        {
            id: 4,
            badge: 'MEGA VALUE PACKS & CELEBRATIONS',
            badgeIcon: '🎉',
            titleMain: 'Gather Round For',
            titleHighlight: 'Mega Family',
            titleEnd: 'Feast Combos',
            description: 'The ultimate party solution! 2 Large Specialty Pizzas, 10 pcs Crispy Wings, Cheesy Garlic Bread, and a chilled 1.5L drink for the whole squad.',
            offerPill: 'MEGA SAVINGS UP TO PKR 1,200',
            primaryBtnText: 'Get Family Feast',
            primaryTarget: 'deals',
            secondaryBtnText: 'See All Combos',
            secondaryTarget: 'deals',
            imageUrl: 'https://images.unsplash.com/photo-1544025162-d76694265947?auto=format&fit=crop&w=800&h=600&q=80',
            bgImageUrl: 'https://images.unsplash.com/photo-1626082927389-6cd097cdc6ec?auto=format&fit=crop&w=1920&q=85',
            cardTag: '⚡ JUMBO COMBO',
            cardTitle: 'Ultimate Family Feast Box',
            cardPrice: 3499,
            cardOriginalPrice: 4699,
            rating: '5.0★'
        },
        {
            id: 5,
            badge: 'PIPING HOT DELIVERY GUARANTEE',
            badgeIcon: '⚡',
            titleMain: 'Delivered In',
            titleHighlight: '30-45 Mins',
            titleEnd: 'Right To Your Door',
            description: 'Experience lightning-fast express delivery with insulated heat bags. Live rider GPS tracking keeps you informed every step of the way from grill to table.',
            offerPill: 'FREE EXPRESS DELIVERY TODAY',
            primaryBtnText: 'Order Online Now',
            primaryTarget: 'menu',
            secondaryBtnText: 'Track Active Order',
            secondaryTarget: 'deals',
            imageUrl: 'https://images.unsplash.com/photo-1617347454431-f49d7ff5c3b1?auto=format&fit=crop&w=800&h=600&q=80',
            bgImageUrl: 'https://images.unsplash.com/photo-1617347454431-f49d7ff5c3b1?auto=format&fit=crop&w=1920&q=85',
            cardTag: '🚀 EXPRESS DELIVERY',
            cardTitle: 'Fresh, Steaming & Fast To Doorstep',
            cardPrice: 1200,
            cardOriginalPrice: 1500,
            rating: '4.9★'
        }
    ];

    private async loadHeroSlides(branchId: number): Promise<void> {
        try {
            const slides = await this.onlineOrderHeroSlideService.getActiveByBranch(branchId);
            if (slides?.length) {
                this.heroSlides = slides.map((s) => ({
                    id: s.id,
                    badge: s.badge || '',
                    badgeIcon: s.badgeIcon || '🔥',
                    titleMain: s.titleMain || '',
                    titleHighlight: s.titleHighlight || '',
                    titleEnd: s.titleEnd || '',
                    description: s.description || '',
                    offerPill: s.offerPill || '',
                    primaryBtnText: s.primaryBtnText || 'Order Now',
                    primaryTarget: s.primaryTarget || 'menu',
                    secondaryBtnText: s.secondaryBtnText || 'Explore Full Menu',
                    secondaryTarget: s.secondaryTarget || 'menu',
                    imageUrl: this.onlineOrderHeroSlideService.getImageUrl(s.imagePath),
                    bgImageUrl:
                        this.onlineOrderHeroSlideService.getImageUrl(s.bgImagePath || s.imagePath) ||
                        this.onlineOrderHeroSlideService.getImageUrl(s.imagePath),
                    rating: s.rating || '4.9★',
                }));
            } else {
                this.heroSlides = [...this.defaultHeroSlides];
            }
        } catch {
            this.heroSlides = [...this.defaultHeroSlides];
        }

        this.currentHeroSlide = 0;
        if (this.heroSlides.length > 1) {
            this.startHeroSlider();
        } else {
            this.stopHeroSlider();
        }
    }

    startHeroSlider(): void {
        this.stopHeroSlider();
        // Interval longer than CSS crossfade (~0.85s) so each transition can finish.
        this.heroSlideTimer = setInterval(() => {
            this.nextHeroSlide();
        }, 4500);
    }

    stopHeroSlider(): void {
        if (this.heroSlideTimer) {
            clearInterval(this.heroSlideTimer);
            this.heroSlideTimer = null;
        }
    }

    nextHeroSlide(): void {
        this.currentHeroSlide = (this.currentHeroSlide + 1) % this.heroSlides.length;
    }

    prevHeroSlide(): void {
        this.currentHeroSlide = (this.currentHeroSlide - 1 + this.heroSlides.length) % this.heroSlides.length;
    }

    goToHeroSlide(index: number): void {
        this.currentHeroSlide = index;
        this.startHeroSlider();
    }

    ngOnInit(): void {
        this.heroSlides = [...this.defaultHeroSlides];
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
                void this.loadHeroSlides(this.branchId);
                this.loadCatalog(this.branchId, paramProductId ? Number(paramProductId) : undefined);
            } else {
                this.handleMissingBranchId();
            }
        });
    }

    ngOnDestroy(): void {
        this.stopHeroSlider();
    }

    @HostListener('window:resize')
    onResize(): void {
        this.checkScreenSize();
    }

    @HostListener('window:scroll', [])
    onWindowScroll(): void {
        const y =
            window.scrollY ||
            document.documentElement.scrollTop ||
            document.body.scrollTop ||
            0;
        this.isScrolled = y > 24;
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
                if (info.address) this.branchAddress = info.address;
                if (info.phone) this.branchPhone = info.phone;
                if (info.currencyFormat) this.currencyFormat = info.currencyFormat;
                if (info.dateFormat) this.dateFormat = info.dateFormat;
                this.facebookUrl = info.facebookUrl || '';
                this.instagramUrl = info.instagramUrl || '';
                this.youtubeUrl = info.youtubeUrl || '';
                this.isRestaurant = !!info.isRestaurant || info.businessType === 2;
                if (info.latitude != null) this.branchLatitude = Number(info.latitude);
                if (info.longitude != null) this.branchLongitude = Number(info.longitude);
                if (info.categories && Array.isArray(info.categories) && info.categories.length > 0) {
                    this.backendCategories = info.categories;
                    this.extractCategories();
                    this.applyFilter();
                }
                if (this.locationCaptured) {
                    this.calculateDistance();
                }
                return;
            }
        } catch {
            // Ignore error
        }
        this.branchName = '';
        this.branchAddress = '';
        this.branchPhone = '';
        this.facebookUrl = '';
        this.instagramUrl = '';
        this.youtubeUrl = '';
        this.isRestaurant = false;
        this.branchLatitude = null;
        this.branchLongitude = null;
    }

    get socialFacebookHref(): string {
        return this.facebookUrl || 'https://www.facebook.com/';
    }

    get socialInstagramHref(): string {
        return this.instagramUrl || 'https://www.instagram.com/';
    }

    get socialYoutubeHref(): string {
        return this.youtubeUrl || 'https://www.youtube.com/';
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
            const promises: Promise<any>[] = [
                this.customerOrderService.getOnlineCatalog(bId || undefined),
            ];
            if (bId && (!this.backendCategories || this.backendCategories.length === 0)) {
                promises.push(this.customerOrderService.getOnlineCategories(bId).catch(() => []));
            }
            const results = await Promise.all(promises);
            this.products = results[0] || [];
            if (results.length > 1 && Array.isArray(results[1]) && results[1].length > 0) {
                this.backendCategories = results[1];
            }

            if (this.products && this.products.length > 0 && this.products[0].currencyFormat) {
                this.currencyFormat = this.products[0].currencyFormat;
            }
            this.organizeCarousels();
            this.extractCategories();
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

        // 1. Add all categories configured by admin on backend
        (this.backendCategories || []).forEach((c) => {
            if (c && c.trim()) {
                set.add(c.trim());
            }
        });

        // 2. Also add any categories found on loaded products
        (this.products || []).forEach((p) => {
            if (p.categoryName && p.categoryName.trim()) {
                set.add(p.categoryName.trim());
            }
        });

        const allCats = Array.from(set);
        const regularCats = allCats.filter(
            (c) => c.toLowerCase() !== 'all' && !c.toLowerCase().includes('deal')
        );

        // Always provide 'All' first, then '🔥 Deals', then all admin-created categories
        this.categories = ['All', '🔥 Deals', ...regularCats];
    }

    private organizeCarousels(): void {
        // Find items that are deals or have deal category ONLY
        const explicitDeals = (this.products || []).filter(
            (p) => p.isDeal === true || (p.categoryName && p.categoryName.trim().toLowerCase().includes('deal'))
        );
        this.dealsList = explicitDeals; // Strictly deals only! No regular product fallback!
        this.trendingDeals = explicitDeals;

        // Popular: items with rating or top items
        this.popularItems = (this.products || []).length > 4 ? (this.products || []).slice(3, 11) : (this.products || []);

        // Group by category for dedicated rows
        const catMap = new Map<string, any[]>();
        (this.products || []).forEach((p) => {
            const cat = (p.categoryName && p.categoryName.trim()) || 'Featured Menu';
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

    getCategorySectionId(category: string): string {
        if (!category) return '';
        const lower = category.toLowerCase().trim();
        if (lower.includes('deal')) {
            return 'storeDealsSection';
        }
        if (lower.includes('popular')) {
            return 'storePopularSection';
        }
        if (lower.includes('trend')) {
            return 'storeTrendingSection';
        }
        const clean = lower
            .replace(/[^a-z0-9]/g, '-')
            .replace(/-+/g, '-')
            .replace(/^-|-$/g, '');
        return 'cat-section-' + clean;
    }

    scrollToSection(sectionId: string, event?: Event): void {
        if (event) {
            event.preventDefault();
            event.stopPropagation();
        }
        if (sectionId === 'home' || sectionId === 'menu' || sectionId === 'deals' || sectionId === 'contact') {
            this.activeNav = sectionId as any;
        }
        this.mobileNavOpen = false;

        if (sectionId === 'home') {
            this.selectedCategory = 'All';
            if (this.searchKeyword) {
                this.searchKeyword = '';
                this.applyFilter();
            }
            window.scrollTo({ top: 0, behavior: 'smooth' });
        } else if (sectionId === 'menu') {
            const el = document.getElementById('storeMenuSection');
            if (el) {
                el.scrollIntoView({ behavior: 'smooth', block: 'start' });
            } else {
                window.scrollTo({ top: 520, behavior: 'smooth' });
            }
        } else if (sectionId === 'deals') {
            const el = document.getElementById('storeDealsSection');
            if (el) {
                el.scrollIntoView({ behavior: 'smooth', block: 'start' });
            } else {
                this.selectCategory('🔥 Deals', event);
            }
        } else if (sectionId === 'contact') {
            const el = document.getElementById('storeContactSection');
            if (el) {
                el.scrollIntoView({ behavior: 'smooth', block: 'start' });
            } else {
                this.contactModalVisible = true;
            }
        }
    }

    selectCategory(cat: string, event?: Event): void {
        if (event) {
            event.preventDefault();
            event.stopPropagation();
        }

        this.selectedCategory = cat;

        if (this.searchKeyword) {
            this.searchKeyword = '';
            this.applyFilter();
        }

        if (this.activeMobileTab === 'favorites') {
            this.activeMobileTab = 'home';
        }

        setTimeout(() => {
            const activeChip = document.querySelector('.fp-cat-chip.active, .fp-circle-cat-item.active');
            if (activeChip) {
                activeChip.scrollIntoView({ behavior: 'smooth', inline: 'center', block: 'nearest' });
            }
        }, 50);

        const lower = (cat || '').toLowerCase().trim();
        if (!cat || lower === 'all') {
            const menuEl = document.getElementById('storeMenuSection');
            if (menuEl) {
                menuEl.scrollIntoView({ behavior: 'smooth', block: 'start' });
            } else {
                window.scrollTo({ top: 0, behavior: 'smooth' });
            }
            return;
        }

        const targetId = this.getCategorySectionId(cat);
        const targetEl = document.getElementById(targetId);
        if (targetEl) {
            targetEl.scrollIntoView({ behavior: 'smooth', block: 'start' });
        } else {
            const allSections = document.querySelectorAll('.fp-store-section');
            let found = false;
            allSections.forEach((section) => {
                if (found) return;
                const titleEl = section.querySelector('.section-title');
                if (titleEl && titleEl.textContent && titleEl.textContent.toLowerCase().includes(lower.replace(/[^a-z0-9]/g, ''))) {
                    section.scrollIntoView({ behavior: 'smooth', block: 'start' });
                    found = true;
                }
            });
            if (!found) {
                const menuEl = document.getElementById('storeMenuSection');
                if (menuEl) {
                    menuEl.scrollIntoView({ behavior: 'smooth', block: 'start' });
                }
            }
        }
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

        this.filteredProducts = (this.products || []).filter((p) => {
            // Favorites tab restriction
            if (this.activeMobileTab === 'favorites') {
                if (!this.favorites.has(p.id)) {
                    return false;
                }
            }

            // Category filtering: Deals only matches items that are deals or have deal category
            let matchesCat = true;
            if (selectedCat === 'All') {
                matchesCat = true;
            } else if (selectedCat === '🔥 Deals' || targetCatLower.includes('deal')) {
                matchesCat = p.isDeal === true || ((p.categoryName || '').toLowerCase().includes('deal'));
            } else if (selectedCat === '✨ Popular') {
                matchesCat = true;
            } else {
                const pCatLower = (p.categoryName || '').trim().toLowerCase();
                matchesCat = pCatLower === targetCatLower;
            }

            // Pill filtering (Foodies Pakistan pills)
            let matchesPill = true;
            if (pill === '🔥 Top Deals') {
                matchesPill = p.isDeal === true || ((p.categoryName || '').toLowerCase().includes('deal'));
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
                (p.barcode || '').toLowerCase().includes(query) ||
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
        if (c.includes('burger') || c.includes('burgur') || c.includes('fast')) {
            return {
                icon: 'pi pi-check',
                color: '#f97316',
                label: cat,
                image: 'https://images.unsplash.com/photo-1568901346375-23c9450c58cd?auto=format&fit=crop&w=150&q=80',
            };
        }
        if (c.includes('showarma') || c.includes('shawarma') || c.includes('roll') || c.includes('wrap')) {
            return {
                icon: 'pi pi-bookmark',
                color: '#d97706',
                label: cat,
                image: 'https://images.unsplash.com/photo-1529006557810-274b9b2fc783?auto=format&fit=crop&w=150&q=80',
            };
        }
        if (c.includes('drink') || c.includes('beverage') || c.includes('cold drink') || c.includes('juice') || c.includes('soda')) {
            return {
                icon: 'pi pi-heart',
                color: '#0284c7',
                label: cat,
                image: 'https://images.unsplash.com/photo-1551024709-8f23befc6f87?auto=format&fit=crop&w=150&q=80',
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

    // Direct curated images matching exact catalog items (ID & name)
    private static readonly CATALOG_IMAGES_BY_ID: { [id: number]: string } = {
        1: 'https://images.unsplash.com/photo-1550583724-b2692b85b150?auto=format&fit=crop&w=500&h=500&q=80', // Milk
        2: 'https://images.unsplash.com/photo-1589985270826-4b7bb135bc9d?auto=format&fit=crop&w=500&h=500&q=80', // Butter
        3: 'https://images.unsplash.com/photo-1628088062854-d1870b4553da?auto=format&fit=crop&w=500&h=500&q=80', // Desi Ghee
        4: 'assets/menu-items/barfi.jpg', // Barfi (Pakistani silver vark barfi)
        5: 'assets/menu-items/milk_bread.jpg', // Milk Bread Large
        6: 'assets/menu-items/milk_bread.jpg', // Milk Bread Small
        7: 'https://images.unsplash.com/photo-1509440159596-0249088772ff?auto=format&fit=crop&w=500&h=500&q=80', // Brown Bread
        8: 'https://images.unsplash.com/photo-1549931319-a545dcf3bc73?auto=format&fit=crop&w=500&h=500&q=80', // Sandwich Bread
        9: 'https://images.unsplash.com/photo-1586190848861-99aa4a171e90?auto=format&fit=crop&w=500&h=500&q=80', // Burger Bun Pack 4
        10: 'https://images.unsplash.com/photo-1619740455993-9e612b1af08a?auto=format&fit=crop&w=500&h=500&q=80', // Hot Dog Bun Pack 4
        11: 'https://images.unsplash.com/photo-1568254183919-78a4f43a2877?auto=format&fit=crop&w=500&h=500&q=80', // Dinner Rolls Pack 6
        12: 'https://images.unsplash.com/photo-1619535860434-ba1d8fa12536?auto=format&fit=crop&w=500&h=500&q=80', // Garlic Bread Stick
        13: 'https://images.unsplash.com/photo-1509440159596-0249088772ff?auto=format&fit=crop&w=500&h=500&q=80', // Fruit Bun
        14: 'https://images.unsplash.com/photo-1601050690597-df0568f70950?auto=format&fit=crop&w=500&h=500&q=80', // Plain Kulcha Pack 4
        15: 'https://images.unsplash.com/photo-1576618148400-f54bed99fcfd?auto=format&fit=crop&w=500&h=500&q=80', // Chocolate Cupcake
        16: 'https://images.unsplash.com/photo-1587668178277-295251f900ce?auto=format&fit=crop&w=500&h=500&q=80', // Vanilla Cupcake
        17: 'https://images.unsplash.com/photo-1551024709-8f23befc6f87?auto=format&fit=crop&w=500&h=500&q=80', // Cream Donut
        18: 'https://images.unsplash.com/photo-1551024601-bec78aea704b?auto=format&fit=crop&w=500&h=500&q=80', // Chocolate Donut
        19: 'https://images.unsplash.com/photo-1555507036-ab1f4038808a?auto=format&fit=crop&w=500&h=500&q=80', // Croissant Plain
        20: 'https://images.unsplash.com/photo-1578985545062-69928b1d9587?auto=format&fit=crop&w=500&h=500&q=80', // Chocolate Pastry
        21: 'https://images.unsplash.com/photo-1565958011703-44f9829ba187?auto=format&fit=crop&w=500&h=500&q=80', // Pineapple Pastry
        22: 'https://images.unsplash.com/photo-1586985289688-ca3cf47d3e6e?auto=format&fit=crop&w=500&h=500&q=80', // Muffin Chocolate
        23: 'https://images.unsplash.com/photo-1586985289688-ca3cf47d3e6e?auto=format&fit=crop&w=500&h=500&q=80', // Muffin Blueberry
        24: 'https://images.unsplash.com/photo-1606313564200-e75d5e30476c?auto=format&fit=crop&w=500&h=500&q=80', // Slice Cake Chocolate
        25: 'https://images.unsplash.com/photo-1558961363-fa8fdf82db35?auto=format&fit=crop&w=500&h=500&q=80', // Sooper Biscuits Pack
        26: 'https://images.unsplash.com/photo-1499636136210-6f4ee915583e?auto=format&fit=crop&w=500&h=500&q=80', // Chocolate Chip Cookies
        27: 'https://images.unsplash.com/photo-1558961363-fa8fdf82db35?auto=format&fit=crop&w=500&h=500&q=80', // Butter Cookies Tin
        28: 'https://images.unsplash.com/photo-1589367920969-ab8e050bbb04?auto=format&fit=crop&w=500&h=500&q=80', // Rusk Plain Large
        29: 'https://images.unsplash.com/photo-1589367920969-ab8e050bbb04?auto=format&fit=crop&w=500&h=500&q=80', // Rusk Zeera
        30: 'https://images.unsplash.com/photo-1558961363-fa8fdf82db35?auto=format&fit=crop&w=500&h=500&q=80', // Glucose Biscuits
        31: 'https://images.unsplash.com/photo-1563729784474-d77dbb933a9e?auto=format&fit=crop&w=500&h=500&q=80', // Cream Biscuits Chocolate
        32: 'https://images.unsplash.com/photo-1541781774459-bb2af2f05b55?auto=format&fit=crop&w=500&h=500&q=80', // Jam Sandwich Biscuits
        33: 'https://images.unsplash.com/photo-1550583724-b2692b85b150?auto=format&fit=crop&w=500&h=500&q=80', // Olper\'s Milk 1L
        34: 'https://images.unsplash.com/photo-1550583724-b2692b85b150?auto=format&fit=crop&w=500&h=500&q=80', // Olper\'s Milk 500ml
        35: 'https://images.unsplash.com/photo-1563227812-0ea4c22e6cc8?auto=format&fit=crop&w=500&h=500&q=80', // Olper\'s Cream 200ml
        36: 'https://images.unsplash.com/photo-1488477181946-6428a0291777?auto=format&fit=crop&w=500&h=500&q=80', // Nestle Yogurt Plain 400g
        37: 'https://images.unsplash.com/photo-1488477181946-6428a0291777?auto=format&fit=crop&w=500&h=500&q=80', // Nestle Yogurt Sweet 400g
        38: 'https://images.unsplash.com/photo-1589985270826-4b7bb135bc9d?auto=format&fit=crop&w=500&h=500&q=80', // Butter 200g
        39: 'https://images.unsplash.com/photo-1486297678162-eb2a19b0a32d?auto=format&fit=crop&w=500&h=500&q=80', // Cheese Slices Pack 10
        40: 'https://images.unsplash.com/photo-1582722872445-44dc5f7e3c8f?auto=format&fit=crop&w=500&h=500&q=80', // Eggs Farm Fresh Dozen
        41: 'https://images.unsplash.com/photo-1576092768241-dec231879fc3?auto=format&fit=crop&w=500&h=500&q=80', // Tapal Danedar 900g
        42: 'https://images.unsplash.com/photo-1544787219-7f47ccb76574?auto=format&fit=crop&w=500&h=500&q=80', // Tapal Danedar 200g
        43: 'https://images.unsplash.com/photo-1597481499750-3e6b22637e12?auto=format&fit=crop&w=500&h=500&q=80', // Lipton Yellow Label 200g
        44: 'https://images.unsplash.com/photo-1587735243615-c03f25aaff15?auto=format&fit=crop&w=500&h=500&q=80', // Nestle Everyday 400g
        45: 'https://images.unsplash.com/photo-1587735243615-c03f25aaff15?auto=format&fit=crop&w=500&h=500&q=80', // Nestle Nido 400g
        46: 'https://images.unsplash.com/photo-1622483767028-3f66f32aef97?auto=format&fit=crop&w=500&h=500&q=80', // Soft Drink 1.5L Cola
        47: 'https://images.unsplash.com/photo-1513558161293-cdaf765ed2fd?auto=format&fit=crop&w=500&h=500&q=80', // Soft Drink 1.5L Lemon
        48: 'https://images.unsplash.com/photo-1548839140-29a749e1bc4e?auto=format&fit=crop&w=500&h=500&q=80', // Mineral Water 1.5L
        49: 'https://images.unsplash.com/photo-1613478223719-2ab802602423?auto=format&fit=crop&w=500&h=500&q=80', // Orange Juice 1L
        50: 'https://images.unsplash.com/photo-1534353473418-4cfa6c56fd38?auto=format&fit=crop&w=500&h=500&q=80', // Mango Juice 1L
        51: 'https://images.unsplash.com/photo-1566478989037-eec170784d0b?auto=format&fit=crop&w=500&h=500&q=80', // Lays Classic 30g
        52: 'https://images.unsplash.com/photo-1566478989037-eec170784d0b?auto=format&fit=crop&w=500&h=500&q=80', // Lays Masala 30g
        53: 'https://images.unsplash.com/photo-1566478989037-eec170784d0b?auto=format&fit=crop&w=500&h=500&q=80', // Lays French Cheese 30g
        54: 'https://images.unsplash.com/photo-1599490659213-e2b9527bd087?auto=format&fit=crop&w=500&h=500&q=80', // Kurkure Chutney Chaska
        55: 'https://images.unsplash.com/photo-1599490659213-e2b9527bd087?auto=format&fit=crop&w=500&h=500&q=80', // Kurkure Masala Munch
        56: 'https://images.unsplash.com/photo-1601050690597-df0568f70950?auto=format&fit=crop&w=500&h=500&q=80', // Nimco Mix 200g
        57: 'https://images.unsplash.com/photo-1578849278619-e73505e9610f?auto=format&fit=crop&w=500&h=500&q=80', // Popcorn Butter 100g
        58: 'https://images.unsplash.com/photo-1567892324422-540bb9687e35?auto=format&fit=crop&w=500&h=500&q=80', // Peanuts Roasted 200g
        59: 'https://images.unsplash.com/photo-1574323347407-f5e1ad6d020b?auto=format&fit=crop&w=500&h=500&q=80', // Wheat Flour 5kg
        60: 'https://images.unsplash.com/photo-1574323347407-f5e1ad6d020b?auto=format&fit=crop&w=500&h=500&q=80', // Wheat Flour 10kg
        61: 'https://images.unsplash.com/photo-1587735243615-c03f25aaff15?auto=format&fit=crop&w=500&h=500&q=80', // Sugar 1kg
        62: 'https://images.unsplash.com/photo-1586201375761-83865001e31c?auto=format&fit=crop&w=500&h=500&q=80', // Rice Basmati 5kg
        63: 'https://images.unsplash.com/photo-1474979266404-7eaacbcd87c5?auto=format&fit=crop&w=500&h=500&q=80', // Cooking Oil 1L
        64: 'https://images.unsplash.com/photo-1474979266404-7eaacbcd87c5?auto=format&fit=crop&w=500&h=500&q=80', // Cooking Oil 5L
        65: 'https://images.unsplash.com/photo-1518110925495-5fe2fda0442c?auto=format&fit=crop&w=500&h=500&q=80', // Salt Iodized 800g
        66: 'https://images.unsplash.com/photo-1596040033229-a9821ebd058d?auto=format&fit=crop&w=500&h=500&q=80', // National Red Chilli 200g
        67: 'https://images.unsplash.com/photo-1615485290382-441e4d049cb5?auto=format&fit=crop&w=500&h=500&q=80', // National Turmeric 200g
        68: 'https://images.unsplash.com/photo-1563379091339-03b21ab4a4f8?auto=format&fit=crop&w=500&h=500&q=80', // Shan Biryani Masala
        69: 'https://images.unsplash.com/photo-1589301760014-d929f3979dbc?auto=format&fit=crop&w=500&h=500&q=80', // Shan Chicken Masala
        70: 'https://images.unsplash.com/photo-1607604276583-eef5d076aa5f?auto=format&fit=crop&w=500&h=500&q=80', // National Ketchup 500g
        71: 'https://images.unsplash.com/photo-1568909344668-6f14a07b56a0?auto=format&fit=crop&w=500&h=500&q=80', // Mitchell\'s Jam Mixed Fruit 450g
        72: 'https://images.unsplash.com/photo-1521483451569-e33803c0330c?auto=format&fit=crop&w=500&h=500&q=80', // Cornflakes 250g
        73: 'https://images.unsplash.com/photo-1584776296944-ab6fb57b0bdd?auto=format&fit=crop&w=500&h=500&q=80', // Oats 500g
        74: 'https://images.unsplash.com/photo-1587049352846-4a222e784d38?auto=format&fit=crop&w=500&h=500&q=80', // Honey 250g
        75: 'https://images.unsplash.com/photo-1589733955941-5eeaf752f6dd?auto=format&fit=crop&w=500&h=500&q=80', // Peanut Butter 340g
        76: 'https://images.unsplash.com/photo-1584308666744-24d5c474f2ae?auto=format&fit=crop&w=500&h=500&q=80', // Chocolate Spread 350g
        77: 'https://images.unsplash.com/photo-1569718212165-3a8278d5f624?auto=format&fit=crop&w=500&h=500&q=80', // Instant Noodles Pack
        78: 'https://images.unsplash.com/photo-1612927601601-6638404737ce?auto=format&fit=crop&w=500&h=500&q=80', // Instant Noodles Family 5
        79: 'https://images.unsplash.com/photo-1585421514738-01798e348b17?auto=format&fit=crop&w=500&h=500&q=80', // Surf Excel 1kg
        80: 'https://images.unsplash.com/photo-1585421514738-01798e348b17?auto=format&fit=crop&w=500&h=500&q=80', // Surf Excel 500g
        81: 'https://images.unsplash.com/photo-1585421514738-01798e348b17?auto=format&fit=crop&w=500&h=500&q=80', // Dishwashing Liquid 500ml
        82: 'https://images.unsplash.com/photo-1584556812952-905ffd0c611a?auto=format&fit=crop&w=500&h=500&q=80', // Floor Cleaner 1L
        83: 'https://images.unsplash.com/photo-1584556812952-905ffd0c611a?auto=format&fit=crop&w=500&h=500&q=80', // Tissue Roll Pack 4
        84: 'https://images.unsplash.com/photo-1584556812952-905ffd0c611a?auto=format&fit=crop&w=500&h=500&q=80', // Kitchen Tissue Pack
        85: 'https://images.unsplash.com/photo-1610557892470-55d9e80c0bce?auto=format&fit=crop&w=500&h=500&q=80', // Garbage Bags Medium
        86: 'https://images.unsplash.com/photo-1518110925495-5fe2fda0442c?auto=format&fit=crop&w=500&h=500&q=80', // Match Box Pack 10
        87: 'https://images.unsplash.com/photo-1600857544200-b2f666a9a2ec?auto=format&fit=crop&w=500&h=500&q=80', // Lux Soap 115g
        88: 'https://images.unsplash.com/photo-1600857544200-b2f666a9a2ec?auto=format&fit=crop&w=500&h=500&q=80', // Lux Soap Pack 3
        89: 'https://images.unsplash.com/photo-1535585209827-a15fcdbc4c2d?auto=format&fit=crop&w=500&h=500&q=80', // Shampoo 180ml
        90: 'https://images.unsplash.com/photo-1559591937-e10220268595?auto=format&fit=crop&w=500&h=500&q=80', // Toothpaste 100g
        91: 'https://images.unsplash.com/photo-1559591937-e10220268595?auto=format&fit=crop&w=500&h=500&q=80', // Toothbrush Soft
        92: 'https://images.unsplash.com/photo-1600857544200-b2f666a9a2ec?auto=format&fit=crop&w=500&h=500&q=80', // Hand Wash 250ml
        93: 'https://images.unsplash.com/photo-1556228720-195a672e8a03?auto=format&fit=crop&w=500&h=500&q=80', // Face Wash 100ml
        94: 'https://images.unsplash.com/photo-1584556812952-905ffd0c611a?auto=format&fit=crop&w=500&h=500&q=80', // Tissue Pocket Pack
    };

    getProductImage(product: any, index: number): string {
        if (product?.imagePath && !product.imagePath.includes('placeholder.png')) {
            return this.getImageUrl(product.imagePath);
        }

        // Direct matching by Product ID (exact 1-to-1 match for all 94 catalog items)
        if (product?.id && OnlineOrderComponent.CATALOG_IMAGES_BY_ID[product.id]) {
            return OnlineOrderComponent.CATALOG_IMAGES_BY_ID[product.id];
        }

        const name = (product?.name || '').toLowerCase().trim();
        const cat = (product?.categoryName || '').toLowerCase().trim();
        const desc = (product?.description || '').toLowerCase().trim();

        // 1. Cheezious-style Pizza Deals & Combos
        if (name.includes('small pizza') || (name.includes('pizza') && name.includes('small'))) {
            return 'assets/menu-items/pizza_small.jpg';
        }
        if (name.includes('regular pizza') || (name.includes('pizza') && name.includes('regular'))) {
            return 'assets/menu-items/pizza_regular.jpg';
        }
        if (name.includes('large pizza') || (name.includes('pizza') && (name.includes('large') || name.includes('family')))) {
            return 'assets/menu-items/pizza_large.jpg';
        }
        if (name.includes('pizza') || desc.includes('pizza')) {
            return 'assets/menu-items/pizza_regular.jpg';
        }

        // 2. Cheezious-style Burger Combos & Deals
        if (name.includes('burger deal') || name.includes('somewhat') || name.includes('combo') || name.includes('bazinga') || name.includes('amazing') || name.includes('cheezious') || name.includes('fast food deal')) {
            return 'assets/menu-items/burger_combo.jpg';
        }

        // 3. Specific Bakery Item Names (Match against product name FIRST, NOT category!)
        if (name.includes('barfi')) return 'assets/menu-items/barfi.jpg';
        if (name.includes('milk bread') || name.includes('sandwich bread')) return 'assets/menu-items/milk_bread.jpg';
        if (name.includes('brown bread')) return 'https://images.unsplash.com/photo-1509440159596-0249088772ff?auto=format&fit=crop&w=500&h=500&q=80';
        if (name.includes('burger bun')) return 'https://images.unsplash.com/photo-1586190848861-99aa4a171e90?auto=format&fit=crop&w=500&h=500&q=80';
        if (name.includes('hot dog')) return 'https://images.unsplash.com/photo-1619740455993-9e612b1af08a?auto=format&fit=crop&w=500&h=500&q=80';
        if (name.includes('dinner roll') || name.includes('rolls')) return 'https://images.unsplash.com/photo-1568254183919-78a4f43a2877?auto=format&fit=crop&w=500&h=500&q=80';
        if (name.includes('garlic bread')) return 'https://images.unsplash.com/photo-1619535860434-ba1d8fa12536?auto=format&fit=crop&w=500&h=500&q=80';
        if (name.includes('fruit bun')) return 'https://images.unsplash.com/photo-1509440159596-0249088772ff?auto=format&fit=crop&w=500&h=500&q=80';
        if (name.includes('kulcha') || name.includes('naan')) return 'https://images.unsplash.com/photo-1601050690597-df0568f70950?auto=format&fit=crop&w=500&h=500&q=80';

        // 4. Sweets, Cakes, Donuts & Pastries
        if (name.includes('donut')) return 'https://images.unsplash.com/photo-1551024601-bec78aea704b?auto=format&fit=crop&w=500&h=500&q=80';
        if (name.includes('cupcake')) return 'https://images.unsplash.com/photo-1576618148400-f54bed99fcfd?auto=format&fit=crop&w=500&h=500&q=80';
        if (name.includes('muffin')) return 'https://images.unsplash.com/photo-1586985289688-ca3cf47d3e6e?auto=format&fit=crop&w=500&h=500&q=80';
        if (name.includes('croissant')) return 'https://images.unsplash.com/photo-1555507036-ab1f4038808a?auto=format&fit=crop&w=500&h=500&q=80';
        if (name.includes('pastry') || name.includes('cake')) return 'https://images.unsplash.com/photo-1578985545062-69928b1d9587?auto=format&fit=crop&w=500&h=500&q=80';

        // 5. Dairy
        if (name.includes('cheese')) return 'https://images.unsplash.com/photo-1486297678162-eb2a19b0a32d?auto=format&fit=crop&w=500&h=500&q=80';
        if (name.includes('ghee')) return 'https://images.unsplash.com/photo-1628088062854-d1870b4553da?auto=format&fit=crop&w=500&h=500&q=80';
        if (name.includes('butter')) return 'https://images.unsplash.com/photo-1589985270826-4b7bb135bc9d?auto=format&fit=crop&w=500&h=500&q=80';
        if (name.includes('yogurt')) return 'https://images.unsplash.com/photo-1488477181946-6428a0291777?auto=format&fit=crop&w=500&h=500&q=80';
        if (name.includes('egg')) return 'https://images.unsplash.com/photo-1582722872445-44dc5f7e3c8f?auto=format&fit=crop&w=500&h=500&q=80';
        if (name.includes('milk') || name.includes('cream')) return 'https://images.unsplash.com/photo-1550583724-b2692b85b150?auto=format&fit=crop&w=500&h=500&q=80';

        // Fast food fallback
        if (name.includes('burger') || name.includes('zinger') || name.includes('chicken') || name.includes('fast food')) {
            return 'assets/menu-items/burger_combo.jpg';
        }

        // Default domain fallback
        const domain = this.getProductDomain(product);
        const idx = Math.abs(product?.id || index || 0);
        switch (domain) {
            case 'food':
                return 'assets/menu-items/burger_combo.jpg';
            case 'solar-panel':
                return this.solarPanelImages[idx % this.solarPanelImages.length];
            case 'inverter':
                return this.inverterImages[idx % this.inverterImages.length];
            case 'battery':
                return this.batteryImages[idx % this.batteryImages.length];
            case 'solar-accessory':
                return this.solarAccessoryImages[idx % this.solarAccessoryImages.length];
            default:
                return 'assets/menu-items/burger_combo.jpg';
        }
    }

    /** When remote/DB image fails, swap to a local food asset so alt text never overlaps the badge. */
    onProductImageError(event: Event, product?: any): void {
        const img = event?.target as HTMLImageElement | null;
        if (!img || img.dataset['fallbackApplied'] === '1') {
            return;
        }
        img.dataset['fallbackApplied'] = '1';
        const name = (product?.name || '').toLowerCase();
        if (name.includes('pizza')) {
            img.src = 'assets/menu-items/pizza_regular.jpg';
        } else if (name.includes('burger') || name.includes('zinger')) {
            img.src = 'assets/menu-items/burger_combo.jpg';
        } else if (name.includes('barfi')) {
            img.src = 'assets/menu-items/barfi.jpg';
        } else if (name.includes('bread') || name.includes('bun')) {
            img.src = 'assets/menu-items/milk_bread.jpg';
        } else {
            img.src = 'assets/menu-items/burger_combo.jpg';
        }
        img.alt = '';
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

    hasVariants(product: any): boolean {
        if (!product) return false;
        if (product.hasVariants != null) return !!product.hasVariants;
        if (product.variants && product.variants.length > 0) return true;
        if (product.sizes && product.sizes.length > 0) return true;
        return false;
    }

    getOriginalPrice(product: any): number {
        const price = product.price || 0;
        return Math.round(price * 1.15);
    }

    // FoodiesPakistan Product Detail Modal / Bottom Sheet
    async openProductModal(product: any, event?: Event): Promise<void> {
        if (event) {
            event.stopPropagation();
        }
        this.selectedProduct = product;
        this.detailQuantity = 1;
        this.productModifierGroups = [];
        this.selectedModifierOptions = {};
        this.detailModalVisible = true;
        this.loadingModifiers = true;

        try {
            const groups = await this.productModifierService.getByProduct(product.id);
            this.productModifierGroups = (groups || []).filter(
                (g) => g.options && g.options.length > 0
            );
            for (const g of this.productModifierGroups) {
                this.selectedModifierOptions[g.id] = [];
                const defaultOpt = g.options.find((o) => o.isDefault);
                if (defaultOpt) {
                    this.selectedModifierOptions[g.id] = [defaultOpt.id];
                } else if (g.isRequired && g.options.length > 0) {
                    this.selectedModifierOptions[g.id] = [g.options[0].id];
                }
            }
        } catch (err) {
            console.warn('Failed to load modifiers for product', err);
            this.productModifierGroups = [];
        } finally {
            this.loadingModifiers = false;
        }
    }

    closeProductModal(): void {
        this.detailModalVisible = false;
        this.selectedProduct = null;
        this.productModifierGroups = [];
        this.selectedModifierOptions = {};
        this.loadingModifiers = false;
    }

    updateDetailQuantity(change: number): void {
        const next = this.detailQuantity + change;
        if (next >= 1) {
            this.detailQuantity = next;
        }
    }

    isOptionSelected(group: ProductModifierGroupDto, option: ProductModifierOptionDto): boolean {
        const selected = this.selectedModifierOptions[group.id] || [];
        return selected.includes(option.id);
    }

    toggleOption(group: ProductModifierGroupDto, option: ProductModifierOptionDto): void {
        const isMulti = group.allowMultiple || (group.maxSelection != null && group.maxSelection > 1);
        const current = [...(this.selectedModifierOptions[group.id] || [])];
        const idx = current.indexOf(option.id);

        if (isMulti) {
            if (idx > -1) {
                if (group.isRequired && current.length <= 1) {
                    return;
                }
                current.splice(idx, 1);
            } else {
                if (group.maxSelection && current.length >= group.maxSelection) {
                    current.shift();
                }
                current.push(option.id);
            }
            this.selectedModifierOptions[group.id] = current;
        } else {
            if (idx > -1) {
                if (!group.isRequired) {
                    this.selectedModifierOptions[group.id] = [];
                }
            } else {
                this.selectedModifierOptions[group.id] = [option.id];
            }
        }
    }

    getOptionPrice(opt: ProductModifierOptionDto): number {
        return opt.price ?? opt.extraPrice ?? 0;
    }

    getDetailUnitPrice(): number {
        if (!this.selectedProduct) return 0;
        let price = this.selectedProduct.price || 0;
        for (const g of this.productModifierGroups) {
            const selectedIds = this.selectedModifierOptions[g.id] || [];
            for (const optId of selectedIds) {
                const opt = g.options.find((o) => o.id === optId);
                if (opt) {
                    price += this.getOptionPrice(opt);
                }
            }
        }
        return price;
    }

    getDetailTotalPrice(): number {
        return this.getDetailUnitPrice() * this.detailQuantity;
    }

    addDetailToCart(): void {
        if (!this.selectedProduct) return;

        for (const g of this.productModifierGroups) {
            if (g.isRequired) {
                const selected = this.selectedModifierOptions[g.id] || [];
                if (!selected.length) {
                    this.messageService.add({
                        severity: 'warn',
                        summary: 'Option Required',
                        detail: `Please select an option for "${g.name}"`,
                    });
                    return;
                }
            }
        }

        const selectedOptionNames: string[] = [];
        const allSelectedIds: number[] = [];

        for (const g of this.productModifierGroups) {
            const selectedIds = this.selectedModifierOptions[g.id] || [];
            for (const optId of selectedIds) {
                const opt = g.options.find((o) => o.id === optId);
                if (opt) {
                    allSelectedIds.push(opt.id);
                    const extra = this.getOptionPrice(opt);
                    selectedOptionNames.push(extra > 0 ? `${opt.name} (+PKR ${extra})` : opt.name);
                }
            }
        }

        const unitPrice = this.getDetailUnitPrice();
        const optionsText = selectedOptionNames.join(', ');

        this.cartService.addToCart(this.selectedProduct, this.detailQuantity, {
            unitPrice,
            optionsText: optionsText || undefined,
            selectedOptionIds: allSelectedIds,
        });

        this.messageService.add({
            severity: 'success',
            summary: 'Added to Cart',
            detail: `${this.detailQuantity > 1 ? this.detailQuantity + 'x ' : ''}${this.selectedProduct.name} added to your order`,
            life: 1800,
        });

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
            // Header is TTL + in-flight deduped in CustomerOrderService; safe to
            // call again after resolveBranchName without a duplicate network hit.
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
            const itemsWithOptions = this.cartService.items.filter((i) => i.optionsText);
            if (itemsWithOptions.length > 0) {
                const optLines = itemsWithOptions
                    .map((i) => `${i.quantity}x ${i.name} [${i.optionsText}]`)
                    .join(' | ');
                addressParts.push(`Options: ${optLines}`);
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
