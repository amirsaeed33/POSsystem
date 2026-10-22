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
        this.branchSub = this.branchContext.currentBranch$.subscribe((branch) => {
            if (branch) {
                this.applyBranchPricing();
            }
        });
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

    isGroceryProduct(product?: ProductDto | null): boolean {
        if (!product) return false;
        return (
            (product.barcode || '').startsWith('GROCERY-') ||
            (product.name || '').trim().toLowerCase() === 'grocery'
        );
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

    openTableDialog(forSend: boolean = false): void {
        this.pendingSendToTable = forSend;
        this.loadTables();
        this.tableDialogVisible = true;
    }

    onTableDialogHide(): void {
        this.pendingSendToTable = false;
    }

    async selectTable(table: RestaurantTableDto): Promise<void> {
        this.selectedTable = table;
        this.orderType = 'dineIn';
        this.tableDialogVisible = false;

        const isDirectSend = this.pendingSendToTable;
        this.pendingSendToTable = false;

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
                    const existingLines = order.lines.map((l) => ({
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

                    if (isDirectSend) {
                        const newCartItems = this.cart.filter((c) => !c.id);
                        this.cart = [...existingLines, ...newCartItems];
                    } else {
                        this.cart = existingLines;
                        this.messageService.add({
                            severity: 'info',
                            summary: 'Order Resumed',
                            detail: `Loaded active order #${order.orderNo} for Table ${table.tableNo}`,
                            life: 3000,
                        });
                        return;
                    }
                }
            } catch (err) {
                console.error('Failed to load table order', err);
            }
        } else {
            // Available table: new running order
            this.activeOrderId = null;
            this.activeOrderNo = '';
            if (!isDirectSend) {
                this.messageService.add({
                    severity: 'info',
                    summary: 'Table Selected',
                    detail: `Table ${table.tableNo} (${table.area || 'Main Hall'}) selected`,
                    life: 2500,
                });
            }
        }

        if (isDirectSend) {
            await this.sendToTable();
        }
    }

    clearTableSelection(): void {
        this.selectedTable = null;
        this.activeOrderId = null;
        this.activeOrderNo = '';
        this.pendingSendToTable = false;
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

    async sendToTable(): Promise<void> {
        if (this.orderType === 'takeaway') {
            this.messageService.add({
                severity: 'info',
                summary: 'Takeaway Order',
                detail: 'Send to Table is not available for Takeaway orders.',
            });
            return;
        }

        if (!this.cart.length) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Cart Empty',
                detail: 'Add food items before sending to the table.',
            });
            return;
        }

        if (this.orderType === 'dineIn' && !this.selectedTable) {
            this.openTableDialog(true);
            this.messageService.add({
                severity: 'info',
                summary: 'Select Table',
                detail: 'Please select a table to send this order to.',
                life: 3000,
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
            await this.loadTables();

            this.messageService.add({
                severity: 'success',
                summary: '🍽️ Sent to Table',
                detail: `Order sent to Table ${kotResult.tableNo} (Order #${kotResult.orderNo})`,
                life: 3500,
            });
        } catch (err: any) {
            this.messageService.add({
                severity: 'error',
                summary: 'Error',
                detail: err?.message || 'Failed to send order to table',
            });
        } finally {
            this.sendingKOT = false;
        }
    }

    sendToKitchen(): Promise<void> {
        return this.sendToTable();
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

    isOptionSelected(groupId: number, optionId: number): boolean {
        const list = this.selectedModifierOptions[groupId];
        return !!list && list.includes(optionId);
    }

    toggleModifierOption(group: ProductModifierGroupDto, option: ProductModifierOptionDto): void {
        if (!this.selectedModifierOptions[group.id]) {
            this.selectedModifierOptions[group.id] = [];
        }

        if (group.allowMultiple) {
            const index = this.selectedModifierOptions[group.id].indexOf(option.id);
            if (index > -1) {
                this.selectedModifierOptions[group.id].splice(index, 1);
            } else {
                this.selectedModifierOptions[group.id].push(option.id);
            }
        } else {
            if (this.selectedModifierOptions[group.id].includes(option.id)) {
                if (!group.isRequired) {
                    this.selectedModifierOptions[group.id] = [];
                }
            } else {
                this.selectedModifierOptions[group.id] = [option.id];
            }
        }
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

    get hasUnsentItems(): boolean {
        return this.cart.some((line) => !line.isSentToKitchen);
    }

    get settleButtonTooltip(): string {
        if (this.isRestaurantMode && this.orderType === 'dineIn') {
            if (!this.selectedTable) {
                return 'Please select a table and send order first.';
            }
            if (this.hasUnsentItems) {
                return 'Send dishes to table first before settling & closing.';
            }
        }
        return '';
    }

    canSave(): boolean {
        if (!this.customerId || this.cart.length === 0 || this.saving) {
            return false;
        }

        // Restaurant Dine-In mode: cannot settle/close table until items are sent to the table
        if (this.isRestaurantMode && this.orderType === 'dineIn') {
            if (!this.selectedTable) {
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

                const branch = this.branchContext.getCurrentBranch();
                const settledReceipt = {
                    branchName: branch?.name || 'SmartPOS Restaurant',
                    branchAddress: branch?.invoiceAddress,
                    branchPhone: branch?.invoiceContactPhone,
                    orderNo: this.activeOrderNo || `ORD-${Date.now().toString().slice(-6)}`,
                    tableNo: this.orderType === 'dineIn' ? (this.selectedTable?.tableNo || 'Dine-In') : 'Takeaway',
                    serverName: this.waiterName || 'Staff',
                    date: new Date(),
                    lines: this.cart.map((l) => ({
                        productName: l.productName,
                        quantity: l.quantity,
                        unitPrice: l.unitPrice,
                        lineTotal: l.quantity * l.unitPrice,
                        modifiers: l.selectedModifiers ? this.parseModifiers(l.selectedModifiers) : [],
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

    printReceiptSnapshot(receipt: any): void {
        const win = window.open('', '', 'height=650,width=420');
        if (!win) {
            window.print();
            return;
        }

        const linesHtml = (receipt.lines || [])
            .map((line: any) => {
                const mods = line.modifiers?.length ? `<div style="font-size:10px; color:#555;">${line.modifiers.join(', ')}</div>` : '';
                const note = line.notes ? `<div style="font-size:10px; color:#666; font-style:italic;">Note: ${line.notes}</div>` : '';
                return `
                    <tr style="border-bottom: 1px dotted #ccc;">
                        <td style="padding: 4px 0;">
                            <div style="font-weight: 500;">${line.productName}</div>
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
                    <div class="meta">Table: <strong>${receipt.tableNo}</strong> &bull; Order: #${receipt.orderNo}</div>
                    <div class="meta">Server: ${receipt.serverName} &bull; ${new Date(receipt.date).toLocaleString()}</div>
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
            .searchPosProducts(keyword)
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
