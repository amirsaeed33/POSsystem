import {
    ChangeDetectorRef,
    Component,
    ElementRef,
    OnDestroy,
    OnInit,
    ViewChild,
} from '@angular/core';
import { Subscription } from 'rxjs';
import { MessageService } from 'primeng/api';
import { PosCartLine } from 'src/app/demo/api/pos';
import { PaymentType } from 'src/app/demo/api/sale';
import { CustomerDto, CustomerType } from 'src/app/demo/api/customer';
import { ProductDto } from 'src/app/demo/api/product';
import { RestaurantTableDto } from 'src/app/demo/api/restaurant-table';
import {
    CreateOrUpdateRestaurantOrderDto,
    OrderType,
    OrderTypes,
    RestaurantKitchenStatuses,
    RestaurantOrderDto,
    RestaurantOrderStatuses,
    SendToKitchenResultDto,
    UnpaidTakeawayOrderDto,
} from 'src/app/demo/api/restaurant-order';
import {
    ProductModifierGroupDto,
    ProductModifierOptionDto,
} from 'src/app/demo/api/product-modifier';
import { BranchDto } from 'src/app/demo/api/branch';
import { BranchContextService } from 'src/app/demo/service/branch-context.service';
import { BranchService } from 'src/app/demo/service/branch.service';
import { RestaurantTableService } from 'src/app/demo/service/restaurant-table.service';
import { RestaurantOrderService } from 'src/app/demo/service/restaurant-order.service';
import { ProductModifierService } from 'src/app/demo/service/product-modifier.service';
import { SaleService } from 'src/app/demo/service/sale.service';
import { ProductService } from 'src/app/demo/service/product.service';
import { StaffDto } from 'src/app/demo/api/staff';
import { StaffService } from 'src/app/demo/service/staff.service';
import { DealService } from 'src/app/demo/service/deal.service';
import { DealDto } from 'src/app/demo/api/deal';
import { AuthService } from 'src/app/demo/service/auth.service';
import { ActivatedRoute, Router } from '@angular/router';
import { RestaurantTakeawayAlarmService } from 'src/app/demo/service/restaurant-takeaway-alarm.service';
import { ThermalPrintService } from 'src/app/demo/service/thermal-print.service';
import { CustomerService } from 'src/app/demo/service/customer.service';
import { CustomerOrderService } from 'src/app/demo/service/customer-order.service';
import { DeliveryAreaService, DeliveryAreaDto } from 'src/app/demo/service/delivery-area.service';
import { CustomerSavedAddressDto } from 'src/app/demo/api/customer-order';
import { findProductImageNear, flyToCart } from 'src/app/demo/utils/fly-to-cart';

/** POS third-party delivery channel (runs as takeaway with a named customer). */
export type PosThirdPartyChannel = 'foodpanda' | null;

/** Single UI source of truth for Dine-In / Takeaway / Third Party / Online button highlight. */
export type PosOrderChannelUi = 'dineIn' | 'takeaway' | 'foodpanda' | 'online';

const THIRD_PARTY_CUSTOMER_NAME = 'Third Party';
/** Legacy customer name — still recognized for existing orders/customers. */
const LEGACY_FOODPANDA_CUSTOMER_NAME = 'Foodpanda';

export interface DealCustomiseItem {
    productId: number;
    productName: string;
    quantity: number;
    unitPrice: number;
    modifierGroups: ProductModifierGroupDto[];
    selectedOptions: Record<number, number[]>;
}

@Component({
    templateUrl: './pos.component.html',
    styleUrls: ['./pos.component.scss'],
    providers: [MessageService],
})
export class PosComponent implements OnInit, OnDestroy {
    @ViewChild('barcodeInput') barcodeInput?: ElementRef<HTMLInputElement>;
    @ViewChild('menuCategoriesTrack')
    menuCategoriesTrack?: ElementRef<HTMLDivElement>;

    private branchSub?: Subscription;

    saving = false;
    scanning = false;
    searchText = '';
    suggestions: ProductDto[] = [];
    /** Keyboard highlight index in menu search dropdown (-1 = none). */
    suggestionHighlightIndex = -1;
    customers: CustomerDto[] = [];
    cart: PosCartLine[] = [];

    customerId: number | null = null;
    notes = '';
    discountAmount = 0;
    discountPercent = 0;
    taxPercent = 0;

    /** Staff delivery address + zone (takeaway / retail delivery). */
    deliveryAreas: DeliveryAreaDto[] = [];
    selectedDeliveryAreaId: number | null = null;
    deliveryAddress = '';
    deliveryLandmark = '';
    savedAddresses: CustomerSavedAddressDto[] = [];
    selectedSavedAddressId: number | null = null;
    loadingSavedAddresses = false;

    // Restaurant Mode
    isRestaurantMode = false;
    serviceChargePercent = 0;
    orderType: 'dineIn' | 'takeaway' = 'dineIn';
    /** When set (e.g. Foodpanda), order runs as takeaway with that partner as customer. */
    thirdPartyChannel: PosThirdPartyChannel = null;
    /**
     * Exclusive selected chip for the order-type toggle.
     * Do not derive highlight from orderType alone — Foodpanda also uses takeaway.
     */
    selectedChannel: PosOrderChannelUi = 'dineIn';
    tables: RestaurantTableDto[] = [];
    selectedTable: RestaurantTableDto | null = null;
    tableDialogVisible = false;
    loadingTables = false;
    pendingSendToTable = false;

    // Car bay (permanent Car 1Ã¢â‚¬â€œN slots) + vehicle plate on select
    selectedCarOrder: RestaurantOrderDto | null = null;
    selectedCarName = '';
    selectedCarNumber = '';
    selectedCarLocation = '';
    vehicleDialogVisible = false;
    pendingVehicleTable: RestaurantTableDto | null = null;
    pendingVehicleNumber = '';
    creatingCarBay = false;

    get isCarService(): boolean {
        if (!this.isRestaurantMode || this.orderType !== 'dineIn') {
            return false;
        }
        return (
            !!this.selectedCarNumber ||
            !!this.selectedCarOrder ||
            this.isCarBay(this.selectedTable)
        );
    }

    isCarBay(table: RestaurantTableDto | null | undefined): boolean {
        if (!table) return false;
        if (table.isCarBay) return true;
        const area = (table.area || '').trim().toLowerCase();
        if (area === 'car parking' || area.includes('car park')) return true;
        return /^car[\s-]/i.test((table.tableNo || '').trim());
    }

    get diningTables(): RestaurantTableDto[] {
        return (this.tables || []).filter((t) => !this.isCarBay(t));
    }

    get carBays(): RestaurantTableDto[] {
        return (this.tables || []).filter((t) => this.isCarBay(t));
    }

    get carServiceLabel(): string {
        if (!this.isCarService) return '';
        const bay = this.selectedCarName || this.selectedTable?.tableNo || 'Car';
        return this.selectedCarNumber ? `${bay} • ${this.selectedCarNumber}` : bay;
    }

    get invoiceBranch(): BranchDto | null {
        return this.branchContext.getCurrentBranch();
    }

    get invoiceBranchName(): string {
        return this.invoiceBranch?.name || 'SmartPOS Restaurant';
    }

    get invoiceBranchAddress(): string {
        return this.invoiceBranch?.invoiceAddress || '';
    }

    get invoiceBranchPhone(): string {
        return this.invoiceBranch?.invoiceContactPhone || '';
    }

    get invoiceBranchFooter(): string {
        const branch = this.invoiceBranch;
        if (branch?.invoiceFooter) {
            return branch.invoiceFooter;
        }
        if (this.invoiceBranchPhone) {
            return `For any Complaint & Suggestions Please Contact us @ ${this.invoiceBranchPhone}`;
        }
        return 'Thank you for dining with us!';
    }

    get guestOrderTypeLabel(): string {
        if (this.isCarService) {
            return 'Car Service';
        }
        if (this.thirdPartyChannel === 'foodpanda') {
            return 'Third Party';
        }
        if (this.isOnlineOrder) {
            return 'Online';
        }
        if (this.orderType === 'takeaway') {
            return 'Take-Away';
        }
        return 'Dine-In';
    }

    get isFoodpandaOrder(): boolean {
        return this.selectedChannel === 'foodpanda' || this.thirdPartyChannel === 'foodpanda';
    }

    get isOnlineOrder(): boolean {
        return this.selectedChannel === 'online';
    }

    private syncActiveOrderMeta(order: {
        id?: number | null;
        orderNo?: string;
        tokenNo?: number | null;
    } | null): void {
        if (!order?.id) {
            this.activeOrderId = null;
            this.activeOrderNo = '';
            this.activeTokenNo = null;
            return;
        }
        this.activeOrderId = order.id;
        this.activeOrderNo = order.orderNo || '';
        this.activeTokenNo = order.tokenNo ?? null;
    }

    // Active Running Order (Hold & Tabs)
    activeOrderId: number | null = null;
    activeOrderNo = '';
    activeTokenNo: number | null = null;
    waiterName = '';
    savingOrder = false;
    sendingKOT = false;
    printingBill = false;

    /** Active restaurant orders for the branch (used for waiter "My Orders" panel). */
    activeRestaurantOrders: RestaurantOrderDto[] = [];
    loadingMyAssignedOrders = false;
    currentWaiterStaffId: number | null = null;
    /** Ticks every second so My Assigned Orders shows a live elapsed timer. */
    assignedOrdersNowMs = Date.now();
    private assignedOrdersClockTimer: ReturnType<typeof setInterval> | null = null;

    // KOT Modal & Thermal print preview
    kotDialogVisible = false;
    kotResult: SendToKitchenResultDto | null = null;

    // Guest Bill Check Dialog
    guestBillDialogVisible = false;

    // Split Bill Dialog
    splitBillDialogVisible = false;
    splitCount = 2;

    // Product Modifiers
    modifierDialogVisible = false;
    selectedProductForModifiers: ProductDto | null = null;
    availableModifierGroups: ProductModifierGroupDto[] = [];
    selectedModifierOptions: Record<number, number[]> = {};
    pendingKitchenNote = '';
    todayDate = new Date();

    // Deal Selection & Customization (Restaurant only)
    dealCustomiseDialogVisible = false;
    loadingDealCustomise = false;
    selectedDealProduct: ProductDto | null = null;
    currentDeal: DealDto | null = null;
    dealCustomiseItems: DealCustomiseItem[] = [];
    dealKitchenNote = '';

    // Cart Deal Details modal
    viewCartDealDialogVisible = false;
    selectedCartDealLine: PosCartLine | null = null;

    // Dine-In Waiter Assignment (Restaurant only)
    waiters: StaffDto[] = [];
    assignWaiterDialogVisible = false;
    pendingSelectedTable: RestaurantTableDto | null = null;
    selectedWaiterId: number | null = null;
    assigningWaiter = false;
    loadingWaiters = false;

    // Quick Add Table form
    newTableNo = '';
    newTableArea = 'Main Hall';
    newTableCapacity = 4;
    creatingTable = false;

    paymentDialogVisible = false;
    completedReceiptVisible = false;
    completedReceipt: any = null;
    paidAmount = 0;
    /** Table/car id while admin prints or settles from the tables dialog. */
    tableBillBusyId: number | null = null;

    unpaidTakeawayOrders: UnpaidTakeawayOrderDto[] = [];
    unpaidTakeawayDialogVisible = false;
    orderToCancel: UnpaidTakeawayOrderDto | null = null;
    cancelTakeawayDialogVisible = false;
    cancellingTakeawayOrder = false;
    private alarmOrdersSub?: Subscription;

    private searchTimer: ReturnType<typeof setTimeout> | null = null;

    constructor(
        private saleService: SaleService,
        private productService: ProductService,
        private restaurantTableService: RestaurantTableService,
        private restaurantOrderService: RestaurantOrderService,
        private productModifierService: ProductModifierService,
        private branchContext: BranchContextService,
        private branchService: BranchService,
        private staffService: StaffService,
        private dealService: DealService,
        private authService: AuthService,
        private route: ActivatedRoute,
        private router: Router,
        public thermalPrintService: ThermalPrintService,
        public takeawayAlarmService: RestaurantTakeawayAlarmService,
        private customerService: CustomerService,
        private customerOrderService: CustomerOrderService,
        private deliveryAreaService: DeliveryAreaService,
        private messageService: MessageService,
        private cdr: ChangeDetectorRef
    ) {}

    get selectedDeliveryArea(): DeliveryAreaDto | null {
        return this.deliveryAreas.find((a) => a.id === this.selectedDeliveryAreaId) || null;
    }

    get deliveryCharges(): number {
        return this.selectedDeliveryArea?.deliveryFee ?? 0;
    }

    get showDeliveryAddressPanel(): boolean {
        return (
            !this.isRestaurantMode ||
            this.orderType === 'takeaway' ||
            this.isOnlineOrder
        );
    }

    get isWaiter(): boolean {
        return this.isRestaurantMode && this.authService.isWaiter();
    }

    private get currentWaiterNameKey(): string {
        return (this.authService.getPrinterIdentity().name || this.waiterName || '')
            .trim()
            .toLowerCase();
    }

    isAssignedToCurrentWaiter(tableOrOrder: {
        waiterId?: number | null;
        waiterName?: string | null;
    }): boolean {
        if (
            this.currentWaiterStaffId &&
            tableOrOrder.waiterId &&
            tableOrOrder.waiterId === this.currentWaiterStaffId
        ) {
            return true;
        }
        const key = this.currentWaiterNameKey;
        if (!key) {
            return false;
        }
        return (tableOrOrder.waiterName || '').trim().toLowerCase() === key;
    }

    /** Occupied/billed tables & car bays currently assigned to this waiter. */
    get myAssignedTables(): RestaurantTableDto[] {
        if (!this.isWaiter) {
            return [];
        }
        return (this.tables || []).filter(
            (t) =>
                (t.status === 1 || t.status === 2) &&
                this.isAssignedToCurrentWaiter(t)
        );
    }

    /** Open/billed orders currently tagged to this waiter. */
    get myAssignedOrders(): RestaurantOrderDto[] {
        if (!this.isWaiter) {
            return [];
        }
        return (this.activeRestaurantOrders || []).filter((o) =>
            this.isAssignedToCurrentWaiter(o)
        );
    }

    getMyAssignedOrderForTable(table: RestaurantTableDto): RestaurantOrderDto | null {
        if (!table) {
            return null;
        }
        const byTableId = this.myAssignedOrders.find(
            (o) => o.tableId === table.id
        );
        if (byTableId) {
            return byTableId;
        }
        if (table.currentOrderId) {
            return (
                this.myAssignedOrders.find((o) => o.id === table.currentOrderId) ||
                null
            );
        }
        const tableNo = (table.tableNo || '').trim().toLowerCase();
        if (!tableNo) {
            return null;
        }
        return (
            this.myAssignedOrders.find(
                (o) => (o.tableNo || '').trim().toLowerCase() === tableNo
            ) || null
        );
    }

    getMyAssignedOrderStatusLabel(table: RestaurantTableDto): string {
        const order = this.getMyAssignedOrderForTable(table);
        if (!order) {
            if (table?.status === 2) {
                return 'Billed';
            }
            return table?.statusName || (table?.status === 1 ? 'Occupied' : 'Open');
        }
        if (order.status === RestaurantOrderStatuses.Billed) {
            return 'Billed';
        }
        if (order.kitchenStatus === RestaurantKitchenStatuses.ReadyToPick) {
            return 'Ready to Pick';
        }
        if (order.kitchenStatus === RestaurantKitchenStatuses.InProgress) {
            return 'In Progress';
        }
        return order.statusName || 'Open';
    }

    getMyAssignedOrderStatusClass(table: RestaurantTableDto): string {
        const label = this.getMyAssignedOrderStatusLabel(table).toLowerCase();
        if (label.includes('ready')) {
            return 'is-ready';
        }
        if (label.includes('billed')) {
            return 'is-billed';
        }
        if (label.includes('progress')) {
            return 'is-progress';
        }
        return 'is-open';
    }

    getMyAssignedOrderElapsed(table: RestaurantTableDto): string {
        void this.assignedOrdersNowMs; // keep template ticking every second
        const start = this.getMyAssignedOrderStartMs(table);
        if (!start) {
            return '';
        }
        const totalSec = Math.max(
            0,
            Math.floor((this.assignedOrdersNowMs - start) / 1000)
        );
        const mins = Math.floor(totalSec / 60);
        const secs = totalSec % 60;
        if (mins < 1) {
            return `${secs}s`;
        }
        if (mins < 60) {
            return `${mins}m ${secs.toString().padStart(2, '0')}s`;
        }
        const h = Math.floor(mins / 60);
        const m = mins % 60;
        return `${h}h ${m}m`;
    }

    isMyAssignedOrderLate(table: RestaurantTableDto): boolean {
        const start = this.getMyAssignedOrderStartMs(table);
        if (!start) {
            return false;
        }
        return this.assignedOrdersNowMs - start > 20 * 60 * 1000;
    }

    private getMyAssignedOrderStartMs(table: RestaurantTableDto): number {
        const order = this.getMyAssignedOrderForTable(table);
        if (!order) {
            return 0;
        }
        const candidates: number[] = [];
        const created = this.parseOrderDateMs(
            (order as any).creationTime ?? (order as any).CreationTime
        );
        if (created) {
            candidates.push(created);
        }
        for (const line of order.lines || []) {
            const sent = this.parseOrderDateMs(
                (line as any).sentToKitchenTime ?? (line as any).SentToKitchenTime
            );
            if (sent) {
                candidates.push(sent);
            }
        }
        if (!candidates.length) {
            return 0;
        }
        // Earliest timestamp = when the order/session actually started.
        return Math.min(...candidates);
    }

    /**
     * API datetimes are often server wall-clock values serialized with a trailing Z.
     * That can place "create" slightly in the future and freeze the timer at 0s.
     * Prefer the most recent parse that is still at-or-before now.
     */
    private parseOrderDateMs(value: any): number {
        if (value == null || value === '') {
            return 0;
        }
        if (value instanceof Date) {
            const t = value.getTime();
            return Number.isNaN(t) ? 0 : t;
        }
        const raw = String(value).trim();
        if (!raw) {
            return 0;
        }
        const asIs = Date.parse(raw);
        const localWall = Date.parse(raw.replace(/([zZ]|[+-]\d{2}:\d{2})$/, ''));
        const asUtc = /([zZ]|[+-]\d{2}:\d{2})$/.test(raw)
            ? asIs
            : Date.parse(raw.includes('T') ? `${raw}Z` : `${raw}T00:00:00Z`);

        const now = this.assignedOrdersNowMs || Date.now();
        const options: number[] = [asIs, localWall, asUtc].filter(
            (t) => !!t && !Number.isNaN(t)
        );
        // If a Z/UTC parse sits in the future, try common host offsets (UTC+1 … +5).
        if (!Number.isNaN(asIs) && asIs > now + 2000) {
            for (let h = 1; h <= 5; h++) {
                options.push(asIs - h * 3600_000);
            }
        }
        if (!options.length) {
            return 0;
        }
        const notFuture = options.filter((t) => t <= now + 2000);
        if (notFuture.length) {
            return Math.max(...notFuture);
        }
        return Math.min(...options);
    }

    private ensureAssignedOrdersClock(): void {
        if (this.assignedOrdersClockTimer) {
            return;
        }
        this.assignedOrdersNowMs = Date.now();
        this.assignedOrdersClockTimer = setInterval(() => {
            this.assignedOrdersNowMs = Date.now();
            this.cdr.detectChanges();
        }, 1000);
    }

    private stopAssignedOrdersClock(): void {
        if (this.assignedOrdersClockTimer) {
            clearInterval(this.assignedOrdersClockTimer);
            this.assignedOrdersClockTimer = null;
        }
    }

    isAutoAssignWaiter(): boolean {
        if (!this.isRestaurantMode) return true;
        const branch = this.branchContext.getCurrentBranch();
        if (!branch) return true;
        return branch.autoAssignWaiterToTables !== false;
    }

    get currentPrinterText(): string {
        return this.authService.getPrinterIdentity().text;
    }

    /** Primary kitchen send label — dine-in/takeaway both dispatch a KOT ticket. */
    get sendKotButtonLabel(): string {
        if (this.isCarService) {
            return '🚗 Send to Car (KOT)';
        }
        return '🍳 Send to KOT';
    }

    get sendKotButtonTitle(): string {
        if (this.isCarService) {
            return 'Send car order to kitchen KOT';
        }
        if (this.orderType === 'takeaway') {
            return 'Send takeaway order to kitchen KOT';
        }
        return 'Send dine-in order to kitchen KOT';
    }

    get currentPrinterTime(): Date {
        return new Date();
    }

    get subTotal(): number {
        return this.cart.reduce(
            (sum, line) => sum + line.quantity * line.unitPrice,
            0
        );
    }

    get computedDiscount(): number {
        let discount = this.discountAmount || 0;
        if ((this.discountPercent || 0) > 0 && discount <= 0) {
            discount =
                Math.round(
                    ((this.subTotal * this.discountPercent) / 100) * 100
                ) / 100;
        }
        if (discount < 0) {
            discount = 0;
        }
        if (discount > this.subTotal) {
            discount = this.subTotal;
        }
        return discount;
    }

    get taxAmount(): number {
        const taxable = this.subTotal - this.computedDiscount;
        return (
            Math.round(((taxable * (this.taxPercent || 0)) / 100) * 100) / 100
        );
    }

    get serviceChargeAmount(): number {
        if (
            !this.isRestaurantMode ||
            this.orderType !== 'dineIn' ||
            !this.serviceChargePercent
        ) {
            return 0;
        }
        const base = this.subTotal - this.computedDiscount;
        return (
            Math.round(((base * this.serviceChargePercent) / 100) * 100) / 100
        );
    }

    get grandTotal(): number {
        const delivery =
            this.showDeliveryAddressPanel && this.deliveryAddress.trim()
                ? this.deliveryCharges
                : 0;
        return (
            Math.round(
                (this.subTotal -
                    this.computedDiscount +
                    this.taxAmount +
                    this.serviceChargeAmount +
                    delivery) *
                    100
            ) / 100
        );
    }

    get changeToReturn(): number {
        const paid = Number(this.paidAmount) || 0;
        return Math.max(0, Math.round((paid - this.grandTotal) * 100) / 100);
    }

    get remainingDue(): number {
        const paid = Number(this.paidAmount) || 0;
        return Math.max(0, Math.round((this.grandTotal - paid) * 100) / 100);
    }

    topProducts: ProductDto[] = [];
    loadingTopProducts = false;

    /** Restaurant menu browser (tap-to-add by category). */
    menuProducts: ProductDto[] = [];
    /** Empty = All categories. Multiple chips can be active at once. */
    selectedMenuCategoryIds: number[] = [];
    loadingMenu = false;
    /** Left panel: Menu products vs current order/cart lines. */
    posLeftTab: 'menu' | 'order' = 'menu';

    private lastLoadedBranchId: number | null = null;
    private posInitDone = false;

    get menuCategories(): { id: number; name: string; count: number }[] {
        const map = new Map<number, { id: number; name: string; count: number }>();
        for (const p of this.menuProducts) {
            if (!p.categoryId) {
                continue;
            }
            const existing = map.get(p.categoryId);
            if (existing) {
                existing.count += 1;
            } else {
                map.set(p.categoryId, {
                    id: p.categoryId,
                    name: (p.categoryName || 'Other').trim() || 'Other',
                    count: 1,
                });
            }
        }
        return Array.from(map.values()).sort((a, b) => {
            const aDeal = a.name.toLowerCase() === 'deals' ? 0 : 1;
            const bDeal = b.name.toLowerCase() === 'deals' ? 0 : 1;
            if (aDeal !== bDeal) {
                return aDeal - bDeal;
            }
            return a.name.localeCompare(b.name, undefined, { sensitivity: 'base' });
        });
    }

    get filteredMenuProducts(): ProductDto[] {
        let list = this.menuProducts;
        if (this.selectedMenuCategoryIds.length > 0) {
            const selected = new Set(this.selectedMenuCategoryIds);
            list = list.filter((p) => p.categoryId != null && selected.has(p.categoryId));
        }
        const q = (this.searchText || '').trim().toLowerCase();
        if (q) {
            list = list.filter(
                (p) =>
                    (p.name || '').toLowerCase().includes(q) ||
                    (p.barcode || '').toLowerCase().includes(q) ||
                    (p.categoryName || '').toLowerCase().includes(q)
            );
        }
        return list;
    }

    isMenuCategorySelected(categoryId: number | null): boolean {
        if (categoryId == null) {
            return this.selectedMenuCategoryIds.length === 0;
        }
        return this.selectedMenuCategoryIds.includes(categoryId);
    }

    ngOnInit(): void {
        void this.initPosForBranch();

        this.branchSub = this.branchContext.currentBranch$.subscribe((branch) => {
            if (!this.posInitDone || !branch?.id) {
                return;
            }
            if (branch.id === this.lastLoadedBranchId) {
                return;
            }
            void this.reloadForBranch(branch.id);
        });

        this.route.queryParams.subscribe(async (params) => {
            if (!this.isRestaurantMode) {
                return;
            }
            const orderId = params['restaurantOrderId'] || params['orderId'];
            if (orderId) {
                await this.loadOrderById(Number(orderId));
            }
        });
    }

    private async initPosForBranch(): Promise<void> {
        try {
            await this.branchContext.ensureLoaded();
        } catch (err) {
            console.error('Failed to load branch context for POS', err);
        }
        this.posInitDone = true;
        const branchId = this.branchContext.getBranchId();
        if (branchId) {
            await this.reloadForBranch(branchId);
        } else {
            this.applyBranchPricing();
            this.loadTopProducts();
            this.loadPosCustomers();
            if (this.isRestaurantMode) {
                this.loadMenuProducts();
            }
        }
    }

    private async reloadForBranch(branchId: number): Promise<void> {
        this.lastLoadedBranchId = branchId;
        this.applyBranchPricing();
        this.loadTopProducts();
        this.loadPosCustomers();
        void this.loadDeliveryAreasForPos();

        if (this.isRestaurantMode) {
            this.loadMenuProducts();
            if (this.isWaiter) {
                this.waiterName = this.authService.getPrinterIdentity().name;
                await this.loadWaiters(branchId);
                this.resolveCurrentWaiterStaffId();
                this.loadTables(branchId);
                this.ensureAssignedOrdersClock();
                void this.loadMyAssignedOrders(branchId);
            } else if (this.takeawayAlarmService && !this.alarmOrdersSub) {
                this.alarmOrdersSub = this.takeawayAlarmService.unpaidOrders$.subscribe((orders) => {
                    this.unpaidTakeawayOrders = orders || [];
                });
            }
            this.loadUnpaidTakeaways();
        } else {
            this.menuProducts = [];
            this.selectedMenuCategoryIds = [];
            this.activeRestaurantOrders = [];
        }
    }

    private resolveCurrentWaiterStaffId(): void {
        const user = this.authService.getUserInfo();
        const userId = user?.id ?? null;
        const nameKey = this.currentWaiterNameKey;
        const byUser = userId
            ? this.waiters.find((w) => w.userId === userId)
            : null;
        const byName = nameKey
            ? this.waiters.find(
                  (w) => (w.name || '').trim().toLowerCase() === nameKey
              )
            : null;
        this.currentWaiterStaffId = byUser?.id ?? byName?.id ?? null;
    }

    async loadMyAssignedOrders(branchId?: number): Promise<void> {
        if (!this.isWaiter) {
            this.activeRestaurantOrders = [];
            this.stopAssignedOrdersClock();
            return;
        }
        const bId = branchId || this.branchContext.getBranchId();
        if (!bId) {
            return;
        }
        this.ensureAssignedOrdersClock();
        this.loadingMyAssignedOrders = true;
        try {
            const list =
                (await this.restaurantOrderService.getActiveOrders(bId)) || [];
            this.activeRestaurantOrders = list.map((o) => this.normalizeAssignedOrder(o));
        } catch (err) {
            console.error('Failed to load waiter assigned orders', err);
            this.activeRestaurantOrders = [];
        } finally {
            this.loadingMyAssignedOrders = false;
            this.cdr.detectChanges();
        }
    }

    private normalizeAssignedOrder(item: any): RestaurantOrderDto {
        const lines = item?.lines ?? item?.Lines ?? [];
        return {
            ...(item || {}),
            id: item?.id ?? item?.Id,
            branchId: item?.branchId ?? item?.BranchId,
            orderNo: item?.orderNo ?? item?.OrderNo,
            tokenNo: item?.tokenNo ?? item?.TokenNo,
            tableId: item?.tableId ?? item?.TableId,
            tableNo: item?.tableNo ?? item?.TableNo,
            orderType: item?.orderType ?? item?.OrderType,
            orderTypeName: item?.orderTypeName ?? item?.OrderTypeName,
            status: item?.status ?? item?.Status,
            statusName: item?.statusName ?? item?.StatusName,
            kitchenStatus: item?.kitchenStatus ?? item?.KitchenStatus,
            kitchenStatusName: item?.kitchenStatusName ?? item?.KitchenStatusName,
            waiterName: item?.waiterName ?? item?.WaiterName,
            grandTotal: Number(item?.grandTotal ?? item?.GrandTotal ?? 0),
            creationTime: item?.creationTime ?? item?.CreationTime,
            billedTime: item?.billedTime ?? item?.BilledTime,
            lines: (Array.isArray(lines) ? lines : []).map((line: any) => ({
                ...(line || {}),
                id: line?.id ?? line?.Id,
                orderId: line?.orderId ?? line?.OrderId,
                productId: line?.productId ?? line?.ProductId,
                productName: line?.productName ?? line?.ProductName,
                quantity: Number(line?.quantity ?? line?.Quantity ?? 0),
                unitPrice: Number(line?.unitPrice ?? line?.UnitPrice ?? 0),
                lineTotal: Number(line?.lineTotal ?? line?.LineTotal ?? 0),
                sentToKitchenTime:
                    line?.sentToKitchenTime ?? line?.SentToKitchenTime,
                isSentToKitchen:
                    line?.isSentToKitchen ?? line?.IsSentToKitchen ?? false,
            })),
        } as RestaurantOrderDto;
    }

    async openAssignedTable(table: RestaurantTableDto): Promise<void> {
        if (!table) {
            return;
        }
        await this.onTableClicked(table);
    }

    private loadPosCustomers(): void {
        this.saleService
            .getPosCustomers()
            .then((customers) => {
                this.customers = (customers || []).map((c) => ({
                    id: c.id,
                    name: c.name,
                    customerType: c.customerType,
                })) as CustomerDto[];
                const walkIn = this.customers.find((c) =>
                    (c.name || '').toLowerCase().includes('walk')
                );
                this.customerId = walkIn?.id || this.customers[0]?.id || null;
                this.focusSearch();
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to load customers',
                });
            });
    }

    isGroceryProduct(product?: ProductDto | null): boolean {
        if (!product) return false;
        return (
            (product.barcode || '').startsWith('GROCERY-') ||
            (product.name || '').trim().toLowerCase() === 'grocery' ||
            (product.categoryName || '').trim().toLowerCase() === 'permanent grocery'
        );
    }

    isDealProduct(product?: ProductDto | null): boolean {
        if (!product) return false;
        return !!(
            product.isDeal ||
            product.dealId ||
            (product.categoryName || '').trim().toLowerCase() === 'deals' ||
            (product.name || '').trim().toLowerCase().startsWith('[deal]') ||
            (product.barcode || '').startsWith('DEAL-')
        );
    }

    isDealCartLine(line?: any): boolean {
        return this.isDealItemOrModifiers(line);
    }

    isDealItemOrModifiers(item?: any): boolean {
        if (!item) return false;
        if (item.isDeal || item.dealId || item.dealSummary) return true;
        const name = (item.productName || item.name || '').toLowerCase();
        if (name.startsWith('[deal]') || name.includes('deal')) return true;
        const mods = this.parseModifiers(item.selectedModifiers);
        if (mods.some((m: string) => typeof m === 'string' && m.startsWith('DEAL:'))) return true;
        if (item.productId || item.productName) {
            const cartMatch = this.cart.find(
                (c) =>
                    (item.productId && c.productId === item.productId) ||
                    (item.productName && c.productName === item.productName)
            );
            if (cartMatch && (cartMatch.isDeal || cartMatch.dealId || cartMatch.dealSummary)) {
                return true;
            }
        }
        return false;
    }

    getDealItemsFromLine(line: any): string[] {
        if (!line) return [];
        let summary: string | undefined = line.dealSummary;
        if (!summary) {
            const mods = this.parseModifiers(line.selectedModifiers);
            const dealMod = mods.find((m: string) => typeof m === 'string' && m.startsWith('DEAL:'));
            if (dealMod) {
                summary = dealMod.substring(5).trim();
            }
        }
        if (!summary && (line.productId || line.productName)) {
            const cartMatch = this.cart.find(
                (c) =>
                    (line.productId && c.productId === line.productId) ||
                    (line.productName && c.productName === line.productName)
            );
            if (cartMatch && cartMatch.dealSummary) {
                summary = cartMatch.dealSummary;
            }
        }
        if (!summary) return [];
        const parts = summary.includes(' • ')
            ? summary.split(' • ')
            : summary.includes(' â€¢ ')
              ? summary.split(' â€¢ ')
              : summary.split(',');
        return parts.map((s: string) => s.trim()).filter(Boolean);
    }

    getExtraModifiersFromLine(line: any): string[] {
        if (!line || !line.selectedModifiers) return [];
        const mods = this.parseModifiers(line.selectedModifiers);
        return mods.filter((m: string) => typeof m === 'string' && !m.startsWith('DEAL:'));
    }

    loadTopProducts(): void {
        this.loadingTopProducts = true;
        this.saleService
            .getTopSellingProducts(5)
            .then((products) => {
                this.topProducts = (products || []).filter((p) => !this.isGroceryProduct(p));
            })
            .catch((err) => {
                console.error('Failed to load top products:', err);
                this.topProducts = [];
            })
            .finally(() => {
                this.loadingTopProducts = false;
            });
    }

    loadMenuProducts(): void {
        this.loadingMenu = true;
        this.saleService
            .getPosProducts()
            .then((products) => {
                this.menuProducts = (products || [])
                    .filter((p) => !this.isGroceryProduct(p))
                    .sort((a, b) => (a.name || '').localeCompare(b.name || ''));
                const validIds = new Set(this.menuCategories.map((c) => c.id));
                this.selectedMenuCategoryIds = this.selectedMenuCategoryIds.filter((id) =>
                    validIds.has(id)
                );
            })
            .catch((err) => {
                console.error('Failed to load menu products:', err);
                this.menuProducts = [];
                this.messageService.add({
                    severity: 'warn',
                    summary: 'Menu',
                    detail: err?.message || 'Failed to load products for menu',
                });
            })
            .finally(() => {
                this.loadingMenu = false;
            });
    }

    selectMenuCategory(categoryId: number | null): void {
        if (categoryId == null) {
            this.selectedMenuCategoryIds = [];
            return;
        }

        const idx = this.selectedMenuCategoryIds.indexOf(categoryId);
        if (idx >= 0) {
            this.selectedMenuCategoryIds = this.selectedMenuCategoryIds.filter((id) => id !== categoryId);
        } else {
            this.selectedMenuCategoryIds = [...this.selectedMenuCategoryIds, categoryId];
        }
    }

    scrollMenuCategories(direction: -1 | 1): void {
        const track = this.menuCategoriesTrack?.nativeElement;
        if (!track) {
            return;
        }
        const step = Math.max(180, Math.floor(track.clientWidth * 0.7));
        track.scrollBy({ left: direction * step, behavior: 'smooth' });
    }

    getMenuProductImage(product: ProductDto): string {
        return this.productService.getImageUrl(product?.imagePath);
    }

    getMenuProductInitials(product: ProductDto): string {
        return this.productService.getProductInitials(product?.name);
    }

    private applyBranchPricing(): void {
        const branch = this.branchContext.getCurrentBranch();
        this.taxPercent = branch?.taxPercent ?? 0;
        this.discountPercent = branch?.discountPercent ?? 0;
        this.discountAmount = branch?.discountAmount ?? 0;
        this.isRestaurantMode = branch?.businessType === 2;
        this.serviceChargePercent = branch?.serviceChargePercent ?? 0;

        if (this.isRestaurantMode && branch?.id) {
            this.loadTables(branch.id);
            this.loadWaiters(branch.id);
        }
    }

    async loadWaiters(branchId?: number): Promise<void> {
        const bId = branchId || this.branchContext.getBranchId();
        if (!bId) return;
        this.loadingWaiters = true;
        try {
            const res = await this.staffService.getAll({
                branchId: bId,
                isActive: true,
                maxResultCount: 100,
            });
            this.waiters = (res.items || []).filter(
                (s) =>
                    s.designation &&
                    s.designation.trim().toLowerCase() === 'waiter'
            );
        } catch (err) {
            console.error('Failed to load waiters', err);
        } finally {
            this.loadingWaiters = false;
        }
    }

    /** Returns IDs of waiters currently serving an occupied/billed table (excluding the currently selected table). */
    getBusyWaiterIds(): Set<number> {
        if (!this.isRestaurantMode) return new Set<number>();
        const busyIds = new Set<number>();
        for (const t of this.tables) {
            // Status 1 = Occupied, 2 = Billed Ã¢â‚¬â€ both mean an active order is running
            if ((t.status === 1 || t.status === 2) && t.waiterId && t.id !== this.selectedTable?.id) {
                busyIds.add(t.waiterId);
            }
        }
        return busyIds;
    }

    /** Waiters that are NOT currently serving another active table. */
    get availableWaiters(): StaffDto[] {
        if (!this.isRestaurantMode) return this.waiters;
        const busyIds = this.getBusyWaiterIds();
        return this.waiters.filter((w) => !busyIds.has(w.id));
    }


    loadTables(branchId?: number): void {
        const bId = branchId || this.branchContext.getBranchId();
        if (!bId) return;
        this.loadingTables = true;
        this.restaurantTableService
            .getTables(bId)
            .then((tables) => {
                this.tables = tables || [];
                if (this.isWaiter) {
                    void this.loadMyAssignedOrders(bId);
                }
            })
            .catch((err) => {
                console.error('Failed to load tables', err);
                this.tables = [];
                this.messageService.add({
                    severity: 'error',
                    summary: 'Tables',
                    detail: err?.message || 'Failed to load tables / car bays. Restart API after DB update if needed.',
                    life: 5000,
                });
            })
            .finally(() => {
                this.loadingTables = false;
            });
    }

    onOrderTypeChange(event?: any): void {
        if (this.orderType === 'dineIn') {
            if (!this.selectedTable && !this.isCarService) {
                this.openTableDialog(false);
            }
        }
    }

    /**
     * Dine-In / Takeaway / Foodpanda (third-party) / Online delivery.
     * UI highlight uses selectedChannel only (not orderType), because Foodpanda
     * and Online still use takeaway kitchen/settle flows under the hood.
     */
    async selectOrderChannel(channel: PosOrderChannelUi): Promise<void> {
        // Set highlight synchronously first so Takeaway cannot keep .active.
        this.selectedChannel = channel;
        this.cdr.detectChanges();

        if (channel === 'foodpanda') {
            this.orderType = 'takeaway';
            this.thirdPartyChannel = 'foodpanda';
            this.clearTableSelection();
            await this.ensureThirdPartyCustomer(THIRD_PARTY_CUSTOMER_NAME);
            // Re-assert after async work in case anything touched order flags.
            this.selectedChannel = 'foodpanda';
            this.thirdPartyChannel = 'foodpanda';
            this.orderType = 'takeaway';
            this.cdr.detectChanges();
            return;
        }

        if (channel === 'online') {
            const wasFoodpanda = this.thirdPartyChannel === 'foodpanda';
            this.thirdPartyChannel = null;
            this.orderType = 'takeaway';
            this.selectedChannel = 'online';
            this.clearTableSelection();
            if (wasFoodpanda) {
                this.selectWalkInCustomer();
            }
            void this.loadDeliveryAreasForPos();
            this.cdr.detectChanges();
            return;
        }

        const wasFoodpanda = this.thirdPartyChannel === 'foodpanda';
        this.thirdPartyChannel = null;
        this.orderType = channel;
        this.selectedChannel = channel;

        if (wasFoodpanda) {
            this.selectWalkInCustomer();
        }

        if (channel === 'dineIn') {
            this.onOrderTypeChange();
        }
        this.cdr.detectChanges();
    }

    /** Keep the chip highlight in sync when order type is set outside the toggle. */
    private syncSelectedChannelFromOrderState(): void {
        if (this.selectedChannel === 'online') {
            return;
        }
        if (this.thirdPartyChannel === 'foodpanda' && this.orderType === 'takeaway') {
            this.selectedChannel = 'foodpanda';
            return;
        }
        this.selectedChannel = this.orderType === 'takeaway' ? 'takeaway' : 'dineIn';
    }

    private selectWalkInCustomer(): void {
        const walkIn = this.customers.find((c) =>
            (c.name || '').toLowerCase().includes('walk')
        );
        this.customerId = walkIn?.id || this.customers[0]?.id || null;
        this.onCustomerSelected();
    }

    /** In-flight ensure so rapid Foodpanda clicks share one lookup/create. */
    private thirdPartyEnsurePromise: Promise<void> | null = null;

    private findThirdPartyCustomer(name: string): CustomerDto | undefined {
        const key = (name || '').trim().toLowerCase();
        if (!key) {
            return undefined;
        }
        const matches = (this.customers || []).filter((c) => {
            const n = (c.name || '').trim().toLowerCase();
            return n === key || n.includes(key);
        });
        if (!matches.length) {
            return undefined;
        }
        const exact = matches.find((c) => (c.name || '').trim().toLowerCase() === key);
        // Prefer exact name, else oldest id so duplicates converge on one record.
        return exact || [...matches].sort((a, b) => a.id - b.id)[0];
    }

    /** Find existing third-party customer (API + local); create only if none exists. */
    private async ensureThirdPartyCustomer(name: string): Promise<void> {
        if (this.thirdPartyEnsurePromise) {
            return this.thirdPartyEnsurePromise;
        }
        this.thirdPartyEnsurePromise = this.ensureThirdPartyCustomerCore(name).finally(() => {
            this.thirdPartyEnsurePromise = null;
        });
        return this.thirdPartyEnsurePromise;
    }

    private async ensureThirdPartyCustomerCore(name: string): Promise<void> {
        const select = (c: CustomerDto) => {
            if (!this.customers.some((x) => x.id === c.id)) {
                this.customers = [
                    ...this.customers,
                    {
                        id: c.id,
                        name: c.name,
                        customerType: c.customerType,
                    } as CustomerDto,
                ];
            }
            this.customerId = c.id;
            this.onCustomerSelected();
        };

        let existing = this.findThirdPartyCustomer(name);
        if (existing) {
            select(existing);
            return;
        }

        try {
            const refreshed = await this.saleService.getPosCustomers(true);
            this.customers = (refreshed || []).map((c) => ({
                id: c.id,
                name: c.name,
                customerType: c.customerType,
            })) as CustomerDto[];
            existing = this.findThirdPartyCustomer(name);
            if (existing) {
                select(existing);
                return;
            }

            const searched = await this.customerService.getAll({
                keyword: name,
                skipCount: 0,
                maxResultCount: 50,
            });
            const fromApi = (searched.items || []).find((c) => {
                const n = (c.name || '').trim().toLowerCase();
                const key = name.trim().toLowerCase();
                return n === key || n.includes(key);
            });
            if (fromApi) {
                select(fromApi);
                return;
            }

            const created = await this.customerService.create({
                name,
                customerType: CustomerType.Direct,
                description: 'Third-party delivery partner (POS)',
            });
            select(created);
            this.messageService.add({
                severity: 'success',
                summary: name,
                detail: `${name} customer saved for third-party takeaway orders.`,
                life: 2500,
            });
        } catch (err: any) {
            try {
                const refreshed = await this.saleService.getPosCustomers(true);
                this.customers = (refreshed || []).map((c) => ({
                    id: c.id,
                    name: c.name,
                    customerType: c.customerType,
                })) as CustomerDto[];
                const again = this.findThirdPartyCustomer(name);
                if (again) {
                    select(again);
                    return;
                }
            } catch {
                /* fall through */
            }
            this.messageService.add({
                severity: 'error',
                summary: 'Customer',
                detail: err?.message || `Failed to save ${name} customer`,
            });
        }
    }

    private isThirdPartyCustomerName(name: string): boolean {
        const n = (name || '').trim().toLowerCase();
        return (
            n === THIRD_PARTY_CUSTOMER_NAME.toLowerCase() ||
            n === LEGACY_FOODPANDA_CUSTOMER_NAME.toLowerCase()
        );
    }

    private syncThirdPartyFromCustomer(): void {
        const selected = this.customers.find((c) => c.id === this.customerId);
        const name = (selected?.name || '').trim().toLowerCase();
        if (this.isThirdPartyCustomerName(name) && this.orderType === 'takeaway') {
            this.thirdPartyChannel = 'foodpanda';
            this.selectedChannel = 'foodpanda';
        } else if (this.thirdPartyChannel === 'foodpanda') {
            this.thirdPartyChannel = null;
            this.syncSelectedChannelFromOrderState();
        }
    }

    openTableDialog(forSend: boolean = false): void {
        this.pendingSendToTable = forSend;
        this.loadTables();
        if (!this.waiters.length) {
            this.loadWaiters();
        }
        this.tableDialogVisible = true;
    }

    /**
     * Park current dine-in/car order (if any), clear POS, open picker for another table/car.
     * Lets staff take multiple concurrent car/table orders without settling first.
     */
    async startNewOrder(): Promise<void> {
        if (!this.isRestaurantMode) return;

        const parked = await this.parkCurrentDineInOrderIfNeeded();
        if (!parked) return;

        this.clearTableSelection();
        this.orderType = 'dineIn';
        this.thirdPartyChannel = null;
        this.selectedChannel = 'dineIn';
        this.openTableDialog(false);
        this.messageService.add({
            severity: 'info',
            summary: 'New Order',
            detail: 'Select a table or car bay to start the next order.',
            life: 2500,
        });
    }

    /** Save open cart onto current table/car bay so it stays occupied while staff take another order. */
    private async parkCurrentDineInOrderIfNeeded(): Promise<boolean> {
        if (this.orderType !== 'dineIn') return true;
        if (!this.cart.length) return true;
        if (!this.selectedTable && !this.isCarService) return true;
        if (!this.activeOrderId) return true;

        const branchId = this.branchContext.getBranchId();
        if (!branchId) return false;

        try {
            const loc = this.buildOrderLocationFields(false);
            if (this.isCarService && !loc.carNumber) {
                this.messageService.add({
                    severity: 'warn',
                    summary: 'Vehicle Number Required',
                    detail: 'Current car order needs a plate number before starting another order.',
                    life: 3500,
                });
                return false;
            }

            const payload: CreateOrUpdateRestaurantOrderDto = {
                id: this.activeOrderId,
                branchId,
                ...loc,
                customerId: this.customerId,
                waiterName: this.waiterName || (this.isWaiter ? this.authService.getPrinterIdentity().name : 'Staff'),
                notes: this.notes,
                discountPercent: this.discountPercent || 0,
                discountAmount: this.discountAmount || 0,
                taxPercent: this.taxPercent || 0,
                serviceChargePercent: this.serviceChargePercent || 0,
                lines: this.cart.map((line) => ({
                    id: line.id,
                    productId: line.productId,
                    productName: line.productName,
                    quantity: line.quantity,
                    unitPrice: line.unitPrice,
                    kitchenNotes: line.kitchenNotes,
                    selectedModifiers: line.selectedModifiers,
                    isSentToKitchen: line.isSentToKitchen || false,
                    printedQuantity: line.printedQuantity || 0,
                })),
            };

            const saved = await this.restaurantOrderService.saveOrder(payload);
            this.syncActiveOrderMeta(saved);
            if (saved.lines?.length) {
                saved.lines.forEach((sl) => {
                    const cartLine = this.cart.find(
                        (cl) => cl.id === sl.id || (!cl.id && cl.productId === sl.productId)
                    );
                    if (cartLine) cartLine.id = sl.id;
                });
            }
            this.loadTables();
            this.messageService.add({
                severity: 'success',
                summary: 'Order Held',
                detail: this.isCarService
                    ? `${this.carServiceLabel} saved — pick another bay or table when ready.`
                    : `Table ${this.selectedTable?.tableNo} saved — pick another table or car when ready.`,
                life: 3000,
            });
            return true;
        } catch (err: any) {
            this.messageService.add({
                severity: 'error',
                summary: 'Could Not Hold Order',
                detail: err?.message || 'Save the current order before starting another.',
                life: 4000,
            });
            return false;
        }
    }

    onTableDialogHide(): void {
        // Only clear pendingSendToTable if dialog was closed without selecting a table
        if (!this.pendingSelectedTable) {
            this.pendingSendToTable = false;
        }
    }

    async onTableClicked(table: RestaurantTableDto): Promise<void> {
        // Switching away from a different active table/car Ã¢â‚¬â€ park current cart first
        const switchingAway =
            !!this.selectedTable &&
            this.selectedTable.id !== table.id &&
            this.cart.length > 0;
        if (switchingAway) {
            const parked = await this.parkCurrentDineInOrderIfNeeded();
            if (!parked) {
                this.tableDialogVisible = true;
                return;
            }
            // Clear local cart so the next bay/table starts clean (previous order stays on its bay)
            this.clearCart();
            this.syncActiveOrderMeta(null);
            this.selectedCarOrder = null;
            this.selectedCarName = '';
            this.selectedCarNumber = '';
            this.selectedCarLocation = '';
        }

        this.pendingSelectedTable = table;
        this.selectedWaiterId = table.waiterId || null;
        this.tableDialogVisible = false;

        // Occupied / billed: load existing order (plate already on order for car bays)
        if (table.status === 1 || table.status === 2) {
            if (this.isCarBay(table)) {
                this.selectedCarName = table.tableNo;
                this.selectedCarNumber = table.currentVehicleNumber || '';
                this.selectedCarLocation = table.area || 'Car Parking';
            }
            if (this.isWaiter) {
                this.waiterName = this.authService.getPrinterIdentity().name;
            }
            await this.selectTable(table);
            if (this.isWaiter) {
                void this.loadMyAssignedOrders();
            }
            return;
        }

        // Available car bay: ask for vehicle plate first
        if (this.isCarBay(table)) {
            this.pendingVehicleTable = table;
            this.pendingVehicleNumber = '';
            this.vehicleDialogVisible = true;
            return;
        }

        // Dining table:
        if (this.isWaiter) {
            this.waiterName = this.authService.getPrinterIdentity().name;
            if (!this.currentWaiterStaffId) {
                if (!this.waiters.length) {
                    await this.loadWaiters();
                }
                this.resolveCurrentWaiterStaffId();
            }
            // Persist assignment so this table shows under My Assigned Orders.
            if (this.currentWaiterStaffId && table.waiterId !== this.currentWaiterStaffId) {
                try {
                    await this.restaurantTableService.assignWaiter({
                        tableId: table.id,
                        waiterId: this.currentWaiterStaffId,
                        waiterName: this.waiterName,
                    });
                    table.waiterId = this.currentWaiterStaffId;
                    table.waiterName = this.waiterName;
                    const listed = this.tables.find((t) => t.id === table.id);
                    if (listed) {
                        listed.waiterId = this.currentWaiterStaffId;
                        listed.waiterName = this.waiterName;
                    }
                } catch {
                    // Continue opening even if assignment persist fails.
                }
            }
            await this.selectTable(table);
            void this.loadMyAssignedOrders();
            return;
        }

        if (this.isAutoAssignWaiter()) {
            await this.autoAssignWaiterAndOpen(table);
            return;
        }

        if (!this.waiters.length) {
            await this.loadWaiters();
        }
        this.assignWaiterDialogVisible = true;
    }

    async confirmVehicleNumber(): Promise<void> {
        const plate = (this.pendingVehicleNumber || '').trim().toUpperCase();
        if (!plate) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Vehicle Number Required',
                detail: 'Enter the car number / plate to identify this order.',
                life: 3000,
            });
            return;
        }
        const table = this.pendingVehicleTable;
        if (!table) {
            this.vehicleDialogVisible = false;
            return;
        }

        this.selectedCarName = table.tableNo;
        this.selectedCarNumber = plate;
        this.selectedCarLocation = table.area || 'Car Parking';
        this.pendingSelectedTable = table;
        this.vehicleDialogVisible = false;
        this.pendingVehicleTable = null;
        this.pendingVehicleNumber = '';

        if (this.isWaiter) {
            this.waiterName = this.authService.getPrinterIdentity().name;
            if (!this.currentWaiterStaffId) {
                if (!this.waiters.length) {
                    await this.loadWaiters();
                }
                this.resolveCurrentWaiterStaffId();
            }
            if (this.currentWaiterStaffId && table.waiterId !== this.currentWaiterStaffId) {
                try {
                    await this.restaurantTableService.assignWaiter({
                        tableId: table.id,
                        waiterId: this.currentWaiterStaffId,
                        waiterName: this.waiterName,
                    });
                    table.waiterId = this.currentWaiterStaffId;
                    table.waiterName = this.waiterName;
                    const listed = this.tables.find((t) => t.id === table.id);
                    if (listed) {
                        listed.waiterId = this.currentWaiterStaffId;
                        listed.waiterName = this.waiterName;
                    }
                } catch {
                    // Continue opening even if assignment persist fails.
                }
            }
            await this.selectTable(table);
            void this.loadMyAssignedOrders();
            return;
        }

        if (this.isAutoAssignWaiter()) {
            await this.autoAssignWaiterAndOpen(table);
            return;
        }

        if (!this.waiters.length) {
            await this.loadWaiters();
        }
        this.assignWaiterDialogVisible = true;
    }

    /** Pick a default waiter and open the table/car without a forced dialog. */
    private async autoAssignWaiterAndOpen(table: RestaurantTableDto): Promise<void> {
        if (!this.waiters.length && !this.isWaiter) {
            await this.loadWaiters();
        }

        const resolved = this.resolveDefaultWaiter(table);
        this.waiterName = resolved.name;
        this.selectedWaiterId = resolved.id;
        this.pendingSelectedTable = table;

        if (!this.isWaiter && resolved.id) {
            try {
                await this.restaurantTableService.assignWaiter({
                    tableId: table.id,
                    waiterId: resolved.id,
                    waiterName: resolved.name,
                });
                table.waiterId = resolved.id;
                table.waiterName = resolved.name;
                const listed = this.tables.find((t) => t.id === table.id);
                if (listed) {
                    listed.waiterId = resolved.id;
                    listed.waiterName = resolved.name;
                }
            } catch {
                // Still open the session even if persist fails
            }
        }

        await this.selectTable(table);
    }

    private resolveDefaultWaiter(table?: RestaurantTableDto | null): {
        id: number | null;
        name: string;
    } {
        if (this.isWaiter) {
            if (!this.currentWaiterStaffId) {
                this.resolveCurrentWaiterStaffId();
            }
            return {
                id: this.currentWaiterStaffId,
                name: this.authService.getPrinterIdentity().name || 'Staff',
            };
        }

        // Prefer waiter already on this table/bay
        if (table?.waiterId && table?.waiterName) {
            return { id: table.waiterId, name: table.waiterName };
        }
        if (table?.waiterName) {
            const byName = this.waiters.find(
                (w) =>
                    (w.name || '').trim().toLowerCase() ===
                    table.waiterName!.trim().toLowerCase()
            );
            return { id: byName?.id ?? null, name: table.waiterName };
        }

        // Keep the waiter from the previous order this session
        if (this.waiterName?.trim()) {
            const match = this.waiters.find(
                (w) =>
                    (w.name || '').trim().toLowerCase() ===
                    this.waiterName.trim().toLowerCase()
            );
            return { id: match?.id ?? null, name: this.waiterName.trim() };
        }

        // First available (non-busy) waiter for the branch
        const available = this.availableWaiters;
        if (available.length) {
            return { id: available[0].id, name: available[0].name };
        }
        // Fall back to any waiter if all are busy
        if (this.waiters.length) {
            return { id: this.waiters[0].id, name: this.waiters[0].name };
        }

        return { id: null, name: 'Staff' };
    }

    cancelVehicleDialog(): void {
        this.vehicleDialogVisible = false;
        this.pendingVehicleTable = null;
        this.pendingVehicleNumber = '';
        this.pendingSelectedTable = null;
        this.pendingSendToTable = false;
    }

    openAssignWaiterDialog(): void {
        if (this.isWaiter || (!this.selectedTable && !this.isCarService)) return;
        this.pendingSelectedTable = this.selectedTable;
        this.selectedWaiterId = this.selectedTable?.waiterId || null;
        if (!this.waiters.length) {
            this.loadWaiters();
        }
        this.assignWaiterDialogVisible = true;
    }

    async confirmAssignWaiter(waiter: StaffDto): Promise<void> {
        if (!this.pendingSelectedTable) {
            if (this.isCarService) {
                this.waiterName = waiter.name;
                this.assignWaiterDialogVisible = false;
                this.messageService.add({
                    severity: 'success',
                    summary: 'Waiter Updated',
                    detail: `Waiter changed to "${waiter.name}" for ${this.carServiceLabel}`,
                    life: 2500,
                });
            }
            return;
        }
        this.assigningWaiter = true;
        try {
            await this.restaurantTableService.assignWaiter({
                tableId: this.pendingSelectedTable.id,
                waiterId: waiter.id,
                waiterName: waiter.name,
            });
            this.pendingSelectedTable.waiterId = waiter.id;
            this.pendingSelectedTable.waiterName = waiter.name;
            this.waiterName = waiter.name;
            this.selectedWaiterId = waiter.id;

            const t = this.tables.find((x) => x.id === this.pendingSelectedTable?.id);
            if (t) {
                t.waiterId = waiter.id;
                t.waiterName = waiter.name;
            }
            if (this.selectedTable?.id === this.pendingSelectedTable.id) {
                this.selectedTable.waiterId = waiter.id;
                this.selectedTable.waiterName = waiter.name;
            }

            this.messageService.add({
                severity: 'success',
                summary: 'Waiter Updated',
                detail: `Waiter "${waiter.name}" assigned to ${this.pendingSelectedTable.tableNo}`,
                life: 2500,
            });

            this.assignWaiterDialogVisible = false;

            // Already on this table/car Ã¢â‚¬â€ just update waiter, don't reopen session
            const isAlreadySelected =
                this.selectedTable?.id === this.pendingSelectedTable.id;
            if (!isAlreadySelected) {
                await this.selectTable(this.pendingSelectedTable);
            }
        } catch (err: any) {
            this.messageService.add({
                severity: 'error',
                summary: 'Error',
                detail: err?.message || 'Failed to assign waiter',
            });
        } finally {
            this.assigningWaiter = false;
        }
    }

    async unassignWaiter(): Promise<void> {
        if (this.isCarService && !this.pendingSelectedTable) {
            this.waiterName = '';
            this.assignWaiterDialogVisible = false;
            this.messageService.add({
                severity: 'info',
                summary: 'Waiter Removed',
                detail: 'Waiter removed from Car Service',
                life: 2500,
            });
            return;
        }
        if (!this.pendingSelectedTable) return;
        this.assigningWaiter = true;
        try {
            await this.restaurantTableService.assignWaiter({
                tableId: this.pendingSelectedTable.id,
                waiterId: 0 as any,
                waiterName: '',
            });
            this.pendingSelectedTable.waiterId = null;
            this.pendingSelectedTable.waiterName = undefined;
            this.waiterName = '';

            const t = this.tables.find((x) => x.id === this.pendingSelectedTable?.id);
            if (t) {
                t.waiterId = null;
                t.waiterName = undefined;
            }

            this.messageService.add({
                severity: 'info',
                summary: 'Waiter Removed',
                detail: `Waiter removed from Table ${this.pendingSelectedTable.tableNo}`,
                life: 2500,
            });

            this.assignWaiterDialogVisible = false;
            const isAlreadySelected =
                this.selectedTable?.id === this.pendingSelectedTable.id;
            if (!isAlreadySelected) {
                await this.selectTable(this.pendingSelectedTable);
            }
        } catch (err: any) {
            this.messageService.add({
                severity: 'error',
                summary: 'Error',
                detail: err?.message || 'Failed to remove waiter',
            });
        } finally {
            this.assigningWaiter = false;
        }
    }

    async skipAssignWaiter(): Promise<void> {
        if (this.pendingSelectedTable) {
            if (this.pendingSelectedTable.waiterName) {
                this.waiterName = this.pendingSelectedTable.waiterName;
            }
            this.assignWaiterDialogVisible = false;
            const isAlreadySelected =
                this.selectedTable?.id === this.pendingSelectedTable.id;
            if (!isAlreadySelected) {
                await this.selectTable(this.pendingSelectedTable);
            }
        } else {
            this.assignWaiterDialogVisible = false;
        }
    }

    onAssignWaiterDialogHide(): void {
        if (
            this.pendingSelectedTable &&
            (!this.selectedTable || this.selectedTable.id !== this.pendingSelectedTable.id)
        ) {
            void this.selectTable(this.pendingSelectedTable);
        }
    }

    async selectTable(table: RestaurantTableDto): Promise<void> {
        this.selectedTable = table;
        this.selectedCarOrder = null;
        const carBay = this.isCarBay(table);
        if (!carBay) {
            this.selectedCarName = '';
            this.selectedCarNumber = '';
            this.selectedCarLocation = '';
        } else {
            this.selectedCarName = this.selectedCarName || table.tableNo;
            this.selectedCarLocation =
                this.selectedCarLocation || table.area || 'Car Parking';
        }
        this.orderType = 'dineIn';
        this.tableDialogVisible = false;

        const isDirectSend = this.pendingSendToTable;
        this.pendingSendToTable = false;
        this.pendingSelectedTable = null;

        if (table.status === 1 || table.status === 2) {
            try {
                let order: RestaurantOrderDto | null = null;
                if (table.currentOrderId) {
                    try {
                        order = await this.restaurantOrderService.get(table.currentOrderId);
                    } catch {
                        order = null;
                    }
                }
                if (!order) {
                    try {
                        order = await this.restaurantOrderService.getByTable(table.id);
                    } catch {
                        order = null;
                    }
                }

                if (order && order.id) {
                    const unsentItems = isDirectSend ? this.cart.filter((c) => !c.id) : [];
                    await this.loadOrderById(order.id);
                    this.selectedTable = table;
                    this.orderType = 'dineIn';
                    if (carBay || order.carNumber || order.orderType === OrderTypes.CarService) {
                        this.selectedCarName = order.carName || table.tableNo;
                        this.selectedCarNumber = order.carNumber || table.currentVehicleNumber || this.selectedCarNumber || '';
                        this.selectedCarLocation = order.carLocation || table.area || 'Car Parking';
                    }
                    if (this.isWaiter) {
                        this.waiterName = this.authService.getPrinterIdentity().name;
                    } else if (order.waiterName || table.waiterName) {
                        this.waiterName = order.waiterName || table.waiterName || this.waiterName || '';
                    }

                    if (isDirectSend && unsentItems.length > 0) {
                        this.cart = [...this.cart, ...unsentItems];
                        await this.sendToTable();
                    }
                    return;
                } else {
                    this.clearCart();
                    this.syncActiveOrderMeta(null);
                    this.messageService.add({
                        severity: 'warn',
                        summary: 'No Active Order',
                        detail: `Table ${table.tableNo} is occupied, but no active order was found. Add items to start a new order.`,
                        life: 3500,
                    });
                    return;
                }
            } catch (err) {
                console.error('Failed to load table order', err);
            }
        } else {
            // Available table / car bay: select table/car locally.
            // Do NOT occupy or hold the table until items are actually sent to table / kitchen (or saved as tab).
            this.selectedCarOrder = null;
            if (!carBay) {
                this.selectedCarName = '';
                this.selectedCarNumber = '';
                this.selectedCarLocation = '';
            }
            // Preserve cart if user already selected products before selecting table
            if (!isDirectSend && this.activeOrderId) {
                this.cart = [];
                this.notes = '';
                this.discountAmount = 0;
                this.discountPercent = 0;
            }
            this.selectedTable = table;
            if (this.isWaiter) {
                this.waiterName = this.authService.getPrinterIdentity().name;
            } else if (!this.waiterName) {
                this.waiterName = table.waiterName || '';
            }

            this.syncActiveOrderMeta(null);

            if (!isDirectSend) {
                this.messageService.add({
                    severity: 'info',
                    summary: carBay ? 'Car Bay Selected' : 'Table Selected',
                    detail: carBay
                        ? `${this.carServiceLabel} selected — select products and click "Send to KOT".`
                        : `Table ${table.tableNo} selected — select products and click "Send to KOT".`,
                    life: 3000,
                });
            }
        }

        if (isDirectSend) {
            await this.sendToTable();
        }
    }

    clearTableSelection(): void {
        this.selectedTable = null;
        this.selectedCarOrder = null;
        this.selectedCarName = '';
        this.selectedCarNumber = '';
        this.selectedCarLocation = '';
        this.syncActiveOrderMeta(null);
        if (this.isWaiter) {
            this.waiterName = this.authService.getPrinterIdentity().name;
        }
        // Keep last waiterName for admins so the next table/car auto-assigns the same person
        this.pendingSendToTable = false;
        this.clearCart();
    }

    // Cancel Restaurant Order (Admin only)
    cancelOrderDialogVisible = false;
    cancellingOrder = false;

    openCancelOrderDialog(): void {
        if (this.isWaiter || !this.activeOrderId) return;
        this.cancelOrderDialogVisible = true;
    }

    async confirmCancelOrder(): Promise<void> {
        if (this.isWaiter || !this.activeOrderId) return;
        this.cancellingOrder = true;
        try {
            await this.restaurantOrderService.cancelOrder(this.activeOrderId);
            this.messageService.add({
                severity: 'success',
                summary: 'Order Cancelled',
                detail: `Order #${this.activeOrderNo || this.activeOrderId} has been cancelled.`,
                life: 3000,
            });
            this.cancelOrderDialogVisible = false;
            this.router.navigate([], { queryParams: {}, replaceUrl: true });
            this.clearCart();
            this.selectedCarOrder = null;
            this.selectedCarName = '';
            this.selectedCarNumber = '';
            this.selectedCarLocation = '';
            await this.loadTables();
            await this.loadUnpaidTakeaways();
            if (this.takeawayAlarmService) {
                this.takeawayAlarmService.checkImmediately();
            }
        } catch (err: any) {
            this.messageService.add({
                severity: 'error',
                summary: 'Error',
                detail: err?.message || 'Failed to cancel order',
            });
        } finally {
            this.cancellingOrder = false;
        }
    }

    openCancelTakeawayDialog(order: UnpaidTakeawayOrderDto): void {
        this.orderToCancel = order;
        this.cancelTakeawayDialogVisible = true;
    }

    async confirmCancelTakeawayOrder(): Promise<void> {
        if (!this.orderToCancel) return;
        this.cancellingTakeawayOrder = true;
        try {
            await this.restaurantOrderService.cancelOrder(this.orderToCancel.id);
            this.messageService.add({
                severity: 'success',
                summary: 'Takeaway KOT Cancelled',
                detail: `Takeaway order #${this.orderToCancel.orderNo} has been cancelled.`,
                life: 3000,
            });
            this.router.navigate([], { queryParams: {}, replaceUrl: true });
            if (this.activeOrderId === this.orderToCancel.id) {
                this.clearCart();
            }
            this.cancelTakeawayDialogVisible = false;
            this.orderToCancel = null;
            await this.loadUnpaidTakeaways();
            if (this.takeawayAlarmService) {
                this.takeawayAlarmService.checkImmediately();
            }
        } catch (err: any) {
            this.messageService.add({
                severity: 'error',
                summary: 'Error',
                detail: err?.message || 'Failed to cancel takeaway order',
            });
        } finally {
            this.cancellingTakeawayOrder = false;
        }
    }

    // Shift / Transfer Table
    transferDialogVisible = false;
    transferringTable = false;
    selectedTargetTableId: number | null = null;

    get availableTransferTables(): RestaurantTableDto[] {
        return (this.tables || []).filter(
            (t) => t.id !== this.selectedTable?.id && t.status === 0
        );
    }

    openTransferDialog(): void {
        if (!this.selectedTable) {
            this.messageService.add({
                severity: 'warn',
                summary: 'No Table',
                detail: 'Select an active table or car first to transfer.',
            });
            return;
        }
        if (!this.activeOrderId) {
            this.messageService.add({
                severity: 'warn',
                summary: 'No Order',
                detail: 'Send items to the table/car first, then transfer the order.',
            });
            return;
        }
        this.selectedTargetTableId = null;
        this.transferDialogVisible = true;
    }

    async confirmTransfer(): Promise<void> {
        if (!this.selectedTable || !this.selectedTargetTableId) return;
        this.transferringTable = true;
        const fromLabel = this.selectedTable.tableNo;
        const orderId = this.activeOrderId;
        try {
            const target = this.tables.find(
                (t) => t.id === this.selectedTargetTableId
            );
            await this.restaurantTableService.transferTable(
                this.selectedTable.id,
                this.selectedTargetTableId
            );
            this.messageService.add({
                severity: 'success',
                summary: 'Order Transferred',
                detail: `Moved from ${fromLabel} to ${target?.tableNo || ''}`,
            });
            this.transferDialogVisible = false;
            await this.loadTables();
            const refreshed =
                (this.tables || []).find((t) => t.id === this.selectedTargetTableId) ||
                target ||
                null;
            this.selectedTable = refreshed;
            if (this.isCarBay(refreshed)) {
                this.selectedCarName = refreshed?.tableNo || '';
                this.selectedCarLocation = refreshed?.area || 'Car Parking';
                // Keep plate if already set on the order; clear only when moving to a plain table.
            } else {
                this.selectedCarOrder = null;
                this.selectedCarName = '';
                this.selectedCarNumber = '';
                this.selectedCarLocation = '';
            }
            if (orderId) {
                await this.loadOrderById(orderId);
            }
        } catch (err: any) {
            this.messageService.add({
                severity: 'error',
                summary: 'Error',
                detail: err?.message || 'Failed to transfer table',
            });
        } finally {
            this.transferringTable = false;
        }
    }

    async loadUnpaidTakeaways(): Promise<void> {
        const branchId = this.branchContext.getBranchId();
        if (!branchId || !this.isRestaurantMode || this.isWaiter) {
            this.unpaidTakeawayOrders = [];
            return;
        }
        try {
            this.unpaidTakeawayOrders = await this.restaurantOrderService.getUnpaidTakeawayOrders(branchId);
        } catch {
            this.unpaidTakeawayOrders = [];
        }
    }

    openUnpaidTakeawayDialog(): void {
        if (this.isWaiter) return;
        this.loadUnpaidTakeaways();
        this.unpaidTakeawayDialogVisible = true;
    }

    async selectUnpaidTakeaway(order: UnpaidTakeawayOrderDto): Promise<void> {
        this.unpaidTakeawayDialogVisible = false;
        this.router.navigate([], { queryParams: { restaurantOrderId: order.id, _t: Date.now() }, replaceUrl: true });
        await this.loadOrderById(order.id);
    }

    async loadOrderById(orderId: number): Promise<void> {
        try {
            this.cart = [];
            this.syncActiveOrderMeta(null);
            this.discountPercent = 0;
            this.discountAmount = 0;
            this.taxPercent = 0;
            this.serviceChargePercent = 0;
            this.notes = '';

            const order = await this.restaurantOrderService.get(orderId);
            if (!order) {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Order Not Found',
                    detail: `Order #${orderId} could not be found.`,
                });
                return;
            }

            if (
                order.status === RestaurantOrderStatuses.Settled ||
                order.status === RestaurantOrderStatuses.Cancelled ||
                (order.status as any) === 'Settled' ||
                (order.status as any) === 'Cancelled'
            ) {
                this.messageService.add({
                    severity: 'warn',
                    summary: 'Order Not Pending',
                    detail: `Order #${order.orderNo || orderId} is already ${order.statusName || order.status}.`,
                });
                this.router.navigate([], { queryParams: {}, replaceUrl: true });
                return;
            }

            this.syncActiveOrderMeta(order);
            if (order.orderType === OrderTypes.CarService || order.carNumber) {
                this.orderType = 'dineIn';
                this.selectedCarOrder = order;
                this.selectedCarName = order.carName || '';
                this.selectedCarNumber = order.carNumber || '';
                this.selectedCarLocation = order.carLocation || '';
                if (order.tableId) {
                    this.selectedTable =
                        this.tables.find((t) => t.id === order.tableId) ||
                        ({
                            id: order.tableId,
                            tableNo: order.carName || order.tableNo || 'Car',
                            area: order.carLocation || 'Car Parking',
                            isCarBay: true,
                            status: 1,
                            capacity: 4,
                            statusName: 'Occupied',
                            branchId: order.branchId,
                            isActive: true,
                            currentVehicleNumber: order.carNumber,
                        } as RestaurantTableDto);
                } else {
                    this.selectedTable = null;
                }
            } else if (order.orderType === OrderTypes.Takeaway) {
                this.orderType = 'takeaway';
                this.selectedCarOrder = null;
                this.selectedCarName = '';
                this.selectedCarNumber = '';
                this.selectedCarLocation = '';
                this.selectedTable = null;
            } else {
                this.orderType = 'dineIn';
                this.thirdPartyChannel = null;
                this.selectedCarOrder = null;
                this.selectedCarName = '';
                this.selectedCarNumber = '';
                this.selectedCarLocation = '';
                if (order.tableId) {
                    this.selectedTable = this.tables.find((t) => t.id === order.tableId) || ({
                        id: order.tableId,
                        tableNo: order.tableNo || 'Table',
                        status: 1,
                    } as any);
                } else {
                    this.selectedTable = null;
                }
            }

            if (this.isWaiter) {
                this.waiterName = this.authService.getPrinterIdentity().name;
            } else {
                this.waiterName = order.waiterName || this.waiterName;
            }
            this.notes = order.notes || '';
            if (order.customerId) {
                this.customerId = order.customerId;
            }
            if (this.orderType === 'takeaway') {
                this.syncThirdPartyFromCustomer();
            } else {
                this.thirdPartyChannel = null;
            }
            this.syncSelectedChannelFromOrderState();
            this.discountPercent = order.discountPercent || 0;
            this.discountAmount = order.discountAmount || 0;
            this.taxPercent = order.taxPercent || 0;
            this.serviceChargePercent = order.serviceChargePercent || 0;

            const existingLines: PosCartLine[] = [];
            if (order.lines?.length) {
                for (const l of order.lines) {
                    let isDeal = false;
                    let dealSummary: string | undefined = undefined;
                    let dealId: number | undefined = undefined;
                    let selectedModifiers = l.selectedModifiers;

                    const mods = this.parseModifiers(selectedModifiers);
                    const dealMod = mods.find((m: string) => typeof m === 'string' && m.startsWith('DEAL:'));
                    if (dealMod) {
                        isDeal = true;
                        dealSummary = dealMod.substring(5).trim();
                    }

                    // Prefer catalog flags / DEAL: modifiers; only fetch deal details when already known as a deal.
                    const catalogProduct = this.menuProducts?.find((p) => p.id === l.productId);
                    if (!isDeal && catalogProduct && this.isDealProduct(catalogProduct)) {
                        isDeal = true;
                        dealId = catalogProduct.dealId || undefined;
                    }
                    if (isDeal && (!dealSummary || !dealId)) {
                        try {
                            const dealInfo = catalogProduct?.dealId
                                ? await this.dealService.get(catalogProduct.dealId)
                                : await this.dealService.getByProductId(l.productId);
                            if (dealInfo && dealInfo.items?.length) {
                                dealId = dealInfo.id;
                                if (!dealSummary) {
                                    dealSummary = dealInfo.items.map((i) => `${i.quantity}x ${i.productName}`).join(' â€¢ ');
                                }
                                if (!selectedModifiers || !selectedModifiers.includes('DEAL:')) {
                                    const extra = this.parseModifiers(selectedModifiers).filter(
                                        (m: string) => typeof m === 'string' && !m.startsWith('DEAL:')
                                    );
                                    selectedModifiers = JSON.stringify([`DEAL: ${dealSummary}`, ...extra]);
                                }
                            }
                        } catch {}
                    }

                    existingLines.push({
                        id: l.id,
                        productId: l.productId,
                        productName: l.productName,
                        quantity: l.quantity,
                        unitPrice: l.unitPrice,
                        retailPrice: l.unitPrice,
                        wholesalePrice: l.unitPrice,
                        stockQuantity: 999,
                        kitchenNotes: l.kitchenNotes,
                        selectedModifiers,
                        isSentToKitchen: true,
                        printedQuantity: l.printedQuantity && l.printedQuantity > 0 ? l.printedQuantity : l.quantity,
                        isDeal,
                        dealId,
                        dealSummary,
                    });
                }
            }
            this.cart = existingLines;
            const targetLabel = this.isCarService
                ? `Car ${this.selectedCarName} (${this.selectedCarNumber})`
                : (this.orderType === 'takeaway' ? 'Takeaway' : 'Table ' + (order.tableNo || ''));
            this.messageService.add({
                severity: 'info',
                summary: 'Order Loaded',
                detail: `Loaded order #${order.orderNo} (${targetLabel}) with ${this.cart.length} item(s)`,
                life: 3000,
            });
        } catch (err: any) {
            console.error('Failed to load order', err);
            this.messageService.add({
                severity: 'error',
                summary: 'Error',
                detail: err?.message || 'Failed to load order',
            });
        }
    }

    async sendToTable(): Promise<void> {
        if (!this.cart.length) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Cart Empty',
                detail: 'Add food items before sending to KOT.',
            });
            return;
        }

        if (this.orderType === 'dineIn' && !this.selectedTable && !this.isCarService) {
            this.openTableDialog(true);
            this.messageService.add({
                severity: 'info',
                summary: 'Select Table or Car',
                detail: 'Please select a dining table or car service before sending to kitchen.',
                life: 3000,
            });
            return;
        }

        const branchId = this.branchContext.getBranchId();
        if (!branchId) return;

        this.sendingKOT = true;
        try {
            const isTakeaway = this.orderType === 'takeaway';
            const isCar = this.isCarService;
            const waiter = this.waiterName || (this.isWaiter ? this.authService.getPrinterIdentity().name : 'Staff');
            const loc = this.buildOrderLocationFields(isTakeaway);

            const payload: CreateOrUpdateRestaurantOrderDto = {
                id: this.activeOrderId,
                branchId,
                ...loc,
                customerId: this.customerId,
                waiterName: waiter,
                notes: this.notes,
                discountPercent: this.discountPercent || 0,
                discountAmount: this.discountAmount || 0,
                taxPercent: this.taxPercent || 0,
                serviceChargePercent: this.serviceChargePercent || 0,
                lines: this.cart.map((line) => ({
                    id: line.id,
                    productId: line.productId,
                    productName: line.productName,
                    quantity: line.quantity,
                    unitPrice: line.unitPrice,
                    kitchenNotes: line.kitchenNotes || '',
                    selectedModifiers: line.selectedModifiers,
                    isSentToKitchen: line.isSentToKitchen || false,
                    printedQuantity: line.printedQuantity || 0,
                })),
            };

            const savedOrder = await this.restaurantOrderService.saveOrder(payload);
            this.syncActiveOrderMeta(savedOrder);
            if (this.selectedTable) {
                this.selectedTable.status = 1;
                this.selectedTable.statusName = 'Occupied';
                this.selectedTable.currentOrderId = savedOrder.id;
                this.selectedTable.waiterName = waiter;
            }

            // Sync saved line ids and printed quantities back to local cart
            if (savedOrder.lines?.length) {
                savedOrder.lines.forEach((sl) => {
                    const cartLine = this.cart.find(
                        (cl) => cl.id === sl.id || (!cl.id && cl.productId === sl.productId)
                    );
                    if (cartLine) {
                        cartLine.id = sl.id;
                        cartLine.printedQuantity = sl.printedQuantity ?? cartLine.printedQuantity ?? 0;
                        // Keep local note if API omits it; otherwise take persisted value.
                        if (sl.kitchenNotes) {
                            cartLine.kitchenNotes = sl.kitchenNotes;
                        }
                    }
                });
            }

            const kotResult = await this.restaurantOrderService.sendToKitchen(savedOrder.id);
            this.kotResult = kotResult;
            if (kotResult?.tokenNo != null) {
                this.activeTokenNo = kotResult.tokenNo;
            }

            this.cart.forEach((l) => (l.isSentToKitchen = true));
            if (!isTakeaway) {
                await this.loadTables();
                if (this.isWaiter) {
                    void this.loadMyAssignedOrders();
                }
            } else {
                if (!this.isWaiter) {
                    this.loadUnpaidTakeaways();
                    if (this.takeawayAlarmService) {
                        this.takeawayAlarmService.checkImmediately();
                    }
                }
            }

            if (!kotResult.hasNewItems || !kotResult.newItems?.length) {
                this.messageService.add({
                    severity: 'info',
                    summary: 'Kitchen Up to Date',
                    detail: kotResult.message || 'All items for this order have already been sent to kitchen.',
                    life: 3500,
                });
            } else {
                // Print KOT immediately — no preview popup.
                this.printCurrentModal('kotThermalPrint', 'all');
                this.messageService.add({
                    severity: 'success',
                    summary: isCar ? 'Sent to Car (KOT)' : 'Sent to KOT',
                    detail: isCar
                        ? `Order sent to Car ${this.selectedCarName} (${this.selectedCarNumber}) — kitchen KOT #${kotResult.orderNo}`
                        : (isTakeaway
                            ? `Takeaway order #${kotResult.orderNo} sent to kitchen KOT (Unpaid)`
                            : `Table ${kotResult.tableNo || this.selectedTable?.tableNo || ''} order #${kotResult.orderNo} sent to kitchen KOT`),
                    life: 2500,
                });
                // Takeaway KOT is parked in unpaid list — clear cart so the next order can start immediately.
                if (isTakeaway) {
                    const keepOrderType = this.orderType;
                    const keepChannel = this.selectedChannel;
                    const keepThirdParty = this.thirdPartyChannel;
                    this.clearCart();
                    this.orderType = keepOrderType;
                    this.thirdPartyChannel = keepThirdParty;
                    this.selectedChannel = keepChannel;
                }
            }
        } catch (err: any) {
            this.messageService.add({
                severity: 'error',
                summary: 'Error',
                detail: err?.message || 'Failed to send order to kitchen',
            });
        } finally {
            this.sendingKOT = false;
        }
    }

    sendTakeawayToKOT(): Promise<void> {
        this.orderType = 'takeaway';
        if (this.selectedChannel !== 'foodpanda') {
            this.selectedChannel = 'takeaway';
            this.thirdPartyChannel = null;
        }
        return this.sendToTable();
    }

    sendToKitchen(): Promise<void> {
        return this.sendToTable();
    }

    canManageTableBill(table: RestaurantTableDto): boolean {
        return !this.isWaiter && !!table && (table.status === 1 || table.status === 2);
    }

    private async resolveActiveOrderForTable(
        table: RestaurantTableDto
    ): Promise<RestaurantOrderDto | null> {
        if (!table) {
            return null;
        }
        let order: RestaurantOrderDto | null = null;
        if (table.currentOrderId) {
            try {
                order = await this.restaurantOrderService.get(table.currentOrderId);
            } catch {
                order = null;
            }
        }
        if (!order) {
            try {
                order = await this.restaurantOrderService.getByTable(table.id);
            } catch {
                order = null;
            }
        }
        return order?.id ? order : null;
    }

    private ensureSettlementCustomer(): void {
        if (this.customerId) {
            return;
        }
        const walkIn =
            this.customers.find(
                (c) => (c.name || '').toLowerCase().includes('walk')
            ) || this.customers[0];
        this.customerId = walkIn?.id || null;
    }

    private async prepareTableOrderForBilling(
        table: RestaurantTableDto
    ): Promise<boolean> {
        const order = await this.resolveActiveOrderForTable(table);
        if (!order) {
            this.messageService.add({
                severity: 'warn',
                summary: 'No Active Order',
                detail: `${table.tableNo} has no open order to bill.`,
            });
            return false;
        }
        await this.loadOrderById(order.id);
        this.selectedTable = table;
        this.orderType = 'dineIn';
        if (this.isCarBay(table) || order.carNumber || order.orderType === OrderTypes.CarService) {
            this.selectedCarName = order.carName || table.tableNo;
            this.selectedCarNumber =
                order.carNumber || table.currentVehicleNumber || this.selectedCarNumber || '';
            this.selectedCarLocation = order.carLocation || table.area || 'Car Parking';
        }
        if (!this.isWaiter && (order.waiterName || table.waiterName)) {
            this.waiterName = order.waiterName || table.waiterName || this.waiterName || '';
        }
        this.ensureSettlementCustomer();
        this.tableDialogVisible = false;
        this.pendingSendToTable = false;
        return true;
    }

    async printGuestBillFromTable(
        table: RestaurantTableDto,
        event?: Event
    ): Promise<void> {
        event?.stopPropagation();
        event?.preventDefault();
        if (!this.canManageTableBill(table) || this.tableBillBusyId) {
            return;
        }
        this.tableBillBusyId = table.id;
        try {
            const ok = await this.prepareTableOrderForBilling(table);
            if (!ok) {
                return;
            }
            if (!this.cart.length) {
                this.messageService.add({
                    severity: 'warn',
                    summary: 'Empty Order',
                    detail: 'This table has no items to print.',
                });
                return;
            }
            await this.printGuestBill();
        } finally {
            this.tableBillBusyId = null;
        }
    }

    async completeBillFromTable(
        table: RestaurantTableDto,
        event?: Event
    ): Promise<void> {
        event?.stopPropagation();
        event?.preventDefault();
        if (!this.canManageTableBill(table) || this.tableBillBusyId) {
            return;
        }
        this.tableBillBusyId = table.id;
        try {
            const ok = await this.prepareTableOrderForBilling(table);
            if (!ok) {
                return;
            }
            if (!this.cart.length) {
                this.messageService.add({
                    severity: 'warn',
                    summary: 'Empty Order',
                    detail: 'This table has no items to settle.',
                });
                return;
            }
            if (this.hasUnsentItems) {
                this.messageService.add({
                    severity: 'warn',
                    summary: 'Send to KOT first',
                    detail: 'Some items are not sent to kitchen yet. Send KOT before completing the bill.',
                    life: 4000,
                });
                return;
            }
            this.completeSale();
        } finally {
            this.tableBillBusyId = null;
        }
    }

    async printGuestBill(): Promise<void> {
        if (this.isWaiter || !this.cart.length) return;
        const branchId = this.branchContext.getBranchId();
        if (!branchId) return;

        this.printingBill = true;
        try {
            if (!this.activeOrderId) {
                const loc = this.buildOrderLocationFields(this.orderType === 'takeaway');
                const payload = {
                    branchId,
                    ...loc,
                    customerId: this.customerId,
                    waiterName: this.waiterName || 'Staff',
                    notes: this.notes,
                    discountPercent: this.discountPercent || 0,
                    discountAmount: this.discountAmount || 0,
                    taxPercent: this.taxPercent || 0,
                    serviceChargePercent: this.serviceChargePercent || 0,
                    lines: this.cart.map((line) => ({
                        id: line.id,
                        productId: line.productId,
                        productName: line.productName,
                        quantity: line.quantity,
                        unitPrice: line.unitPrice,
                        kitchenNotes: line.kitchenNotes,
                        selectedModifiers: line.selectedModifiers,
                        isSentToKitchen: line.isSentToKitchen || false,
                    })),
                };
                const saved = await this.restaurantOrderService.saveOrder(payload);
                this.syncActiveOrderMeta(saved);
            }

            const orderId = this.activeOrderId;
            if (!orderId) {
                throw new Error('No active order to print.');
            }

            const billed = await this.restaurantOrderService.printBill(orderId);
            if (billed) {
                this.syncActiveOrderMeta(billed);
            }
            this.guestBillDialogVisible = true;
            this.loadTables();
            this.messageService.add({
                severity: 'info',
                summary: 'Guest Check Ready',
                detail: `Bill printed for ${this.isCarService ? this.carServiceLabel : (this.selectedTable?.tableNo || 'Table')}`,
                life: 3000,
            });
        } catch (err: any) {
            this.messageService.add({
                severity: 'error',
                summary: 'Error',
                detail: err?.message || 'Failed to print guest bill',
            });
        } finally {
            this.printingBill = false;
        }
    }

    openSplitBill(): void {
        if (this.isWaiter || !this.cart.length) return;
        this.splitBillDialogVisible = true;
    }

    get splitAmountPerPerson(): number {
        const count = Math.max(1, this.splitCount || 1);
        return Math.round((this.grandTotal / count) * 100) / 100;
    }

    // Line Cooking Notes Dialog
    editingLineForNote: PosCartLine | null = null;
    lineNoteDialogVisible = false;
    tempLineNote = '';
    savingLineNote = false;

    openEditLineNote(line: PosCartLine): void {
        this.editingLineForNote = line;
        this.tempLineNote = line.kitchenNotes || '';
        this.lineNoteDialogVisible = true;
    }

    async saveLineNote(): Promise<void> {
        if (!this.editingLineForNote) {
            this.lineNoteDialogVisible = false;
            return;
        }

        const noteText = this.tempLineNote.trim();
        this.editingLineForNote.kitchenNotes = noteText || undefined;
        const line = this.editingLineForNote;

        if (!this.isRestaurantMode) {
            this.lineNoteDialogVisible = false;
            this.editingLineForNote = null;
            this.tempLineNote = '';
            return;
        }

        if (this.orderType === 'dineIn' && !this.selectedTable && !this.isCarService) {
            this.messageService.add({
                severity: 'info',
                summary: 'Select Table or Car',
                detail: 'Select a table/car before saving the kitchen note.',
            });
            return;
        }

        const branchId = this.branchContext.getBranchId();
        if (!branchId || !this.cart.length) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Note',
                detail: 'Add items to the order before saving a kitchen note.',
            });
            return;
        }

        this.savingLineNote = true;
        try {
            const loc = this.buildOrderLocationFields(this.orderType === 'takeaway');
            const waiter = this.waiterName || (this.isWaiter ? this.authService.getPrinterIdentity().name : 'Staff');
            const saved = await this.restaurantOrderService.saveOrder({
                id: this.activeOrderId,
                branchId,
                ...loc,
                customerId: this.customerId,
                waiterName: waiter,
                notes: this.notes,
                discountPercent: this.discountPercent || 0,
                discountAmount: this.discountAmount || 0,
                taxPercent: this.taxPercent || 0,
                serviceChargePercent: this.serviceChargePercent || 0,
                lines: this.cart.map((cartLine) => ({
                    id: cartLine.id,
                    productId: cartLine.productId,
                    productName: cartLine.productName,
                    quantity: cartLine.quantity,
                    unitPrice: cartLine.unitPrice,
                    // Always send string so backend persists / clears notes reliably.
                    kitchenNotes: cartLine.kitchenNotes || '',
                    selectedModifiers: cartLine.selectedModifiers,
                    isSentToKitchen: cartLine.isSentToKitchen || false,
                    printedQuantity: cartLine.printedQuantity || 0,
                })),
            });

            this.syncActiveOrderMeta(saved);
            if (this.selectedTable) {
                this.selectedTable.status = 1;
                this.selectedTable.statusName = 'Occupied';
                this.selectedTable.currentOrderId = saved.id;
                this.selectedTable.waiterName = waiter;
            }

            if (saved.lines?.length) {
                saved.lines.forEach((sl) => {
                    const cartLine = this.cart.find(
                        (cl) =>
                            cl.id === sl.id ||
                            (!cl.id &&
                                cl.productId === sl.productId &&
                                (cl.kitchenNotes || '') === (sl.kitchenNotes || ''))
                    ) || this.cart.find(
                        (cl) => cl.id === sl.id || (!cl.id && cl.productId === sl.productId)
                    );
                    if (cartLine) {
                        cartLine.id = sl.id;
                        cartLine.kitchenNotes = sl.kitchenNotes || cartLine.kitchenNotes;
                        cartLine.printedQuantity = sl.printedQuantity ?? cartLine.printedQuantity ?? 0;
                        cartLine.isSentToKitchen =
                            cartLine.isSentToKitchen || !!sl.isSentToKitchen;
                    }
                });
            }

            // Keep local note even if API omits it in the response.
            if (line) {
                line.kitchenNotes = noteText || undefined;
            }

            this.lineNoteDialogVisible = false;
            this.editingLineForNote = null;
            this.tempLineNote = '';

            this.messageService.add({
                severity: 'success',
                summary: 'Kitchen Note',
                detail: noteText
                    ? `Note saved for ${line.productName}. Opening kitchen…`
                    : `Note cleared for ${line.productName}. Opening kitchen…`,
                life: 2500,
            });

            await this.router.navigate(['/kitchen']);
        } catch (err: any) {
            this.messageService.add({
                severity: 'error',
                summary: 'Kitchen Note',
                detail: err?.message || 'Failed to save kitchen note.',
            });
        } finally {
            this.savingLineNote = false;
        }
    }

    parseModifiers(modJson?: string): string[] {
        if (!modJson) return [];
        try {
            const parsed = JSON.parse(modJson);
            return Array.isArray(parsed) ? parsed : [];
        } catch {
            return [modJson];
        }
    }

    async saveRunningTab(): Promise<void> {
        if (this.isWaiter) return;
        if (!this.cart.length) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Empty Order',
                detail: 'Please add items before saving tab.',
            });
            return;
        }

        const branchId = this.branchContext.getBranchId();
        if (!branchId) return;

        this.savingOrder = true;
        try {
            const loc = this.buildOrderLocationFields(this.orderType === 'takeaway');
            const payload: CreateOrUpdateRestaurantOrderDto = {
                id: this.activeOrderId,
                branchId,
                ...loc,
                customerId: this.customerId,
                waiterName: this.waiterName || 'Staff',
                notes: this.notes,
                discountPercent: this.discountPercent || 0,
                discountAmount: this.discountAmount || 0,
                taxPercent: this.taxPercent || 0,
                serviceChargePercent: this.serviceChargePercent || 0,
                lines: this.cart.map((line) => ({
                    id: line.id,
                    productId: line.productId,
                    productName: line.productName,
                    quantity: line.quantity,
                    unitPrice: line.unitPrice,
                    kitchenNotes: line.kitchenNotes,
                    selectedModifiers: line.selectedModifiers,
                    isSentToKitchen: line.isSentToKitchen || false,
                })),
            };

            const saved = await this.restaurantOrderService.saveOrder(payload);
            this.syncActiveOrderMeta(saved);
            if (this.selectedTable) {
                this.selectedTable.status = 1;
                this.selectedTable.statusName = 'Occupied';
                this.selectedTable.currentOrderId = saved.id;
                this.selectedTable.waiterName = this.waiterName || 'Staff';
            }
            this.loadTables();

            this.messageService.add({
                severity: 'success',
                summary: 'Tab Saved',
                detail: `Order #${saved.orderNo} held on ${
                    this.isCarService
                        ? this.carServiceLabel
                        : this.selectedTable
                          ? 'Table ' + this.selectedTable.tableNo
                          : 'Takeaway'
                }`,
                life: 3000,
            });
        } catch (err: any) {
            this.messageService.add({
                severity: 'error',
                summary: 'Error',
                detail: err?.message || 'Failed to save tab',
            });
        } finally {
            this.savingOrder = false;
        }
    }

    printCurrentModal(elementId: string, filterType: 'all' | 'kitchen' | 'bar' = 'all'): void {
        if (this.isRestaurantMode) {
            const branchId = this.branchContext.getBranchId();
            if (branchId) {
                const receiptType = elementId === 'kotThermalPrint' ? 'KitchenOrderTicket' : 'GuestBill';
                this.restaurantOrderService
                    .recordPrintLog({
                        branchId,
                        orderId: this.activeOrderId,
                        orderNo: this.activeOrderNo,
                        receiptType,
                    })
                    .catch(() => {});
            }

            if (elementId === 'kotThermalPrint' && this.activeOrderId) {
                this.restaurantOrderService
                    .confirmKotPrint({ orderId: this.activeOrderId })
                    .then(() => {
                        this.cart.forEach((l) => {
                            if ((l.quantity || 0) > (l.printedQuantity || 0)) {
                                l.printedQuantity = l.quantity;
                                l.isSentToKitchen = true;
                            }
                        });
                    })
                    .catch((err: any) => console.error('Failed to confirm KOT print', err));
            }
        }

        if (elementId === 'kotThermalPrint' && this.kotResult) {
            this.thermalPrintService.printKotTicket(this.kotResult, filterType);
            return;
        }

        if (elementId === 'guestCheckPrint') {
            this.thermalPrintService.printGuestBill({
                orderNo: this.activeOrderNo,
                tokenNo: this.activeTokenNo,
                orderTypeLabel: this.guestOrderTypeLabel,
                tableNo: this.selectedTable?.tableNo || 'Takeaway',
                carName: this.selectedCarName,
                carNumber: this.selectedCarNumber,
                carLocation: this.selectedCarLocation,
                isCarService: this.isCarService,
                waiterName: this.waiterName || (this.isWaiter ? this.authService.getPrinterIdentity().name : 'Staff'),
                lines: this.cart,
                subTotal: this.subTotal,
                discountAmount: this.computedDiscount,
                discountPercent: this.discountPercent,
                taxAmount: this.taxAmount,
                taxPercent: this.taxPercent,
                serviceChargeAmount: this.serviceChargeAmount,
                serviceChargePercent: this.serviceChargePercent,
                grandTotal: this.grandTotal,
            });
            return;
        }

        const printContent = document.getElementById(elementId);
        if (!printContent) {
            console.warn(`Print element not found: ${elementId}`);
            return;
        }
        this.thermalPrintService.executePrint(printContent.innerHTML, 'Ticket');
    }

    /** Shared table / car bay fields for restaurant order save payloads. */
    private buildOrderLocationFields(isTakeaway: boolean): {
        tableId: number | null;
        tableNo: string;
        orderType: OrderType;
        carName?: string;
        carNumber?: string;
        carLocation?: string;
    } {
        const isCar = !isTakeaway && this.isCarService;
        if (isTakeaway) {
            if (this.isOnlineOrder) {
                return {
                    tableId: null,
                    tableNo: 'Online',
                    orderType: OrderTypes.Delivery,
                };
            }
            return {
                tableId: null,
                tableNo: this.isFoodpandaOrder ? 'Third Party' : 'Takeaway',
                orderType: OrderTypes.Takeaway,
            };
        }
        if (isCar) {
            const bay = this.selectedCarName || this.selectedTable?.tableNo || 'Car';
            const plate = this.selectedCarNumber || '';
            const bayTableId =
                this.selectedTable?.id ??
                this.tables.find(
                    (t) =>
                        this.isCarBay(t) &&
                        (t.tableNo || '').trim().toLowerCase() ===
                            (bay || '').trim().toLowerCase()
                )?.id ??
                null;
            // Keep selectedTable in sync so later KOTs still send TableId
            if (!this.selectedTable && bayTableId) {
                this.selectedTable =
                    this.tables.find((t) => t.id === bayTableId) || this.selectedTable;
            }
            return {
                tableId: bayTableId,
                tableNo: plate ? `Car: ${bay} - ${plate}` : bay,
                orderType: OrderTypes.CarService,
                carName: bay,
                carNumber: plate || undefined,
                carLocation: this.selectedCarLocation || this.selectedTable?.area || 'Car Parking',
            };
        }
        return {
            tableId: this.selectedTable?.id ?? null,
            tableNo: this.selectedTable?.tableNo || 'Table',
            orderType: OrderTypes.DineIn,
        };
    }

    async createQuickTable(): Promise<void> {
        const branchId = this.branchContext.getBranchId();
        if (!branchId || !this.newTableNo?.trim()) return;
        this.creatingTable = true;
        try {
            const table = await this.restaurantTableService.create({
                branchId,
                tableNo: this.newTableNo.trim(),
                area: this.newTableArea?.trim() || 'Main Hall',
                capacity: this.newTableCapacity || 4,
                isCarBay: false,
            });
            this.tables = [...this.tables, table];
            this.newTableNo = '';
            this.messageService.add({
                severity: 'success',
                summary: 'Table Added',
                detail: `Table ${table.tableNo} created`,
            });
        } catch (err: any) {
            this.messageService.add({
                severity: 'error',
                summary: 'Error',
                detail: err?.message || 'Failed to create table',
            });
        } finally {
            this.creatingTable = false;
        }
    }

    async createQuickCarBay(name?: string): Promise<void> {
        const branchId = this.branchContext.getBranchId();
        if (!branchId) return;

        let tableNo = (name || this.newTableNo || '').trim();
        if (!tableNo) {
            const existingCars = this.tables.filter((t) => this.isCarBay(t)).length;
            tableNo = `Car ${existingCars + 1}`;
        }

        this.creatingCarBay = true;
        try {
            const table = await this.restaurantTableService.create({
                branchId,
                tableNo,
                area: 'Car Parking',
                capacity: 4,
                isCarBay: true,
            });
            this.tables = [...this.tables, table];
            this.newTableNo = '';
            this.messageService.add({
                severity: 'success',
                summary: 'Car Bay Added',
                detail: `${table.tableNo} ready — tap it and enter the vehicle number when a car arrives.`,
                life: 4000,
            });
        } catch (err: any) {
            this.messageService.add({
                severity: 'error',
                summary: 'Error',
                detail: err?.message || 'Failed to create car bay',
            });
        } finally {
            this.creatingCarBay = false;
        }
    }

    async createCarBaysBatch(count: number = 5): Promise<void> {
        const branchId = this.branchContext.getBranchId();
        if (!branchId) return;
        this.creatingCarBay = true;
        try {
            const existing = new Set(
                this.tables.filter((t) => this.isCarBay(t)).map((t) => (t.tableNo || '').toLowerCase())
            );
            let created = 0;
            for (let i = 1; i <= count + 20 && created < count; i++) {
                const name = `Car ${i}`;
                if (existing.has(name.toLowerCase())) continue;
                const table = await this.restaurantTableService.create({
                    branchId,
                    tableNo: name,
                    area: 'Car Parking',
                    capacity: 4,
                    isCarBay: true,
                });
                this.tables = [...this.tables, table];
                existing.add(name.toLowerCase());
                created++;
            }
            this.messageService.add({
                severity: 'success',
                summary: 'Car Bays Ready',
                detail: created
                    ? `Added ${created} car bay(s). Tap a bay and enter the plate when a car arrives.`
                    : 'Car bays already exist for this branch.',
                life: 4000,
            });
        } catch (err: any) {
            this.messageService.add({
                severity: 'error',
                summary: 'Error',
                detail: err?.message || 'Failed to create car bays',
            });
        } finally {
            this.creatingCarBay = false;
        }
    }

    focusSearch(): void {
        // Restaurant staff tap menu tiles; avoid opening the mobile keyboard.
        if (this.isRestaurantMode) {
            return;
        }
        setTimeout(() => this.barcodeInput?.nativeElement?.focus(), 50);
    }

    onSearchInput(): void {
        if (this.searchTimer) {
            clearTimeout(this.searchTimer);
        }
        const q = (this.searchText || '').trim();
        if (!q) {
            this.suggestions = [];
            this.suggestionHighlightIndex = -1;
            return;
        }
        // Restaurant: dropdown from loaded menu + keep grid filter via searchText.
        if (this.isRestaurantMode) {
            this.suggestions = this.buildMenuSuggestions(q);
            this.suggestionHighlightIndex = this.suggestions.length ? 0 : -1;
            return;
        }
        this.searchTimer = setTimeout(() => this.loadSuggestions(q), 250);
    }

    onSearchKeydown(event: KeyboardEvent): void {
        const key = event.key;
        if (key === 'ArrowDown') {
            if (!this.suggestions.length) {
                const q = (this.searchText || '').trim();
                if (q && this.isRestaurantMode) {
                    this.suggestions = this.buildMenuSuggestions(q);
                }
            }
            if (!this.suggestions.length) {
                return;
            }
            event.preventDefault();
            this.suggestionHighlightIndex =
                this.suggestionHighlightIndex < 0
                    ? 0
                    : Math.min(
                          this.suggestionHighlightIndex + 1,
                          this.suggestions.length - 1
                      );
            return;
        }
        if (key === 'ArrowUp') {
            if (!this.suggestions.length) {
                return;
            }
            event.preventDefault();
            this.suggestionHighlightIndex = Math.max(
                this.suggestionHighlightIndex - 1,
                0
            );
            return;
        }
        if (key === 'Escape') {
            if (this.suggestions.length) {
                event.preventDefault();
                this.suggestions = [];
                this.suggestionHighlightIndex = -1;
            }
            return;
        }
        if (key === 'Enter') {
            event.preventDefault();
            if (
                this.suggestionHighlightIndex >= 0 &&
                this.suggestions[this.suggestionHighlightIndex]
            ) {
                void this.selectSuggestion(
                    this.suggestions[this.suggestionHighlightIndex]
                );
                return;
            }
            this.onSearchEnter();
        }
    }

    /** Local menu matches for restaurant search dropdown (max 12). */
    private buildMenuSuggestions(keyword: string): ProductDto[] {
        const q = (keyword || '').trim().toLowerCase();
        if (!q) {
            return [];
        }
        return (this.menuProducts || [])
            .filter(
                (p) =>
                    !this.isGroceryProduct(p) &&
                    ((p.name || '').toLowerCase().includes(q) ||
                        (p.barcode || '').toLowerCase().includes(q) ||
                        (p.categoryName || '').toLowerCase().includes(q))
            )
            .slice(0, 12);
    }

    onSearchEnter(): void {
        const code = (this.searchText || '').trim();
        if (!code || this.scanning || this.saving) {
            return;
        }

        // Restaurant: pick first dropdown match → add to cart immediately.
        if (this.isRestaurantMode) {
            const match =
                this.suggestions[0] || this.buildMenuSuggestions(code)[0] || null;
            if (match) {
                void this.selectSuggestion(match);
                return;
            }
            this.messageService.add({
                severity: 'warn',
                summary: 'Not found',
                detail: 'No menu item matches your search',
            });
            return;
        }

        this.scanning = true;
        this.saleService
            .findPosProduct(code)
            .then((product) => {
                if (this.isGroceryProduct(product)) {
                    this.messageService.add({
                        severity: 'warn',
                        summary: 'Not found',
                        detail: 'Product not found',
                    });
                    return;
                }
                this.addProduct(product);
                this.searchText = '';
                this.suggestions = [];
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'warn',
                    summary: 'Not found',
                    detail: error?.message || 'Product not found',
                });
            })
            .finally(() => {
                this.scanning = false;
                this.focusSearch();
            });
    }

    selectSuggestion(product: ProductDto): void {
        if (this.scanning || this.saving) {
            return;
        }
        this.addProduct(product);
        this.searchText = '';
        this.suggestions = [];
        this.suggestionHighlightIndex = -1;
        this.focusSearch();
    }

    getMenuCartQty(productId: number): number {
        return (this.cart || [])
            .filter((l) => l.productId === productId)
            .reduce((sum, l) => sum + (Number(l.quantity) || 0), 0);
    }

    /** Click/source for fly-to-cart when a menu add finally lands in the order. */
    private pendingFlySource: Event | null = null;

    async bumpMenuProduct(product: ProductDto, delta: number, event?: Event): Promise<void> {
        if (!product || this.scanning || this.saving) {
            return;
        }
        if (delta > 0) {
            this.pendingFlySource = event ?? null;
            await this.addProduct(product);
            return;
        }

        const lines = (this.cart || []).filter((l) => l.productId === product.id);
        if (!lines.length) {
            return;
        }

        // Prefer a line that can still be reduced; otherwise try the last line (shows KOT lock message).
        const line =
            [...lines]
                .reverse()
                .find((l) => (Number(l.quantity) || 0) > this.getMinQty(l)) ||
            lines[lines.length - 1];
        this.bumpQty(line, -1);
    }

    /** Empty→sale = included; 0 = free; other >0 = add as +extra when customer selects. */
    hasModifierPlusSign(
        optionPrice: number | null | undefined,
        productSalePrice?: number | null
    ): boolean {
        const price = Number(optionPrice) || 0;
        if (price <= 0) return false;
        const sale = Number(productSalePrice);
        if (Number.isFinite(sale) && sale > 0 && Math.abs(price - sale) < 0.01) {
            return false;
        }
        return true;
    }

    isModifierSalePrice(
        optionPrice: number | null | undefined,
        productSalePrice?: number | null
    ): boolean {
        const sale = Number(productSalePrice);
        const price = Number(optionPrice);
        return (
            Number.isFinite(sale) &&
            sale > 0 &&
            Number.isFinite(price) &&
            price > 0 &&
            Math.abs(price - sale) < 0.01
        );
    }

    get currentCustomisedTotal(): number {
        if (!this.selectedProductForModifiers) return 0;
        const base = this.getUnitPrice(this.selectedProductForModifiers) || 0;
        const salePrice = Number(this.selectedProductForModifiers.price) || 0;
        let extra = 0;
        for (const group of this.availableModifierGroups) {
            const selectedIds = this.selectedModifierOptions[group.id] || [];
            for (const optId of selectedIds) {
                const opt = group.options?.find((o) => o.id === optId);
                if (opt && this.hasModifierPlusSign(opt.price, salePrice)) {
                    extra += Number(opt.price) || 0;
                }
            }
        }
        return base + extra;
    }

    isOptionSelected(groupId: number, optionId: number): boolean {
        const list = this.selectedModifierOptions[groupId];
        return !!list && list.includes(optionId);
    }


    isPizzaProduct(product?: any): boolean {
        if (!product) return false;
        const cat = (product.categoryName || '').trim().toLowerCase();
        if (cat.includes('pizza')) return true;
        const name = (product.name || product.productName || '').trim().toLowerCase();
        if (name.includes('pizza')) return true;
        return false;
    }

    /** Sale-price options (from empty add) show amount without "+". */
    formatModifierOptionPrice(
        optionPrice: number | null | undefined,
        productSalePrice?: number | null
    ): string {
        const price = Number(optionPrice) || 0;
        if (price <= 0) {
            return 'Free';
        }
        const formatted = price.toFixed(2);
        if (this.isModifierSalePrice(price, productSalePrice)) {
            return formatted;
        }
        return `+ ${formatted}`;
    }

    isGroupSingleSelect(group: ProductModifierGroupDto): boolean {
        if (!group) return false;
        if (group.maxSelection === 1 || group.allowMultiple === false) return true;
        const name = (group.name || '').trim().toLowerCase();
        if (name.includes('size') || name.includes('portion')) return true;
        return false;
    }

    toggleModifierOption(group: ProductModifierGroupDto, option: ProductModifierOptionDto): void {
        if (!this.selectedModifierOptions[group.id]) {
            this.selectedModifierOptions[group.id] = [];
        }

        const isSingle = this.isGroupSingleSelect(group);
        const index = this.selectedModifierOptions[group.id].indexOf(option.id);

        if (isSingle) {
            if (index > -1) {
                if (!group.isRequired) {
                    this.selectedModifierOptions[group.id] = [];
                }
            } else {
                this.selectedModifierOptions[group.id] = [option.id];
            }
        } else {
            if (index > -1) {
                if (group.isRequired && this.selectedModifierOptions[group.id].length <= 1) {
                    return;
                }
                this.selectedModifierOptions[group.id].splice(index, 1);
            } else {
                if (group.maxSelection && this.selectedModifierOptions[group.id].length >= group.maxSelection) {
                    this.selectedModifierOptions[group.id].shift();
                }
                this.selectedModifierOptions[group.id].push(option.id);
            }
        }
    }

    async addProduct(product: ProductDto): Promise<void> {
        if (this.isRestaurantMode) {
            // Use flags already on the loaded product â€” do not probe Deal/GetByProductId
            // for every add (non-deal products return 500 and slow the POS).
            if (this.isDealProduct(product)) {
                await this.openDealCustomiseDialog(product);
                return;
            }
            try {
                const groups = await this.productModifierService.getByProduct(product.id);
                if (groups && groups.length > 0) {
                    this.selectedProductForModifiers = product;
                    this.availableModifierGroups = groups;
                    this.selectedModifierOptions = {};
                    this.pendingKitchenNote = '';
                    for (const g of groups) {
                        this.selectedModifierOptions[g.id] = [];
                        if (g.isRequired && g.options?.length) {
                            this.selectedModifierOptions[g.id] = [g.options[0].id];
                        }
                    }
                    this.modifierDialogVisible = true;
                    return;
                }
            } catch (err) {
                console.warn('Could not fetch modifiers for product:', err);
            }
        }
        this.pushToCart(product);
    }

    async openDealCustomiseDialog(product: ProductDto): Promise<void> {
        try {
            this.loadingDealCustomise = true;
            let deal: DealDto | null = null;
            if (product.dealId) {
                deal = await this.dealService.get(product.dealId);
            } else {
                deal = await this.dealService.getByProductId(product.id);
            }

            if (!deal || !deal.items?.length) {
                this.pushToCart(product);
                return;
            }

            this.selectedDealProduct = product;
            this.currentDeal = deal;
            this.dealKitchenNote = '';

            const itemsWithModifiers: DealCustomiseItem[] = [];
            for (const item of deal.items) {
                let groups: ProductModifierGroupDto[] = [];
                try {
                    groups = await this.productModifierService.getByProduct(item.productId);
                } catch (e) {
                    console.warn('Could not load modifiers for deal item:', item.productId, e);
                }

                const selectedOptions: Record<number, number[]> = {};
                for (const g of groups || []) {
                    selectedOptions[g.id] = [];
                    if (g.isRequired && g.options?.length) {
                        selectedOptions[g.id] = [g.options[0].id];
                    }
                }

                itemsWithModifiers.push({
                    productId: item.productId,
                    productName: item.productName || 'Product #' + item.productId,
                    quantity: item.quantity,
                    unitPrice: item.unitPrice,
                    modifierGroups: groups || [],
                    selectedOptions,
                });
            }

            this.dealCustomiseItems = itemsWithModifiers;
            this.dealCustomiseDialogVisible = true;
        } catch (err: any) {
            console.error('Failed to load deal details:', err);
            this.messageService.add({
                severity: 'warn',
                summary: 'Deal Info',
                detail: 'Could not load deal options. Adding deal to cart.',
            });
            this.pushToCart(product);
        } finally {
            this.loadingDealCustomise = false;
        }
    }

    isDealOptionSelected(item: DealCustomiseItem, groupId: number, optionId: number): boolean {
        return (item.selectedOptions[groupId] || []).includes(optionId);
    }

    toggleDealModifierOption(
        item: DealCustomiseItem,
        group: ProductModifierGroupDto,
        option: ProductModifierOptionDto
    ): void {
        if (!item.selectedOptions[group.id]) {
            item.selectedOptions[group.id] = [];
        }
        const list = item.selectedOptions[group.id];
        const isSingle = this.isGroupSingleSelect(group);
        const index = list.indexOf(option.id);

        if (isSingle) {
            if (index > -1) {
                if (!group.isRequired) {
                    item.selectedOptions[group.id] = [];
                }
            } else {
                item.selectedOptions[group.id] = [option.id];
            }
        } else {
            if (index > -1) {
                if (group.isRequired && list.length <= 1) {
                    return;
                }
                list.splice(index, 1);
            } else {
                if (group.maxSelection && list.length >= group.maxSelection) {
                    list.shift();
                }
                list.push(option.id);
            }
        }
    }

    get currentDealCustomisedTotal(): number {
        if (!this.selectedDealProduct) return 0;
        const base = this.currentDeal?.dealPrice ?? this.getUnitPrice(this.selectedDealProduct) ?? 0;
        let extra = 0;
        for (const item of this.dealCustomiseItems) {
            const itemPrice = Number(item.unitPrice) || 0;
            for (const group of item.modifierGroups) {
                const sel = item.selectedOptions[group.id] || [];
                for (const optId of sel) {
                    const opt = group.options?.find((o) => o.id === optId);
                    if (opt && this.hasModifierPlusSign(opt.price, itemPrice)) {
                        extra += Number(opt.price) || 0;
                    }
                }
            }
        }
        return base + extra;
    }

    confirmDealAndAddToCart(): void {
        if (!this.selectedDealProduct || !this.currentDeal) return;

        for (const item of this.dealCustomiseItems) {
            for (const group of item.modifierGroups) {
                const sel = item.selectedOptions[group.id] || [];
                if (group.isRequired && sel.length === 0) {
                    this.messageService.add({
                        severity: 'warn',
                        summary: 'Required Selection',
                        detail: `Please select an option for "${group.name}" in ${item.productName}.`,
                    });
                    return;
                }
            }
        }

        const modifierNames: string[] = [];
        let extraPrice = 0;
        for (const item of this.dealCustomiseItems) {
            const itemOpts: string[] = [];
            const itemPrice = Number(item.unitPrice) || 0;
            for (const group of item.modifierGroups) {
                const sel = item.selectedOptions[group.id] || [];
                for (const optId of sel) {
                    const opt = group.options?.find((o) => o.id === optId);
                    if (opt) {
                        const optPrice = Number(opt.price) || 0;
                        const hasPlus = this.hasModifierPlusSign(optPrice, itemPrice);
                        if (hasPlus) {
                            extraPrice += optPrice;
                        }
                        itemOpts.push(hasPlus ? `${opt.name} (+PKR ${optPrice.toFixed(2)})` : opt.name);
                    }
                }
            }
            if (itemOpts.length > 0) {
                modifierNames.push(`${item.quantity > 1 ? item.quantity + 'x ' : ''}${item.productName}: ${itemOpts.join(', ')}`);
            }
        }

        const dealItemsSummary = this.dealCustomiseItems
            .map((item) => {
                const chosen: string[] = [];
                for (const group of item.modifierGroups) {
                    const sel = item.selectedOptions[group.id] || [];
                    for (const optId of sel) {
                        const opt = group.options?.find((o) => o.id === optId);
                        if (opt) chosen.push(opt.name);
                    }
                }
                return `${item.quantity}x ${item.productName}${chosen.length ? ' (' + chosen.join(', ') + ')' : ''}`;
            })
            .join(' • ');

        this.pushDealToCart(
            this.selectedDealProduct,
            this.currentDeal,
            { names: modifierNames, extraPrice },
            this.dealKitchenNote?.trim() || undefined,
            dealItemsSummary
        );

        this.dealCustomiseDialogVisible = false;
        this.selectedDealProduct = null;
        this.currentDeal = null;
        this.dealCustomiseItems = [];
        this.dealKitchenNote = '';
        this.focusSearch();
    }

    cancelDealCustomise(): void {
        this.dealCustomiseDialogVisible = false;
        this.selectedDealProduct = null;
        this.currentDeal = null;
        this.dealCustomiseItems = [];
        this.dealKitchenNote = '';
        this.focusSearch();
    }

    pushDealToCart(
        product: ProductDto,
        deal: DealDto,
        extraModifiers?: { names: string[]; extraPrice: number },
        kitchenNote?: string,
        dealSummary?: string
    ): void {
        const basePrice = deal.dealPrice || product.price || 0;
        const retailPrice = basePrice + (extraModifiers?.extraPrice || 0);
        const wholesalePrice = basePrice + (extraModifiers?.extraPrice || 0);
        const unitPrice = this.resolveUnitPrice(retailPrice, wholesalePrice);

        // Always include DEAL: summary so Kitchen Order Ticket (KOT), Guest Check, and Table reload know all items!
        const allModifiers = [
            `DEAL: ${dealSummary || product.name}`,
            ...(extraModifiers?.names || [])
        ];
        const modJson = JSON.stringify(allModifiers);

        this.cart.push({
            productId: product.id,
            productName: product.name,
            quantity: 1,
            unitPrice,
            retailPrice,
            wholesalePrice,
            stockQuantity: 9999,
            kitchenNotes: kitchenNote,
            selectedModifiers: modJson,
            isDeal: true,
            dealId: deal.id,
            dealSummary: dealSummary,
            isSentToKitchen: false,
        });

        this.messageService.add({
            severity: 'success',
            summary: 'Deal Added',
            detail: `Added "${product.name}" to order.`,
        });
    }

    async viewCartDealDetails(line: PosCartLine): Promise<void> {
        this.selectedCartDealLine = line;
        if (
            this.isDealCartLine(line) &&
            !line.dealSummary &&
            !this.getDealItemsFromLine(line).length
        ) {
            try {
                const dealInfo = line.dealId
                    ? await this.dealService.get(line.dealId)
                    : await this.dealService.getByProductId(line.productId);
                if (dealInfo && dealInfo.items?.length) {
                    line.dealSummary = dealInfo.items.map((i) => `${i.quantity}x ${i.productName}`).join(' â€¢ ');
                    line.dealId = dealInfo.id;
                }
            } catch (err) {
                console.warn('Could not load deal details for dialog:', err);
            }
        }
        this.viewCartDealDialogVisible = true;
    }

    confirmModifiersAndAddToCart(): void {
        if (!this.selectedProductForModifiers) return;

        for (const group of this.availableModifierGroups) {
            const selectedIds = this.selectedModifierOptions[group.id] || [];
            if (group.isRequired && selectedIds.length === 0) {
                this.messageService.add({
                    severity: 'warn',
                    summary: 'Required Selection',
                    detail: `Please select an option for "${group.name}".`,
                });
                return;
            }
        }

        let extraPrice = 0;
        const modifierNames: string[] = [];
        const salePrice = Number(this.selectedProductForModifiers.price) || 0;

        for (const group of this.availableModifierGroups) {
            const selectedIds = this.selectedModifierOptions[group.id] || [];
            for (const optId of selectedIds) {
                const opt = group.options?.find((o) => o.id === optId);
                if (opt) {
                    const optPrice = Number(opt.price) || 0;
                    const hasPlus = this.hasModifierPlusSign(optPrice, salePrice);
                    if (hasPlus) {
                        extraPrice += optPrice;
                    }
                    const priceLabel = this.formatModifierOptionPrice(
                        optPrice,
                        salePrice
                    );
                    modifierNames.push(
                        hasPlus
                            ? `${opt.name} (${priceLabel})`
                            : opt.name
                    );
                }
            }
        }

        this.pushToCart(
            this.selectedProductForModifiers,
            { names: modifierNames, extraPrice },
            this.pendingKitchenNote?.trim() || undefined
        );

        this.modifierDialogVisible = false;
        this.selectedProductForModifiers = null;
        this.availableModifierGroups = [];
        this.selectedModifierOptions = {};
        this.pendingKitchenNote = '';
        this.focusSearch();
    }

    /** Fly product image into the Order Details tab (#storeCartFlyTarget). */
    private flyAddedProductToOrder(product: ProductDto, event?: Event | null): void {
        if (!this.isRestaurantMode) {
            return;
        }
        const source =
            event ??
            document.querySelector(
                `.pos-menu-tile img[src], .pos-menu-tile.in-cart, .pos-menu-tile`
            );
        const tile =
            (source instanceof Element && source.closest?.('.pos-menu-tile')) ||
            (source instanceof Element ? source : null);
        const imageEl =
            findProductImageNear(source) ||
            (tile instanceof Element ? tile.querySelector('img') : null);
        flyToCart(source, {
            imageEl,
            imageUrl: product?.imagePath ? this.getMenuProductImage(product) : undefined,
            targetSelector: '#storeCartFlyTarget',
            highlightOnly: true,
        });
    }

    cancelModifiers(): void {
        this.modifierDialogVisible = false;
        this.selectedProductForModifiers = null;
        this.availableModifierGroups = [];
        this.selectedModifierOptions = {};
        this.pendingKitchenNote = '';
        this.pendingFlySource = null;
        this.focusSearch();
    }

    pushToCart(
        product: ProductDto,
        extraModifiers?: { names: string[]; extraPrice: number },
        kitchenNote?: string
    ): void {
        const retailPrice = (product.price || 0) + (extraModifiers?.extraPrice || 0);
        const wholesalePrice = (product.wholesalePrice || 0) + (extraModifiers?.extraPrice || 0);
        const unitPrice = this.resolveUnitPrice(retailPrice, wholesalePrice);

        const modJson = extraModifiers?.names?.length ? JSON.stringify(extraModifiers.names) : undefined;
        const existing = !modJson && !kitchenNote
            ? this.cart.find((x) => x.productId === product.id && !x.selectedModifiers && !x.kitchenNotes)
            : null;

        if (existing) {
            if (!this.isRestaurantMode && existing.quantity + 1 > (product.stockQuantity || 0)) {
                this.messageService.add({
                    severity: 'warn',
                    summary: 'Stock',
                    detail: 'Insufficient stock',
                });
                this.pendingFlySource = null;
                return;
            }
            existing.quantity += 1;
            existing.retailPrice = retailPrice;
            existing.wholesalePrice = wholesalePrice;
            existing.unitPrice = unitPrice;
            existing.stockQuantity = product.stockQuantity || 0;
            this.flyAddedProductToOrder(product, this.pendingFlySource);
            this.pendingFlySource = null;
            return;
        }

        if (!this.isRestaurantMode && (product.stockQuantity || 0) < 1) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Stock',
                detail: `"${product.name}" is out of stock. Add stock via Purchases first.`,
            });
            this.pendingFlySource = null;
            return;
        }

        this.cart.push({
            productId: product.id,
            productName: product.name,
            quantity: 1,
            unitPrice,
            retailPrice,
            wholesalePrice,
            stockQuantity: product.stockQuantity || 0,
            kitchenNotes: kitchenNote || undefined,
            selectedModifiers: modJson,
            isSentToKitchen: false,
        });
        this.flyAddedProductToOrder(product, this.pendingFlySource);
        this.pendingFlySource = null;
    }

    getUnitPrice(product: ProductDto): number {
        return this.resolveUnitPrice(product.price || 0, product.wholesalePrice || 0);
    }

    private resolveUnitPrice(retailPrice: number, wholesalePrice: number): number {
        const customer = this.customers.find((c) => c.id === this.customerId);
        const isWholesaler = customer?.customerType === CustomerType.Wholesaler;
        if (isWholesaler) {
            return wholesalePrice > 0 ? wholesalePrice : retailPrice;
        }
        return retailPrice;
    }

    onCustomerSelected(): void {
        for (const line of this.cart) {
            line.unitPrice = this.resolveUnitPrice(line.retailPrice, line.wholesalePrice);
        }
        void this.loadSavedAddressesForPosCustomer();
        this.focusSearch();
    }

    async loadDeliveryAreasForPos(): Promise<void> {
        const branchId = this.branchContext.getBranchId() || 0;
        if (!branchId) {
            this.deliveryAreas = [];
            return;
        }
        try {
            this.deliveryAreas = await this.deliveryAreaService.getActiveAreasForBranch(branchId);
            if (this.deliveryAreas.length && !this.selectedDeliveryAreaId) {
                this.selectedDeliveryAreaId = this.deliveryAreas[0].id;
            }
        } catch {
            this.deliveryAreas = [];
        }
    }

    async loadSavedAddressesForPosCustomer(): Promise<void> {
        if (!this.customerId) {
            this.savedAddresses = [];
            this.selectedSavedAddressId = null;
            return;
        }
        this.loadingSavedAddresses = true;
        try {
            this.savedAddresses = await this.customerOrderService.getSavedAddresses({
                customerId: this.customerId,
            });
            const def = this.savedAddresses.find((a) => a.isDefault) || this.savedAddresses[0];
            if (def && !this.deliveryAddress.trim()) {
                this.applyPosSavedAddress(def);
            }
        } catch {
            this.savedAddresses = [];
        } finally {
            this.loadingSavedAddresses = false;
        }
    }

    onPosSavedAddressPicked(id: number | null): void {
        const addr = id ? this.savedAddresses.find((a) => a.id === id) : null;
        this.applyPosSavedAddress(addr || null);
    }

    applyPosSavedAddress(addr: CustomerSavedAddressDto | null): void {
        if (!addr) {
            this.selectedSavedAddressId = null;
            return;
        }
        this.selectedSavedAddressId = addr.id;
        this.deliveryAddress = addr.address || '';
        this.deliveryLandmark = addr.landmark || '';
        if (addr.deliveryAreaId && this.deliveryAreas.some((a) => a.id === addr.deliveryAreaId)) {
            this.selectedDeliveryAreaId = addr.deliveryAreaId;
        }
    }

    private async persistPosDeliveryAddress(): Promise<void> {
        if (!this.customerId || !this.deliveryAddress.trim()) {
            return;
        }
        const area = this.selectedDeliveryArea;
        try {
            await this.customerOrderService.saveSavedAddress({
                customerId: this.customerId,
                address: this.deliveryAddress.trim(),
                landmark: this.deliveryLandmark.trim() || undefined,
                deliveryAreaId: this.selectedDeliveryAreaId,
                deliveryAreaName: area?.name,
                isDefault: true,
            });
            void this.loadSavedAddressesForPosCustomer();
        } catch {
            /* non-blocking for settle */
        }
    }

    private buildDeliveryNoteSuffix(): string {
        const parts: string[] = [];
        if (this.deliveryAddress.trim()) {
            parts.push(`📍 ${this.deliveryAddress.trim()}`);
        }
        if (this.deliveryLandmark.trim()) {
            parts.push(`🚩 ${this.deliveryLandmark.trim()}`);
        }
        if (this.selectedDeliveryArea?.name) {
            parts.push(`Zone: ${this.selectedDeliveryArea.name}`);
        }
        return parts.join(' | ');
    }

    canRemoveLine(line: PosCartLine): boolean {
        if (!this.isRestaurantMode) {
            return true;
        }
        return !line.isSentToKitchen && !(line.printedQuantity && line.printedQuantity > 0);
    }

    getMinQty(line: PosCartLine): number {
        if (!this.isRestaurantMode) {
            return 0.01;
        }
        if (line.isSentToKitchen || (line.printedQuantity && line.printedQuantity > 0)) {
            return line.printedQuantity || line.quantity || 1;
        }
        return 0.01;
    }

    bumpQty(line: PosCartLine, delta: number): void {
        const min = this.getMinQty(line);
        const next = Number(((line.quantity || 0) + delta).toFixed(2));
        if (this.isRestaurantMode && delta < 0 && next < min) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Permission Denied',
                detail: `Cannot reduce quantity below previously sent amount (${min}).`,
            });
            return;
        }
        if (next <= 0) {
            if (!this.canRemoveLine(line)) {
                this.messageService.add({
                    severity: 'warn',
                    summary: 'Permission Denied',
                    detail: 'Items already sent to KOT cannot be removed. To cancel, please use Cancel KOT / Cancel Order.',
                });
                return;
            }
            this.removeLine(line);
            return;
        }
        this.applyQty(line, next);
    }

    updateQty(line: PosCartLine, qty: number | null): void {
        if (qty == null || Number.isNaN(Number(qty))) {
            return;
        }

        const value = Number(qty);
        const min = this.getMinQty(line);
        if (this.isRestaurantMode && value < min) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Permission Denied',
                detail: `Cannot reduce quantity below previously sent amount (${min}).`,
            });
            line.quantity = min;
            return;
        }

        if (value <= 0) {
            line.quantity = 1;
            return;
        }

        this.applyQty(line, value);
    }

    private applyQty(line: PosCartLine, qty: number): void {
        if (!this.isRestaurantMode && qty > line.stockQuantity) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Stock',
                detail: 'Insufficient stock',
            });
            line.quantity = line.stockQuantity;
            return;
        }
        line.quantity = qty;
    }

    removeLine(line: PosCartLine): void {
        if (!this.canRemoveLine(line)) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Permission Denied',
                detail: 'Previously sent items cannot be removed. To cancel, please use Cancel KOT / Cancel Order.',
            });
            return;
        }
        this.cart = this.cart.filter((x) => x !== line);
        this.focusSearch();
    }

    clearCart(): void {
        this.cart = [];
        this.applyBranchPricing();
        this.notes = '';
        this.paymentDialogVisible = false;
        this.paidAmount = 0;
        this.syncActiveOrderMeta(null);
        this.selectedTable = null;
        this.selectedCarOrder = null;
        this.selectedCarName = '';
        this.selectedCarNumber = '';
        this.selectedCarLocation = '';
        this.thirdPartyChannel = null;
        this.syncSelectedChannelFromOrderState();
        this.discountPercent = 0;
        this.discountAmount = 0;
        this.taxPercent = 0;
        this.serviceChargePercent = 0;
        this.router.navigate([], { queryParams: {}, replaceUrl: true });
        if (this.isRestaurantMode) {
            this.loadUnpaidTakeaways();
            if (this.takeawayAlarmService) {
                this.takeawayAlarmService.checkImmediately();
            }
        }
        this.focusSearch();
    }

    get hasUnsentItems(): boolean {
        return this.cart.some((line) => !line.isSentToKitchen);
    }

    get settleButtonTooltip(): string {
        if (this.isWaiter) {
            return 'Waiters cannot settle payments. Ask an Admin or Cashier.';
        }
        if (this.isRestaurantMode && this.orderType === 'dineIn') {
            if (!this.selectedTable && !this.isCarService) {
                return 'Please select a table or car and send order first.';
            }
            if (this.hasUnsentItems) {
                return this.isCarService
                    ? 'Send dishes to car first before settling & closing.'
                    : 'Send dishes to table first before settling & closing.';
            }
        }
        return '';
    }

    canSave(): boolean {
        // Waiters in Restaurant mode cannot settle payments or complete sales
        if (this.isWaiter) {
            return false;
        }

        if (!this.customerId || this.cart.length === 0 || this.saving) {
            return false;
        }

        // Restaurant Dine-In mode: cannot settle/close table/car until items are sent to kitchen
        if (this.isRestaurantMode && this.orderType === 'dineIn') {
            if (!this.selectedTable && !this.isCarService) {
                return false;
            }
            if (this.hasUnsentItems) {
                return false;
            }
        }

        return true;
    }

    onPaidAmountFocus(event: any): void {
        const input = event?.originalEvent?.target as HTMLInputElement;
        if (input?.select) {
            setTimeout(() => input.select(), 0);
        }
    }

    onPaymentDialogHide(): void {
        if (!this.saving) {
            this.paymentDialogVisible = false;
        }
    }

    completeSale(): void {
        if (this.isWaiter) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Access Restricted',
                detail: 'Waiters are not authorized to settle payments or close bills.',
            });
            return;
        }

        if (!this.canSave()) {
            return;
        }

        const invalidLine = this.cart.find(
            (line) => !line.quantity || line.quantity <= 0
        );
        if (invalidLine) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Validation',
                detail: `Enter a valid quantity for "${invalidLine.productName}".`,
            });
            return;
        }

        this.paidAmount = this.grandTotal;
        this.paymentDialogVisible = true;
    }

    async confirmPaymentAndSave(): Promise<void> {
        if (this.isWaiter) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Access Restricted',
                detail: 'Waiters are not authorized to settle payments.',
            });
            return;
        }

        if (!this.canSave()) {
            return;
        }

        if (this.paidAmount == null || this.paidAmount < 0) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Validation',
                detail: 'Enter the amount paid by the customer',
            });
            return;
        }

        const total = this.grandTotal;
        const paid = Math.round((Number(this.paidAmount) || 0) * 100) / 100;
        const change = Math.max(0, Math.round((paid - total) * 100) / 100);
        const appliedCash = Math.min(paid, total);
        const remaining = Math.max(0, Math.round((total - paid) * 100) / 100);

        const paymentType =
            paid >= total ? PaymentType.Cash : PaymentType.Mixed;
        const cashAmount = paid >= total ? 0 : appliedCash;
        const cardAmount = 0;
        let saleNote = this.notes?.trim() || '';
        const deliveryNote = this.buildDeliveryNoteSuffix();
        if (deliveryNote) {
            saleNote = saleNote ? `${saleNote} | ${deliveryNote}` : deliveryNote;
        }

        this.saving = true;

        try {
            await this.persistPosDeliveryAddress();

            if (this.isRestaurantMode) {
                const branchId = this.branchContext.getBranchId();
                if (!this.activeOrderId && branchId) {
                    const loc = this.buildOrderLocationFields(this.orderType === 'takeaway');
                    const payload = {
                        branchId,
                        ...loc,
                        customerId: this.customerId,
                        waiterName: this.waiterName || 'Staff',
                        notes: this.notes,
                        discountPercent: this.discountPercent || 0,
                        discountAmount: this.discountAmount || 0,
                        taxPercent: this.taxPercent || 0,
                        serviceChargePercent: this.serviceChargePercent || 0,
                        lines: this.cart.map((line) => ({
                            id: line.id,
                            productId: line.productId,
                            productName: line.productName,
                            quantity: line.quantity,
                            unitPrice: line.unitPrice,
                            kitchenNotes: line.kitchenNotes,
                            selectedModifiers: line.selectedModifiers,
                            isSentToKitchen: line.isSentToKitchen || false,
                        })),
                    };
                    const saved = await this.restaurantOrderService.saveOrder(payload);
                    this.syncActiveOrderMeta(saved);
                }

                const branch = this.branchContext.getCurrentBranch();
                const isCar = this.isCarService;
                const settledReceipt = {
                    branchName: branch?.name || 'SmartPOS Restaurant',
                    branchAddress: branch?.invoiceAddress,
                    branchPhone: branch?.invoiceContactPhone,
                    branchFooter: branch?.invoiceFooter,
                    tokenNo: this.activeTokenNo,
                    orderNo: this.activeOrderNo || `ORD-${Date.now().toString().slice(-6)}`,
                    tableNo: isCar
                        ? `Car: ${this.selectedCarName} - ${this.selectedCarNumber}`
                        : (this.orderType === 'dineIn' ? (this.selectedTable?.tableNo || 'Dine-In') : 'Takeaway'),
                    carName: isCar ? this.selectedCarName : undefined,
                    carNumber: isCar ? this.selectedCarNumber : undefined,
                    carLocation: isCar ? this.selectedCarLocation : undefined,
                    serverName: this.waiterName || 'Staff',
                    date: new Date(),
                    lines: this.cart.map((l) => ({
                        productName: l.productName,
                        quantity: l.quantity,
                        unitPrice: l.unitPrice,
                        lineTotal: l.quantity * l.unitPrice,
                        isDeal: this.isDealItemOrModifiers(l),
                        dealItems: this.getDealItemsFromLine(l),
                        modifiers: this.getExtraModifiersFromLine(l),
                        notes: l.kitchenNotes
                    })),
                    subTotal: this.subTotal,
                    discountPercent: this.discountPercent,
                    discountAmount: this.computedDiscount,
                    taxPercent: this.taxPercent,
                    taxAmount: this.taxAmount,
                    serviceChargePercent: this.serviceChargePercent,
                    serviceChargeAmount: this.serviceChargeAmount,
                    grandTotal: total,
                    paidAmount: paid,
                    changeAmount: change,
                    paymentType: paymentType === PaymentType.Cash ? 'CASH' : 'MIXED / CARD',
                };

                await this.restaurantOrderService.settleOrder({
                    orderId: this.activeOrderId!,
                    customerId: (this.customerId || this.customers?.[0]?.id || 1) as number,
                    paymentType,
                    cashAmount,
                    cardAmount,
                    notes: saleNote || undefined,
                });

                if (this.takeawayAlarmService) {
                    this.takeawayAlarmService.checkImmediately();
                }
                this.loadUnpaidTakeaways();

                const detail =
                    change > 0
                        ? `Order settled. Return ${change.toFixed(2)} change to customer.`
                        : remaining > 0
                          ? `Order settled. Balance of ${remaining.toFixed(2)} posted on customer credit.`
                          : 'Order settled successfully';

                this.messageService.add({
                    severity: 'success',
                    summary: 'Order Completed',
                    detail,
                });

                this.paymentDialogVisible = false;
                this.clearCart();
                this.selectedCarOrder = null;
                this.selectedCarName = '';
                this.selectedCarNumber = '';
                this.selectedCarLocation = '';
                this.loadTables();
                this.loadTopProducts();

                this.completedReceipt = settledReceipt;
                this.completedReceiptVisible = true;
                this.printReceiptSnapshot(settledReceipt);
            } else {
                const branch = this.branchContext.getCurrentBranch();
                const retailReceipt = {
                    branchName: branch?.name || 'SmartPOS Store',
                    branchAddress: branch?.invoiceAddress,
                    branchPhone: branch?.invoiceContactPhone,
                    orderNo: `SALE-${Date.now().toString().slice(-6)}`,
                    tableNo: 'Retail POS Sale',
                    serverName: 'Cashier',
                    cashierName: this.authService.getPrinterIdentity().name,
                    ntn: branch?.taxNumber || '',
                    date: new Date(),
                    lines: this.cart.map((l) => ({
                        productName: l.productName,
                        quantity: l.quantity,
                        unitPrice: l.unitPrice,
                        lineTotal: l.quantity * l.unitPrice,
                    })),
                    subTotal: this.subTotal,
                    discountPercent: this.discountPercent,
                    discountAmount: this.computedDiscount || 0,
                    taxPercent: this.taxPercent,
                    taxAmount: this.taxAmount,
                    serviceChargePercent: 0,
                    serviceChargeAmount: 0,
                    grandTotal: total,
                    paidAmount: paid,
                    changeAmount: change,
                    paymentType: paymentType === PaymentType.Cash ? 'CASH' : 'MIXED / CARD',
                };

                await this.saleService.create({
                    customerId: this.customerId as number,
                    saleDate: new Date().toISOString(),
                    notes: saleNote || undefined,
                    discountAmount: this.discountAmount || 0,
                    discountPercent: this.discountPercent || 0,
                    taxPercent: this.taxPercent || 0,
                    paymentType,
                    cashAmount,
                    cardAmount,
                    deliveryCharges:
                        this.showDeliveryAddressPanel && this.deliveryAddress.trim()
                            ? this.deliveryCharges
                            : 0,
                    deliveryAreaName: this.selectedDeliveryArea?.name,
                    lines: this.cart.map((line) => ({
                        productId: line.productId,
                        quantity: line.quantity,
                        unitPrice: line.unitPrice,
                    })),
                });

                const detail =
                    change > 0
                        ? `Sale completed. Return ${change.toFixed(2)} change to the customer.`
                        : remaining > 0
                          ? `Sale completed. Remaining ${remaining.toFixed(2)} posted on credit.`
                          : 'Sale completed successfully';

                this.messageService.add({
                    severity: 'success',
                    summary: 'Success',
                    detail,
                });
                this.paymentDialogVisible = false;
                this.clearCart();
                this.loadTopProducts();

                this.completedReceipt = retailReceipt;
                this.completedReceiptVisible = true;
                this.printReceiptSnapshot(retailReceipt);
            }
        } catch (error: any) {
            this.messageService.add({
                severity: 'error',
                summary: 'Error',
                detail: error?.message || 'Failed to complete sale',
            });
        } finally {
            this.saving = false;
            this.focusSearch();
        }
    }

    reprintCompletedReceipt(): void {
        if (this.completedReceipt) {
            this.printReceiptSnapshot(this.completedReceipt);
        }
    }

    get completedReceiptLogoUrl(): string {
        return this.branchService.getImageUrl(
            this.branchContext.getCurrentBranch()?.imagePath
        );
    }

    get completedReceiptCity(): string {
        const addr = (
            this.completedReceipt?.branchAddress ||
            this.branchContext.getCurrentBranch()?.invoiceAddress ||
            ''
        ).trim();
        if (!addr) {
            return 'Faisalabad';
        }
        const parts = addr.split(',').map((p: string) => p.trim()).filter(Boolean);
        return parts.length ? parts[parts.length - 1] : addr;
    }

    get completedReceiptOrderType(): string {
        const r = this.completedReceipt;
        if (!r) {
            return 'Sale';
        }
        if (r.carNumber) {
            return 'Car Service';
        }
        const table = String(r.tableNo || '').toLowerCase();
        if (table.includes('takeaway') || table.includes('take-away')) {
            return 'Take-Away';
        }
        if (table.includes('delivery') || table.includes('retail')) {
            return table.includes('retail') ? 'Sale' : 'Online Delivery';
        }
        if (table.includes('dine') || this.orderType === 'dineIn') {
            return 'Dine-In';
        }
        return 'Take-Away';
    }

    get completedReceiptSubLocation(): string {
        const r = this.completedReceipt;
        if (!r) {
            return '';
        }
        if (r.carNumber) {
            return `Car: ${r.carName || ''} (${r.carNumber})`;
        }
        if (this.completedReceiptOrderType === 'Dine-In' && r.tableNo) {
            return `Table: ${r.tableNo}`;
        }
        return '';
    }

    formatReceiptDate(value: Date | string | null | undefined): string {
        const d = value ? new Date(value) : new Date();
        if (Number.isNaN(d.getTime())) {
            return '';
        }
        const day = String(d.getDate()).padStart(2, '0');
        const month = String(d.getMonth() + 1).padStart(2, '0');
        const year = String(d.getFullYear()).slice(-2);
        let hours = d.getHours();
        const minutes = String(d.getMinutes()).padStart(2, '0');
        const seconds = String(d.getSeconds()).padStart(2, '0');
        const ampm = hours >= 12 ? 'pm' : 'am';
        hours = hours % 12 || 12;
        return `${day}/${month}/${year} ${String(hours).padStart(2, '0')}:${minutes}:${seconds}${ampm}`;
    }

    get completedReceiptUan(): string {
        const phone = (
            this.completedReceipt?.branchPhone ||
            this.branchContext.getCurrentBranch()?.invoiceContactPhone ||
            '03080004248'
        ).trim();
        return /^uan\b/i.test(phone) ? phone : `UAN ${phone}`;
    }

    get completedReceiptSoftwareBy(): string {
        const branch = this.branchContext.getCurrentBranch();
        const raw = (branch?.softwareDeveloperName || 'Software By AlmahirHub').trim();
        if (/^software\s+by\b/i.test(raw)) {
            return raw;
        }
        if (/^software\s+develop/i.test(raw)) {
            return raw.replace(/^software\s+develop(?:ed)?\s+by\b/i, 'Software By');
        }
        return `Software By ${raw}`;
    }

    get completedReceiptSoftwarePhone(): string {
        return (
            this.branchContext.getCurrentBranch()?.softwareDeveloperPhone ||
            '03167854606'
        ).trim();
    }

    get completedReceiptInvoiceFooter(): string {
        return (
            this.completedReceipt?.branchFooter ||
            this.branchContext.getCurrentBranch()?.invoiceFooter ||
            ''
        ).trim();
    }

    printReceiptSnapshot(receipt: any): void {
        if (this.isRestaurantMode) {
            const branchId = this.branchContext.getBranchId();
            if (branchId) {
                this.restaurantOrderService
                    .recordPrintLog({
                        branchId,
                        orderId: receipt.orderId || this.activeOrderId,
                        orderNo: receipt.orderNo,
                        receiptType: 'SaleReceipt',
                    })
                    .catch(() => {});
            }
        }

        this.thermalPrintService.printPosReceipt(receipt);
    }
    private loadSuggestions(keyword: string): void {
        this.saleService
            .searchPosProducts(keyword, null)
            .then((items) => {
                if ((this.searchText || '').trim() !== keyword) {
                    return;
                }
                this.suggestions = (items || []).filter((p) => !this.isGroceryProduct(p));
                this.suggestionHighlightIndex = this.suggestions.length ? 0 : -1;
            })
            .catch(() => {
                this.suggestions = [];
                this.suggestionHighlightIndex = -1;
            });
    }

    private toDateInputValue(date: Date = new Date()): string {
        const year = date.getFullYear();
        const month = `${date.getMonth() + 1}`.padStart(2, '0');
        const day = `${date.getDate()}`.padStart(2, '0');
        return `${year}-${month}-${day}`;
    }

    ngOnDestroy(): void {
        this.branchSub?.unsubscribe();
        this.alarmOrdersSub?.unsubscribe();
        this.stopAssignedOrdersClock();
    }
}
