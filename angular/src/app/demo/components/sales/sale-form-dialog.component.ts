import {
    Component,
    EventEmitter,
    Input,
    OnChanges,
    Output,
    SimpleChanges,
} from '@angular/core';
import { MessageService } from 'primeng/api';
import {
    CreateSaleDto,
    CreateSaleLineDto,
    PaymentType,
} from 'src/app/demo/api/sale';
import { ProductDto } from 'src/app/demo/api/product';
import { CustomerDto, CustomerType } from 'src/app/demo/api/customer';
import { SaleService } from 'src/app/demo/service/sale.service';
import { ProductService } from 'src/app/demo/service/product.service';
import { CustomerService } from 'src/app/demo/service/customer.service';

@Component({
    selector: 'app-sale-form-dialog',
    templateUrl: './sale-form-dialog.component.html',
})
export class SaleFormDialogComponent implements OnChanges {
    @Input() visible = false;
    @Output() visibleChange = new EventEmitter<boolean>();
    @Output() saved = new EventEmitter<void>();

    sale: CreateSaleDto = this.emptySale();
    products: ProductDto[] = [];
    customers: CustomerDto[] = [];
    saving = false;
    loading = false;

    paymentTypes = [
        { value: PaymentType.Cash, label: 'Cash' },
        { value: PaymentType.Card, label: 'Card' },
        { value: PaymentType.Credit, label: 'Credit' },
        { value: PaymentType.Mixed, label: 'Mixed' },
    ];

    constructor(
        private saleService: SaleService,
        private productService: ProductService,
        private customerService: CustomerService,
        private messageService: MessageService
    ) {}

    get subTotal(): number {
        return (this.sale.lines || []).reduce(
            (sum, line) => sum + this.lineTotal(line),
            0
        );
    }

    get computedDiscount(): number {
        let discount = this.sale.discountAmount || 0;
        if ((this.sale.discountPercent || 0) > 0 && discount <= 0) {
            discount =
                Math.round(
                    ((this.subTotal * this.sale.discountPercent) / 100) * 100
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
            Math.round(
                ((taxable * (this.sale.taxPercent || 0)) / 100) * 100
            ) / 100
        );
    }

    get grandTotal(): number {
        return (
            Math.round(
                (this.subTotal - this.computedDiscount + this.taxAmount) * 100
            ) / 100
        );
    }

    get isMixedPayment(): boolean {
        return this.sale.paymentType === PaymentType.Mixed;
    }

    ngOnChanges(changes: SimpleChanges): void {
        if (changes['visible'] && this.visible) {
            this.resetForm();
            this.loadLookups();
        }
    }

    onVisibleChange(visible: boolean): void {
        this.visible = visible;
        this.visibleChange.emit(visible);
    }

    onHide(): void {
        this.onVisibleChange(false);
    }

    addLine(): void {
        this.sale.lines.push(this.emptyLine());
    }

    removeLine(index: number): void {
        if (this.sale.lines.length > 1) {
            this.sale.lines.splice(index, 1);
        }
    }

    onCustomerSelected(): void {
        (this.sale.lines || []).forEach((line) => {
            if (line.productId) {
                this.onProductSelected(line);
            }
        });
    }

    onProductSelected(line: CreateSaleLineDto): void {
        const product = this.products.find((p) => p.id === line.productId);
        if (product) {
            line.unitPrice = this.getUnitPriceForCustomer(product);
        }
    }

    onPaymentTypeChange(): void {
        if (this.sale.paymentType !== PaymentType.Mixed) {
            this.sale.cashAmount = 0;
            this.sale.cardAmount = 0;
        }
    }

    lineTotal(line: CreateSaleLineDto): number {
        return (line.quantity || 0) * (line.unitPrice || 0);
    }

    customerOptionLabel(customer: CustomerDto): string {
        const typeLabel =
            customer.customerType === CustomerType.Wholesaler
                ? 'Wholesaler'
                : 'Direct';
        return `${customer.name} — ${typeLabel}`;
    }

    save(): void {
        if (!this.sale.customerId) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Validation',
                detail: 'Customer is required',
            });
            return;
        }
        if (!this.sale.saleDate) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Validation',
                detail: 'Sale date is required',
            });
            return;
        }

        const lines = (this.sale.lines || []).filter(
            (line) => line.productId && (line.quantity || 0) > 0
        );
        if (!lines.length) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Validation',
                detail: 'Add at least one product line',
            });
            return;
        }

        for (const line of lines) {
            if ((line.quantity || 0) <= 0) {
                this.messageService.add({
                    severity: 'warn',
                    summary: 'Validation',
                    detail: 'Quantity must be greater than zero',
                });
                return;
            }
            if (line.unitPrice == null || line.unitPrice < 0) {
                this.messageService.add({
                    severity: 'warn',
                    summary: 'Validation',
                    detail: 'Unit price must be zero or greater',
                });
                return;
            }
        }

        this.saving = true;
        this.saleService
            .create({
                customerId: this.sale.customerId,
                saleDate: this.sale.saleDate,
                notes: this.sale.notes?.trim() || undefined,
                discountAmount: this.sale.discountAmount || 0,
                discountPercent: this.sale.discountPercent || 0,
                taxPercent: this.sale.taxPercent || 0,
                paymentType: this.sale.paymentType,
                cashAmount: this.sale.cashAmount || 0,
                cardAmount: this.sale.cardAmount || 0,
                lines: lines.map((line) => ({
                    productId: line.productId,
                    quantity: line.quantity,
                    unitPrice: line.unitPrice || 0,
                })),
            })
            .then(() => {
                this.messageService.add({
                    severity: 'success',
                    summary: 'Success',
                    detail: 'Sale created successfully',
                });
                this.saved.emit();
                this.onHide();
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to create sale',
                });
            })
            .finally(() => {
                this.saving = false;
            });
    }

    private getUnitPriceForCustomer(product: ProductDto): number {
        const customer = this.customers.find((c) => c.id === this.sale.customerId);
        const isWholesaler = customer?.customerType === CustomerType.Wholesaler;
        if (isWholesaler) {
            return product.wholesalePrice > 0
                ? product.wholesalePrice
                : product.price || 0;
        }
        return product.price || 0;
    }

    private emptyLine(): CreateSaleLineDto {
        return {
            productId: null as any,
            quantity: 1,
            unitPrice: 0,
        };
    }

    private emptySale(): CreateSaleDto {
        return {
            customerId: null as any,
            saleDate: this.toDateInputValue(),
            notes: '',
            discountAmount: 0,
            discountPercent: 0,
            taxPercent: 0,
            paymentType: PaymentType.Credit,
            cashAmount: 0,
            cardAmount: 0,
            lines: [this.emptyLine()],
        };
    }

    private resetForm(): void {
        this.sale = this.emptySale();
        this.saving = false;
        this.loading = false;
    }

    private toDateInputValue(date: Date = new Date()): string {
        const year = date.getFullYear();
        const month = `${date.getMonth() + 1}`.padStart(2, '0');
        const day = `${date.getDate()}`.padStart(2, '0');
        return `${year}-${month}-${day}`;
    }

    private loadLookups(): void {
        this.loading = true;
        Promise.all([
            this.productService.getAll({ skipCount: 0, maxResultCount: 1000 }),
            this.customerService.getAll({ skipCount: 0, maxResultCount: 1000 }),
        ])
            .then(([products, customers]) => {
                this.products = products.items;
                this.customers = customers.items;
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to load lookup data',
                });
            })
            .finally(() => {
                this.loading = false;
            });
    }
}
