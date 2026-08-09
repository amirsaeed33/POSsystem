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
import { BranchContextService } from 'src/app/demo/service/branch-context.service';
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

    paymentDialogVisible = false;
    paidAmount = 0;

    private searchTimer: ReturnType<typeof setTimeout> | null = null;

    constructor(
        private saleService: SaleService,
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

    get grandTotal(): number {
        return (
            Math.round(
                (this.subTotal - this.computedDiscount + this.taxAmount) * 100
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

    ngOnInit(): void {
        this.applyBranchPricing();
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

    private applyBranchPricing(): void {
        const branch = this.branchContext.getCurrentBranch();
        this.taxPercent = branch?.taxPercent ?? 0;
        this.discountPercent = branch?.discountPercent ?? 0;
        this.discountAmount = branch?.discountAmount ?? 0;
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

    addProduct(product: ProductDto): void {
        const retailPrice = product.price || 0;
        const wholesalePrice = product.wholesalePrice || 0;
        const unitPrice = this.resolveUnitPrice(retailPrice, wholesalePrice);

        const existing = this.cart.find((x) => x.productId === product.id);
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

    confirmPaymentAndSave(): void {
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

        this.saving = true;
        this.saleService
            .create({
                customerId: this.customerId as number,
                saleDate: this.toDateInputValue(),
                notes: this.notes?.trim() || undefined,
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
            })
            .then(() => {
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
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to complete sale',
                });
            })
            .finally(() => {
                this.saving = false;
                this.focusSearch();
            });
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
