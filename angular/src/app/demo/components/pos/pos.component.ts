import {
    Component,
    ElementRef,
    OnInit,
    ViewChild,
} from '@angular/core';
import { MessageService } from 'primeng/api';
import { PosCartLine } from 'src/app/demo/api/pos';
import { PaymentType } from 'src/app/demo/api/sale';
import { CustomerDto, CustomerType } from 'src/app/demo/api/customer';
import { ProductDto } from 'src/app/demo/api/product';
import { RestaurantTableDto } from 'src/app/demo/api/restaurant-table';
import {
    OrderTypes,
    RestaurantOrderDto,
    SendToKitchenResultDto,
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

@Component({
    templateUrl: './pos.component.html',
    styleUrls: ['./pos.component.scss'],
    providers: [MessageService],
})
export class PosComponent implements OnInit {
    @ViewChild('barcodeInput') barcodeInput?: ElementRef<HTMLInputElement>;

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
    selectedModifierOptions: Record<number, number> = {};
    pendingKitchenNote = '';
    todayDate = new Date();

    // Quick Add Table form
    newTableNo = '';
    newTableArea = 'Main Hall';
    newTableCapacity = 4;
    creatingTable = false;

    paymentDialogVisible = false;
    paidAmount = 0;

    private searchTimer: ReturnType<typeof setTimeout> | null = null;

    constructor(
        private saleService: SaleService,
        private restaurantTableService: RestaurantTableService,
        private restaurantOrderService: RestaurantOrderService,
        private productModifierService: ProductModifierService,
        private branchContext: BranchContextService,
        private messageService: MessageService
    ) {}

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

    ngOnInit(): void {
        this.applyBranchPricing();
        this.loadTopProducts();
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

    loadTopProducts(): void {
        this.loadingTopProducts = true;
        this.saleService
            .getTopSellingProducts(5)
            .then((products) => {
                this.topProducts = products || [];
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
        }
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
            })
            .finally(() => {
                this.loadingTables = false;
            });
    }

    async selectTable(table: RestaurantTableDto): Promise<void> {
        this.selectedTable = table;
        this.orderType = 'dineIn';
        this.tableDialogVisible = false;

        if (table.status === 1 || table.status === 2) {
            try {
                const order = await this.restaurantOrderService.getByTable(table.id);
                if (order && order.lines?.length) {
                    this.activeOrderId = order.id;
                    this.activeOrderNo = order.orderNo;
                    this.waiterName = order.waiterName || '';
                    this.notes = order.notes || '';
                    if (order.customerId) {
                        this.customerId = order.customerId;
                    }
                    this.cart = order.lines.map((l) => ({
                        id: l.id,
                        productId: l.productId,
                        productName: l.productName,
                        quantity: l.quantity,
                        unitPrice: l.unitPrice,
                        retailPrice: l.unitPrice,
                        wholesalePrice: l.unitPrice,
                        stockQuantity: 999,
                        kitchenNotes: l.kitchenNotes,
                        selectedModifiers: l.selectedModifiers,
                        isSentToKitchen: l.isSentToKitchen,
                    }));
                    this.messageService.add({
                        severity: 'info',
                        summary: 'Order Resumed',
                        detail: `Loaded active order #${order.orderNo} for Table ${table.tableNo}`,
                        life: 3000,
                    });
                    return;
                }
            } catch (err) {
                console.error('Failed to load table order', err);
            }
        }

        // Available table: new running order
        this.activeOrderId = null;
        this.activeOrderNo = '';
        this.messageService.add({
            severity: 'info',
            summary: 'Table Selected',
            detail: `Table ${table.tableNo} (${table.area || 'Main Hall'}) selected`,
            life: 2500,
        });
    }

    clearTableSelection(): void {
        this.selectedTable = null;
        this.activeOrderId = null;
        this.activeOrderNo = '';
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

    async sendToKitchen(): Promise<void> {
        if (!this.cart.length) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Cart Empty',
                detail: 'Add food items before sending to the kitchen.',
            });
            return;
        }

        const branchId = this.branchContext.getBranchId();
        if (!branchId) return;

        this.sendingKOT = true;
        try {
            const payload = {
                id: this.activeOrderId,
                branchId,
                tableId: this.orderType === 'dineIn' ? this.selectedTable?.id : null,
                tableNo: this.orderType === 'dineIn' ? this.selectedTable?.tableNo : 'Takeaway',
                orderType: this.orderType === 'dineIn' ? OrderTypes.DineIn : OrderTypes.Takeaway,
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

            const savedOrder = await this.restaurantOrderService.saveOrder(payload);
            this.activeOrderId = savedOrder.id;
            this.activeOrderNo = savedOrder.orderNo;

            const kotResult = await this.restaurantOrderService.sendToKitchen(savedOrder.id);
            this.kotResult = kotResult;
            this.kotDialogVisible = true;

            this.cart.forEach((l) => (l.isSentToKitchen = true));
            this.loadTables();

            this.messageService.add({
                severity: 'success',
                summary: '🍳 KOT Printed',
                detail: `Kitchen Order Ticket printed for ${kotResult.tableNo} (Order #${kotResult.orderNo})`,
                life: 3500,
            });
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

    async printGuestBill(): Promise<void> {
        if (!this.cart.length) return;
        const branchId = this.branchContext.getBranchId();
        if (!branchId) return;

        this.printingBill = true;
        try {
            if (!this.activeOrderId) {
                const payload = {
                    branchId,
                    tableId: this.orderType === 'dineIn' ? this.selectedTable?.id : null,
                    tableNo: this.orderType === 'dineIn' ? this.selectedTable?.tableNo : 'Takeaway',
                    orderType: this.orderType === 'dineIn' ? OrderTypes.DineIn : OrderTypes.Takeaway,
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
                summary: '🧾 Guest Check Ready',
                detail: `Bill printed for ${this.selectedTable?.tableNo || 'Table'}`,
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
        if (!this.cart.length) return;
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
            const payload = {
                id: this.activeOrderId,
                branchId,
                tableId: this.orderType === 'dineIn' ? this.selectedTable?.id : null,
                tableNo: this.orderType === 'dineIn' ? this.selectedTable?.tableNo : 'Takeaway',
                orderType: this.orderType === 'dineIn' ? OrderTypes.DineIn : OrderTypes.Takeaway,
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
            this.loadTables();

            this.messageService.add({
                severity: 'success',
                summary: '💾 Tab Saved',
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
            });
            this.tables.push(table);
            this.selectTable(table);
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

    async addProduct(product: ProductDto): Promise<void> {
        if (this.isRestaurantMode) {
            try {
                const groups = await this.productModifierService.getByProduct(product.id);
                if (groups && groups.length > 0) {
                    this.selectedProductForModifiers = product;
                    this.availableModifierGroups = groups;
                    this.selectedModifierOptions = {};
                    this.pendingKitchenNote = '';
                    for (const g of groups) {
                        if (g.isRequired && g.options?.length) {
                            this.selectedModifierOptions[g.id] = g.options[0].id;
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

    confirmModifiersAndAddToCart(): void {
        if (!this.selectedProductForModifiers) return;

        for (const group of this.availableModifierGroups) {
            if (group.isRequired && !this.selectedModifierOptions[group.id]) {
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
            const selectedOptId = this.selectedModifierOptions[group.id];
            if (selectedOptId) {
                const opt = group.options?.find((o) => o.id === selectedOptId);
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
            if (existing.quantity + 1 > (product.stockQuantity || 0)) {
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

        if ((product.stockQuantity || 0) < 1) {
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

    bumpQty(line: PosCartLine, delta: number): void {
        const next = Number(((line.quantity || 0) + delta).toFixed(2));
        if (next <= 0) {
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
        if (value <= 0) {
            line.quantity = 1;
            return;
        }

        this.applyQty(line, value);
    }

    private applyQty(line: PosCartLine, qty: number): void {
        if (qty > line.stockQuantity) {
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
        this.focusSearch();
    }

    canSave(): boolean {
        return !!this.customerId && this.cart.length > 0 && !this.saving;
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
                    const payload = {
                        branchId,
                        tableId: this.orderType === 'dineIn' ? this.selectedTable?.id : null,
                        tableNo: this.orderType === 'dineIn' ? this.selectedTable?.tableNo : 'Takeaway',
                        orderType: this.orderType === 'dineIn' ? OrderTypes.DineIn : OrderTypes.Takeaway,
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

                await this.restaurantOrderService.settleOrder({
                    orderId: this.activeOrderId!,
                    customerId: this.customerId as number,
                    paymentType,
                    cashAmount,
                    cardAmount,
                    notes: saleNote || undefined,
                });

                const detail =
                    change > 0
                        ? `Order settled. Return ${change.toFixed(2)} change to customer.`
                        : remaining > 0
                          ? `Order settled. Balance of ${remaining.toFixed(2)} posted on customer credit.`
                          : 'Order settled successfully';

                this.messageService.add({
                    severity: 'success',
                    summary: '🍽️ Order Completed',
                    detail,
                });

                this.paymentDialogVisible = false;
                this.clearCart();
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

    private loadSuggestions(keyword: string): void {
        this.saleService
            .searchPosProducts(keyword)
            .then((items) => {
                if ((this.searchText || '').trim() !== keyword) {
                    return;
                }
                this.suggestions = items;
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
}
