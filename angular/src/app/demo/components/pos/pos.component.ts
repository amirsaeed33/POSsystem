import {
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
    RestaurantOrderDto,
    RestaurantOrderStatuses,
    SendToKitchenResultDto,
    UnpaidTakeawayOrderDto,
} from 'src/app/demo/api/restaurant-order';
import {
    ProductModifierGroupDto,
    ProductModifierOptionDto,
} from 'src/app/demo/api/product-modifier';
import { BranchContextService } from 'src/app/demo/service/branch-context.service';
import { RestaurantTableService } from 'src/app/demo/service/restaurant-table.service';
import { RestaurantOrderService } from 'src/app/demo/service/restaurant-order.service';
import { ProductModifierService } from 'src/app/demo/service/product-modifier.service';
import { SaleService } from 'src/app/demo/service/sale.service';
import { StaffDto } from 'src/app/demo/api/staff';
import { StaffService } from 'src/app/demo/service/staff.service';
import { DealService } from 'src/app/demo/service/deal.service';
import { DealDto } from 'src/app/demo/api/deal';
import { AuthService } from 'src/app/demo/service/auth.service';
import { ActivatedRoute, Router } from '@angular/router';
import { RestaurantTakeawayAlarmService } from 'src/app/demo/service/restaurant-takeaway-alarm.service';

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

    private branchSub?: Subscription;

    saving = false;
    scanning = false;
    searchText = '';
    suggestions: ProductDto[] = [];
    customers: CustomerDto[] = [];
    cart: PosCartLine[] = [];

    customerId: number | null = null;
    notes = '';
    discountAmount = 0;
    discountPercent = 0;
    taxPercent = 0;

    // Restaurant Mode
    isRestaurantMode = false;
    serviceChargePercent = 0;
    orderType: 'dineIn' | 'takeaway' = 'dineIn';
    tables: RestaurantTableDto[] = [];
    selectedTable: RestaurantTableDto | null = null;
    tableDialogVisible = false;
    loadingTables = false;
    pendingSendToTable = false;

    // Car bay (permanent Car 1â€“N slots) + vehicle plate on select
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
        return this.selectedCarNumber ? `${bay} â€¢ ${this.selectedCarNumber}` : bay;
    }


    // Active Running Order (Hold & Tabs)
    activeOrderId: number | null = null;
    activeOrderNo = '';
    waiterName = '';
    savingOrder = false;
    sendingKOT = false;
    printingBill = false;

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
    paidAmount = 0;

    unpaidTakeawayOrders: UnpaidTakeawayOrderDto[] = [];
    unpaidTakeawayDialogVisible = false;
    orderToCancel: UnpaidTakeawayOrderDto | null = null;
    cancelTakeawayDialogVisible = false;
    cancellingTakeawayOrder = false;
    private alarmOrdersSub?: Subscription;

    private searchTimer: ReturnType<typeof setTimeout> | null = null;

    constructor(
        private saleService: SaleService,
        private restaurantTableService: RestaurantTableService,
        private restaurantOrderService: RestaurantOrderService,
        private productModifierService: ProductModifierService,
        private branchContext: BranchContextService,
        private staffService: StaffService,
        private dealService: DealService,
        private authService: AuthService,
        private route: ActivatedRoute,
        private router: Router,
        public takeawayAlarmService: RestaurantTakeawayAlarmService,
        private messageService: MessageService
    ) {}

    get isWaiter(): boolean {
        return this.isRestaurantMode && this.authService.isWaiter();
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
        return (
            Math.round(
                (this.subTotal -
                    this.computedDiscount +
                    this.taxAmount +
                    this.serviceChargeAmount) *
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
    private lastLoadedBranchId: number | null = null;
    private posInitDone = false;

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
        }
    }

    private async reloadForBranch(branchId: number): Promise<void> {
        this.lastLoadedBranchId = branchId;
        this.applyBranchPricing();
        this.loadTopProducts();
        this.loadPosCustomers();

        if (this.isRestaurantMode) {
            if (this.isWaiter) {
                this.waiterName = this.authService.getPrinterIdentity().name;
            } else if (this.takeawayAlarmService && !this.alarmOrdersSub) {
                this.alarmOrdersSub = this.takeawayAlarmService.unpaidOrders$.subscribe((orders) => {
                    this.unpaidTakeawayOrders = orders || [];
                });
            }
            this.loadUnpaidTakeaways();
        }
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
        const parts = summary.includes(' â€¢ ') ? summary.split(' â€¢ ') : summary.split(',');
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
            // Status 1 = Occupied, 2 = Billed â€” both mean an active order is running
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
            this.activeOrderId = saved.id;
            this.activeOrderNo = saved.orderNo;
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
                    ? `${this.carServiceLabel} saved â€” pick another bay or table when ready.`
                    : `Table ${this.selectedTable?.tableNo} saved â€” pick another table or car when ready.`,
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
        // Switching away from a different active table/car â€” park current cart first
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
            this.activeOrderId = null;
            this.activeOrderNo = '';
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
            await this.selectTable(table);
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
            await this.selectTable(table);
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
            return {
                id: null,
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

            // Already on this table/car â€” just update waiter, don't reopen session
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
                    this.activeOrderId = null;
                    this.activeOrderNo = '';
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

            this.activeOrderId = null;
            this.activeOrderNo = '';

            if (!isDirectSend) {
                this.messageService.add({
                    severity: 'info',
                    summary: carBay ? 'Car Bay Selected' : 'Table Selected',
                    detail: carBay
                        ? `${this.carServiceLabel} selected — select products and click "Send to Car".`
                        : `Table ${table.tableNo} selected — select products and click "Send to Table".`,
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
        this.activeOrderId = null;
        this.activeOrderNo = '';
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
        return this.tables.filter(
            (t) => t.id !== this.selectedTable?.id && t.status === 0
        );
    }

    openTransferDialog(): void {
        if (!this.selectedTable) {
            this.messageService.add({
                severity: 'warn',
                summary: 'No Table',
                detail: 'Select an active table first to transfer.',
            });
            return;
        }
        this.selectedTargetTableId = null;
        this.transferDialogVisible = true;
    }

    async confirmTransfer(): Promise<void> {
        if (!this.selectedTable || !this.selectedTargetTableId) return;
        this.transferringTable = true;
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
                summary: 'Table Transferred',
                detail: `Order moved from Table ${this.selectedTable.tableNo} to Table ${target?.tableNo || ''}`,
            });
            this.transferDialogVisible = false;
            await this.loadTables();
            if (target) {
                this.selectedTable = target;
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
            this.activeOrderId = null;
            this.activeOrderNo = '';
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

            this.activeOrderId = order.id;
            this.activeOrderNo = order.orderNo;
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

                    try {
                        const dealInfo = await this.dealService.getByProductId(l.productId);
                        if (dealInfo && dealInfo.items?.length) {
                            isDeal = true;
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
                detail: this.orderType === 'takeaway' ? 'Add food items before sending to KOT.' : 'Add food items before sending to the table.',
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
                    kitchenNotes: line.kitchenNotes,
                    selectedModifiers: line.selectedModifiers,
                    isSentToKitchen: line.isSentToKitchen || false,
                    printedQuantity: line.printedQuantity || 0,
                })),
            };

            const savedOrder = await this.restaurantOrderService.saveOrder(payload);
            this.activeOrderId = savedOrder.id;
            this.activeOrderNo = savedOrder.orderNo;
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
                    }
                });
            }

            const kotResult = await this.restaurantOrderService.sendToKitchen(savedOrder.id);
            this.kotResult = kotResult;

            this.cart.forEach((l) => (l.isSentToKitchen = true));
            if (!isTakeaway) {
                await this.loadTables();
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
                this.kotDialogVisible = true;
                this.messageService.add({
                    severity: 'success',
                    summary: isCar ? 'ðŸš— Sent to Car' : (isTakeaway ? 'ðŸ³ Sent to KOT' : 'ðŸ½ï¸ Sent to Table'),
                    detail: isCar
                        ? `Order sent to Car ${this.selectedCarName} (${this.selectedCarNumber}) (Order #${kotResult.orderNo})`
                        : (isTakeaway
                            ? `Takeaway order #${kotResult.orderNo} sent to kitchen KOT (Unpaid)`
                            : `Order sent to Table ${kotResult.tableNo} (Order #${kotResult.orderNo})`),
                    life: 3500,
                });
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
        return this.sendToTable();
    }

    sendToKitchen(): Promise<void> {
        return this.sendToTable();
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
                this.activeOrderId = saved.id;
                this.activeOrderNo = saved.orderNo;
            }

            await this.restaurantOrderService.printBill(this.activeOrderId);
            this.guestBillDialogVisible = true;
            this.loadTables();
            this.messageService.add({
                severity: 'info',
                summary: 'ðŸ§¾ Guest Check Ready',
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

    openEditLineNote(line: PosCartLine): void {
        this.editingLineForNote = line;
        this.tempLineNote = line.kitchenNotes || '';
        this.lineNoteDialogVisible = true;
    }

    saveLineNote(): void {
        if (this.editingLineForNote) {
            this.editingLineForNote.kitchenNotes = this.tempLineNote.trim() || undefined;
        }
        this.lineNoteDialogVisible = false;
        this.editingLineForNote = null;
        this.tempLineNote = '';
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
            this.activeOrderId = saved.id;
            this.activeOrderNo = saved.orderNo;
            if (this.selectedTable) {
                this.selectedTable.status = 1;
                this.selectedTable.statusName = 'Occupied';
                this.selectedTable.currentOrderId = saved.id;
                this.selectedTable.waiterName = this.waiterName || 'Staff';
            }
            this.loadTables();

            this.messageService.add({
                severity: 'success',
                summary: 'ðŸ’¾ Tab Saved',
                detail: `Order #${saved.orderNo} held on ${this.selectedTable ? 'Table ' + this.selectedTable.tableNo : 'Takeaway'}`,
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

    printCurrentModal(elementId: string): void {
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

        const printContent = document.getElementById(elementId);
        if (!printContent) {
            window.print();
            return;
        }
        const win = window.open('', '', 'height=600,width=400');
        if (!win) {
            window.print();
            return;
        }
        win.document.write('<html><head><title>Print</title>');
        win.document.write('<style>');
        win.document.write(`
            body { font-family: 'Courier New', monospace; font-size: 13px; color: #000; padding: 12px; margin: 0; width: 78mm; }
            .ticket-header { text-align: center; border-bottom: 1px dashed #000; padding-bottom: 8px; margin-bottom: 8px; }
            .ticket-title { font-size: 16px; font-weight: bold; }
            .ticket-meta { font-size: 12px; margin: 2px 0; }
            .ticket-table { width: 100%; border-collapse: collapse; margin-bottom: 8px; }
            .ticket-table th, .ticket-table td { text-align: left; padding: 3px 0; }
            .ticket-table th.text-right, .ticket-table td.text-right { text-align: right; }
            .ticket-notes { font-size: 11px; font-style: italic; padding-left: 6px; }
            .ticket-footer { border-top: 1px dashed #000; padding-top: 8px; text-align: center; font-size: 11px; }
            .total-row { display: flex; justify-content: space-between; font-weight: bold; font-size: 14px; margin-top: 4px; }
        `);
        win.document.write('</style></head><body>');
        win.document.write(printContent.innerHTML);
        win.document.write('</body></html>');
        win.document.close();
        win.focus();
        setTimeout(() => {
            win.print();
            win.close();
        }, 250);
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
            return {
                tableId: null,
                tableNo: 'Takeaway',
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
                detail: `${table.tableNo} ready â€” tap it and enter the vehicle number when a car arrives.`,
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
        setTimeout(() => this.barcodeInput?.nativeElement?.focus(), 50);
    }

    onSearchInput(): void {
        if (this.searchTimer) {
            clearTimeout(this.searchTimer);
        }
        const q = (this.searchText || '').trim();
        if (!q) {
            this.suggestions = [];
            return;
        }
        this.searchTimer = setTimeout(() => this.loadSuggestions(q), 250);
    }

    onSearchEnter(): void {
        const code = (this.searchText || '').trim();
        if (!code || this.scanning || this.saving) {
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
        this.focusSearch();
    }

    get currentCustomisedTotal(): number {
        if (!this.selectedProductForModifiers) return 0;
        const base = this.getUnitPrice(this.selectedProductForModifiers) || 0;
        let extra = 0;
        for (const group of this.availableModifierGroups) {
            const selectedIds = this.selectedModifierOptions[group.id] || [];
            for (const optId of selectedIds) {
                const opt = group.options?.find((o) => o.id === optId);
                if (opt) {
                    extra += opt.price || 0;
                }
            }
        }
        return base + extra;
    }

    isOptionSelected(groupId: number, optionId: number): boolean {
        const list = this.selectedModifierOptions[groupId];
        return !!list && list.includes(optionId);
    }

    toggleModifierOption(group: ProductModifierGroupDto, option: ProductModifierOptionDto): void {
        if (!this.selectedModifierOptions[group.id]) {
            this.selectedModifierOptions[group.id] = [];
        }

        const index = this.selectedModifierOptions[group.id].indexOf(option.id);
        if (index > -1) {
            this.selectedModifierOptions[group.id].splice(index, 1);
        } else {
            this.selectedModifierOptions[group.id].push(option.id);
        }
    }

    async addProduct(product: ProductDto): Promise<void> {
        if (this.isRestaurantMode) {
            let isDeal = this.isDealProduct(product);
            if (!isDeal) {
                try {
                    const d = await this.dealService.getByProductId(product.id);
                    if (d && d.items?.length) {
                        isDeal = true;
                        product.isDeal = true;
                        product.dealId = d.id;
                    }
                } catch {
                    // Not a deal
                }
            }
            if (isDeal) {
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
        const index = list.indexOf(option.id);
        if (index > -1) {
            list.splice(index, 1);
        } else {
            if (!group.allowMultiple) {
                item.selectedOptions[group.id] = [option.id];
            } else {
                list.push(option.id);
            }
        }
    }

    get currentDealCustomisedTotal(): number {
        if (!this.selectedDealProduct) return 0;
        const base = this.currentDeal?.dealPrice ?? this.getUnitPrice(this.selectedDealProduct) ?? 0;
        let extra = 0;
        for (const item of this.dealCustomiseItems) {
            for (const group of item.modifierGroups) {
                const sel = item.selectedOptions[group.id] || [];
                for (const optId of sel) {
                    const opt = group.options?.find((o) => o.id === optId);
                    if (opt && opt.price > 0) {
                        extra += opt.price;
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
            for (const group of item.modifierGroups) {
                const sel = item.selectedOptions[group.id] || [];
                for (const optId of sel) {
                    const opt = group.options?.find((o) => o.id === optId);
                    if (opt) {
                        if (opt.price > 0) extraPrice += opt.price;
                        itemOpts.push(`${opt.name}${opt.price > 0 ? ' (+' + opt.price.toFixed(2) + ')' : ''}`);
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
            .join(' â€¢ ');

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
        if (!line.dealSummary && !this.getDealItemsFromLine(line).length) {
            try {
                const dealInfo = await this.dealService.getByProductId(line.productId);
                if (dealInfo && dealInfo.items?.length) {
                    line.dealSummary = dealInfo.items.map((i) => `${i.quantity}x ${i.productName}`).join(' â€¢ ');
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

        for (const group of this.availableModifierGroups) {
            const selectedIds = this.selectedModifierOptions[group.id] || [];
            for (const optId of selectedIds) {
                const opt = group.options?.find((o) => o.id === optId);
                if (opt) {
                    extraPrice += opt.price || 0;
                    modifierNames.push(`${opt.name}${opt.price > 0 ? ' (+' + opt.price.toFixed(2) + ')' : ''}`);
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

    cancelModifiers(): void {
        this.modifierDialogVisible = false;
        this.selectedProductForModifiers = null;
        this.availableModifierGroups = [];
        this.selectedModifierOptions = {};
        this.pendingKitchenNote = '';
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
                return;
            }
            existing.quantity += 1;
            existing.retailPrice = retailPrice;
            existing.wholesalePrice = wholesalePrice;
            existing.unitPrice = unitPrice;
            existing.stockQuantity = product.stockQuantity || 0;
            return;
        }

        if (!this.isRestaurantMode && (product.stockQuantity || 0) < 1) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Stock',
                detail: `"${product.name}" is out of stock. Add stock via Purchases first.`,
            });
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
        this.focusSearch();
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
        this.activeOrderId = null;
        this.activeOrderNo = '';
        this.selectedTable = null;
        this.selectedCarOrder = null;
        this.selectedCarName = '';
        this.selectedCarNumber = '';
        this.selectedCarLocation = '';
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

        this.saving = true;

        try {
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
                    this.activeOrderId = saved.id;
                }

                const branch = this.branchContext.getCurrentBranch();
                const isCar = this.isCarService;
                const settledReceipt = {
                    branchName: branch?.name || 'SmartPOS Restaurant',
                    branchAddress: branch?.invoiceAddress,
                    branchPhone: branch?.invoiceContactPhone,
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

                this.printReceiptSnapshot(settledReceipt);
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
                    summary: 'ðŸ½ï¸ Order Completed',
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
            } else {
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

        const win = window.open('', '', 'height=650,width=420');
        if (!win) {
            window.print();
            return;
        }

        const linesHtml = (receipt.lines || [])
            .map((line: any) => {
                const dealBadge = line.isDeal
                    ? `<span style="font-size:9px; background:#000; color:#fff; padding:1px 3px; font-weight:bold; margin-right:3px; border-radius:2px;">DEAL</span>`
                    : '';
                const dealHtml = line.dealItems?.length
                    ? `<div style="font-size:11px; color:#222; margin-top:2px; padding-left:4px; border-left:2px solid #555;">
                        <div style="font-size:9.5px; font-weight:bold; color:#555;">INCLUDES:</div>
                        ${line.dealItems.map((d: string) => `<div>&bull; ${d}</div>`).join('')}
                       </div>`
                    : '';
                const mods = line.modifiers?.length
                    ? `<div style="font-size:10px; color:#555;">Options: ${line.modifiers.join(', ')}</div>`
                    : '';
                const note = line.notes
                    ? `<div style="font-size:10px; color:#b91c1c; font-style:italic;">Note: ${line.notes}</div>`
                    : '';
                return `
                    <tr style="border-bottom: 1px dotted #ccc;">
                        <td style="padding: 4px 0;">
                            <div style="font-weight: 500;">${dealBadge}${line.productName}</div>
                            ${dealHtml}
                            ${mods}
                            ${note}
                        </td>
                        <td style="text-align: center; padding: 4px 0;">${line.quantity}</td>
                        <td style="text-align: right; padding: 4px 0;">${Number(line.unitPrice).toFixed(2)}</td>
                        <td style="text-align: right; padding: 4px 0; font-weight: 500;">${Number(line.lineTotal).toFixed(2)}</td>
                    </tr>
                `;
            })
            .join('');

        const discountRow = receipt.discountAmount > 0
            ? `<div class="row"><span>Discount${receipt.discountPercent > 0 ? ' (' + receipt.discountPercent + '%)' : ''}:</span><span>-${Number(receipt.discountAmount).toFixed(2)}</span></div>`
            : '';
        const taxRow = receipt.taxAmount > 0
            ? `<div class="row"><span>Tax (${receipt.taxPercent}%):</span><span>+${Number(receipt.taxAmount).toFixed(2)}</span></div>`
            : '';
        const serviceRow = receipt.serviceChargeAmount > 0
            ? `<div class="row"><span>Service Charge (${receipt.serviceChargePercent}%):</span><span>+${Number(receipt.serviceChargeAmount).toFixed(2)}</span></div>`
            : '';

        const html = `
            <!DOCTYPE html>
            <html>
            <head>
                <title>Receipt #${receipt.orderNo}</title>
                <style>
                    body {
                        font-family: 'Courier New', Courier, monospace;
                        font-size: 13px;
                        color: #000;
                        padding: 10px;
                        margin: 0;
                        width: 78mm;
                        box-sizing: border-box;
                    }
                    .header {
                        text-align: center;
                        border-bottom: 1px dashed #000;
                        padding-bottom: 8px;
                        margin-bottom: 8px;
                    }
                    .title {
                        font-size: 16px;
                        font-weight: bold;
                        text-transform: uppercase;
                    }
                    .subtitle {
                        font-size: 12px;
                        font-weight: bold;
                        margin-top: 4px;
                    }
                    .meta {
                        font-size: 11px;
                        margin: 2px 0;
                        color: #222;
                    }
                    table {
                        width: 100%;
                        border-collapse: collapse;
                        margin-bottom: 8px;
                        font-size: 12px;
                    }
                    th {
                        border-bottom: 1px dashed #000;
                        padding: 4px 0;
                        text-align: left;
                    }
                    th.text-center, td.text-center { text-align: center; }
                    th.text-right, td.text-right { text-align: right; }
                    .totals {
                        border-top: 1px dashed #000;
                        padding-top: 6px;
                        font-size: 12px;
                    }
                    .row {
                        display: flex;
                        justify-content: space-between;
                        margin-bottom: 3px;
                    }
                    .grand-total {
                        display: flex;
                        justify-content: space-between;
                        font-size: 15px;
                        font-weight: bold;
                        border-top: 1px dashed #000;
                        border-bottom: 1px dashed #000;
                        padding: 6px 0;
                        margin: 6px 0;
                    }
                    .footer {
                        text-align: center;
                        font-size: 11px;
                        margin-top: 10px;
                        padding-top: 6px;
                        border-top: 1px dashed #000;
                    }
                </style>
            </head>
            <body>
                <div class="header">
                    <div class="title">${receipt.branchName}</div>
                    ${receipt.branchAddress ? `<div class="meta">${receipt.branchAddress}</div>` : ''}
                    ${receipt.branchPhone ? `<div class="meta">Tel: ${receipt.branchPhone}</div>` : ''}
                    <div class="subtitle">*** SALES RECEIPT ***</div>
                    ${receipt.carNumber ? `<div class="meta">Car: <strong>${receipt.carName || ''} (${receipt.carNumber})</strong> &bull; Spot: <strong>${receipt.carLocation || ''}</strong> &bull; Order: #${receipt.orderNo}</div>` : `<div class="meta">Table: <strong>${receipt.tableNo}</strong> &bull; Order: #${receipt.orderNo}</div>`}
                    <div class="meta">Server: ${receipt.serverName} &bull; ${new Date(receipt.date).toLocaleString()}</div>
                    ${this.isRestaurantMode ? `<div class="meta font-bold" style="font-weight:bold; margin-top:3px;">Printed By: ${this.authService.getPrinterIdentity().name} (${this.authService.getPrinterIdentity().role}) &bull; ${new Date().toLocaleTimeString()}</div>` : ''}
                </div>

                <table>
                    <thead>
                        <tr>
                            <th>Dish / Item</th>
                            <th class="text-center">Qty</th>
                            <th class="text-right">Price</th>
                            <th class="text-right">Total</th>
                        </tr>
                    </thead>
                    <tbody>
                        ${linesHtml}
                    </tbody>
                </table>

                <div class="totals">
                    <div class="row">
                        <span>Subtotal:</span>
                        <span>${Number(receipt.subTotal).toFixed(2)}</span>
                    </div>
                    ${discountRow}
                    ${taxRow}
                    ${serviceRow}
                    <div class="grand-total">
                        <span>TOTAL AMOUNT:</span>
                        <span>${Number(receipt.grandTotal).toFixed(2)}</span>
                    </div>
                    <div class="row">
                        <span>Paid (${receipt.paymentType}):</span>
                        <span>${Number(receipt.paidAmount).toFixed(2)}</span>
                    </div>
                    <div class="row">
                        <span>Change:</span>
                        <span>${Number(receipt.changeAmount).toFixed(2)}</span>
                    </div>
                </div>

                <div class="footer">
                    Thank you for dining with us!<br/>
                    Please visit again!
                </div>
            </body>
            </html>
        `;

        win.document.open();
        win.document.write(html);
        win.document.close();
        win.focus();
        setTimeout(() => {
            try {
                win.print();
                win.close();
            } catch (e) {
                console.error('Print error:', e);
            }
        }, 250);
    }

    private loadSuggestions(keyword: string): void {
        this.saleService
            .searchPosProducts(keyword, null)
            .then((items) => {
                if ((this.searchText || '').trim() !== keyword) {
                    return;
                }
                this.suggestions = (items || []).filter((p) => !this.isGroceryProduct(p));
            })
            .catch(() => {
                this.suggestions = [];
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
    }
}
