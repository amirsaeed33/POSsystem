import {
    Component,
    ElementRef,
    OnInit,
    ViewChild,
} from '@angular/core';
import { MessageService } from 'primeng/api';
import { PosCartLine } from 'src/app/demo/api/pos';
import { CreateSaleDto, PaymentType } from 'src/app/demo/api/sale';
import { CustomerDto, CustomerType } from 'src/app/demo/api/customer';
import { ProductDto } from 'src/app/demo/api/product';
import { SaleService } from 'src/app/demo/service/sale.service';
import { CustomerService } from 'src/app/demo/service/customer.service';

@Component({
    templateUrl: './pos.component.html',
    styleUrls: ['./pos.component.scss'],
    providers: [MessageService],
})
export class PosComponent implements OnInit {
    @ViewChild('barcodeInput') barcodeInput?: ElementRef<HTMLInputElement>;

    saving = false;
    scanning = false;
    barcode = '';
    customers: CustomerDto[] = [];
    cart: PosCartLine[] = [];

    customerId: number | null = null;
    notes = '';
    discountAmount = 0;
    discountPercent = 0;
    taxPercent = 0;
    paymentType = PaymentType.Cash;
    cashAmount = 0;
    cardAmount = 0;

    paymentTypes = [
        { value: PaymentType.Cash, label: 'Cash' },
        { value: PaymentType.Card, label: 'Card' },
        { value: PaymentType.Credit, label: 'Credit' },
        { value: PaymentType.Mixed, label: 'Mixed' },
    ];

    constructor(
        private saleService: SaleService,
        private customerService: CustomerService,
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

    get creditAmount(): number {
        if (this.paymentType === PaymentType.Credit) {
            return this.grandTotal;
        }
        if (this.paymentType === PaymentType.Mixed) {
            return Math.max(
                0,
                Math.round(
                    (this.grandTotal -
                        (this.cashAmount || 0) -
                        (this.cardAmount || 0)) *
                        100
                ) / 100
            );
        }
        return 0;
    }

    get isMixedPayment(): boolean {
        return this.paymentType === PaymentType.Mixed;
    }

    get showCreditAmount(): boolean {
        return (
            this.paymentType === PaymentType.Credit ||
            this.paymentType === PaymentType.Mixed
        );
    }

    ngOnInit(): void {
        this.customerService
            .getAll({ skipCount: 0, maxResultCount: 1000 })
            .then((result) => {
                this.customers = result.items || [];
                const walkIn = this.customers.find((c) =>
                    (c.name || '').toLowerCase().includes('walk')
                );
                this.customerId = walkIn?.id || this.customers[0]?.id || null;
                this.focusBarcode();
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to load customers',
                });
            });
    }

    focusBarcode(): void {
        setTimeout(() => this.barcodeInput?.nativeElement?.focus(), 50);
    }

    onPaymentTypeChange(): void {
        if (
            this.paymentType === PaymentType.Cash ||
            this.paymentType === PaymentType.Card ||
            this.paymentType === PaymentType.Credit
        ) {
            this.cashAmount = 0;
            this.cardAmount = 0;
        } else if (this.paymentType === PaymentType.Mixed) {
            this.cashAmount = this.grandTotal;
            this.cardAmount = 0;
        }
        this.focusBarcode();
    }

    setPaymentType(type: number): void {
        this.paymentType = type;
        this.onPaymentTypeChange();
    }

    bumpQty(line: PosCartLine, delta: number): void {
        this.updateQty(line, (line.quantity || 0) + delta);
    }

    onBarcodeEnter(): void {
        const code = (this.barcode || '').trim();
        if (!code || this.scanning || this.saving) {
            return;
        }

        this.scanning = true;
        this.saleService
            .getProductByBarcode(code)
            .then((product) => {
                this.addProduct(product);
                this.barcode = '';
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'warn',
                    summary: 'Not found',
                    detail: error?.message || 'Product not found',
                });
                this.barcode = '';
            })
            .finally(() => {
                this.scanning = false;
                this.focusBarcode();
            });
    }

    addProduct(product: ProductDto): void {
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
            return;
        }

        if ((product.stockQuantity || 0) < 1) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Stock',
                detail: 'Out of stock',
            });
            return;
        }

        this.cart.push({
            productId: product.id,
            productName: product.name,
            quantity: 1,
            unitPrice: this.getUnitPrice(product),
            stockQuantity: product.stockQuantity || 0,
        });
    }

    getUnitPrice(product: ProductDto): number {
        const customer = this.customers.find((c) => c.id === this.customerId);
        const isWholesaler = customer?.customerType === CustomerType.Wholesaler;
        if (isWholesaler) {
            return product.wholesalePrice > 0
                ? product.wholesalePrice
                : product.price || 0;
        }
        return product.price || 0;
    }

    onCustomerSelected(): void {
        this.focusBarcode();
    }

    updateQty(line: PosCartLine, qty: number): void {
        if (qty <= 0) {
            this.removeLine(line);
            return;
        }
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
        this.focusBarcode();
    }

    clearCart(): void {
        this.cart = [];
        this.discountAmount = 0;
        this.discountPercent = 0;
        this.taxPercent = 0;
        this.notes = '';
        this.paymentType = PaymentType.Cash;
        this.cashAmount = 0;
        this.cardAmount = 0;
        this.focusBarcode();
    }

    canSave(): boolean {
        return !!this.customerId && this.cart.length > 0 && !this.saving;
    }

    buildSaleDto(): CreateSaleDto {
        return {
            customerId: this.customerId as number,
            saleDate: this.toDateInputValue(),
            notes: this.notes?.trim() || undefined,
            discountAmount: this.discountAmount || 0,
            discountPercent: this.discountPercent || 0,
            taxPercent: this.taxPercent || 0,
            paymentType: this.paymentType,
            cashAmount:
                this.paymentType === PaymentType.Mixed
                    ? this.cashAmount || 0
                    : 0,
            cardAmount:
                this.paymentType === PaymentType.Mixed
                    ? this.cardAmount || 0
                    : 0,
            lines: this.cart.map((line) => ({
                productId: line.productId,
                quantity: line.quantity,
                unitPrice: line.unitPrice,
            })),
        };
    }

    completeSale(): void {
        if (!this.canSave()) {
            return;
        }

        if (this.paymentType === PaymentType.Mixed) {
            const paid = (this.cashAmount || 0) + (this.cardAmount || 0);
            if (paid > this.grandTotal + 0.001) {
                this.messageService.add({
                    severity: 'warn',
                    summary: 'Validation',
                    detail: 'Cash + card cannot exceed total',
                });
                return;
            }
        }

        this.saving = true;
        this.saleService
            .create(this.buildSaleDto())
            .then(() => {
                this.messageService.add({
                    severity: 'success',
                    summary: 'Success',
                    detail: 'Sale completed successfully',
                });
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
                this.focusBarcode();
            });
    }

    private toDateInputValue(date: Date = new Date()): string {
        const year = date.getFullYear();
        const month = `${date.getMonth() + 1}`.padStart(2, '0');
        const day = `${date.getDate()}`.padStart(2, '0');
        return `${year}-${month}-${day}`;
    }
}
