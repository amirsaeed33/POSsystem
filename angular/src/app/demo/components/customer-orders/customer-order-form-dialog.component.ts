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
    CreateCustomerOrderDto,
    CreateCustomerOrderLineDto,
} from 'src/app/demo/api/customer-order';
import { ProductDto } from 'src/app/demo/api/product';
import { CustomerDto, CustomerType } from 'src/app/demo/api/customer';
import { CustomerOrderService } from 'src/app/demo/service/customer-order.service';
import { ProductService } from 'src/app/demo/service/product.service';
import { CustomerService } from 'src/app/demo/service/customer.service';

interface OrderCartLine {
    productId: number;
    productName: string;
    quantity: number;
    unitPrice: number;
    imagePath?: string;
    stockQuantity: number;
}

@Component({
    selector: 'app-customer-order-form-dialog',
    templateUrl: './customer-order-form-dialog.component.html',
    styleUrls: ['./customer-order-form-dialog.component.scss'],
})
export class CustomerOrderFormDialogComponent implements OnChanges {
    @Input() visible = false;
    @Output() visibleChange = new EventEmitter<boolean>();
    @Output() saved = new EventEmitter<void>();

    order: CreateCustomerOrderDto = this.emptyOrder();
    products: ProductDto[] = [];
    filteredProducts: ProductDto[] = [];
    customers: CustomerDto[] = [];
    cart: OrderCartLine[] = [];
    productSearch = '';
    saving = false;
    loading = false;

    constructor(
        private customerOrderService: CustomerOrderService,
        private productService: ProductService,
        private customerService: CustomerService,
        private messageService: MessageService
    ) {}

    get grandTotal(): number {
        return this.cart.reduce(
            (sum, line) => sum + this.lineTotal(line),
            0
        );
    }

    get cartItemCount(): number {
        return this.cart.reduce((sum, line) => sum + (line.quantity || 0), 0);
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

    onCustomerSelected(): void {
        this.cart.forEach((line) => {
            const product = this.products.find((p) => p.id === line.productId);
            if (product) {
                line.unitPrice = this.getUnitPriceForCustomer(product);
            }
        });
    }

    onProductSearch(): void {
        const q = (this.productSearch || '').trim().toLowerCase();
        if (!q) {
            this.filteredProducts = [...this.products];
            return;
        }
        this.filteredProducts = this.products.filter(
            (p) =>
                (p.name || '').toLowerCase().includes(q) ||
                (p.barcode || '').toLowerCase().includes(q) ||
                (p.categoryName || '').toLowerCase().includes(q) ||
                (p.brandName || '').toLowerCase().includes(q)
        );
    }

    getImageUrl(product: ProductDto | OrderCartLine): string {
        return this.productService.getImageUrl(
            (product as ProductDto).imagePath || (product as OrderCartLine).imagePath
        );
    }

    getDisplayPrice(product: ProductDto): number {
        return this.getUnitPriceForCustomer(product);
    }

    cartQty(productId: number): number {
        return this.cart.find((l) => l.productId === productId)?.quantity || 0;
    }

    addProduct(product: ProductDto): void {
        const existing = this.cart.find((l) => l.productId === product.id);
        if (existing) {
            existing.quantity = Number((existing.quantity + 1).toFixed(2));
            existing.unitPrice = this.getUnitPriceForCustomer(product);
            return;
        }

        this.cart.push({
            productId: product.id,
            productName: product.name,
            quantity: 1,
            unitPrice: this.getUnitPriceForCustomer(product),
            imagePath: product.imagePath,
            stockQuantity: product.stockQuantity || 0,
        });
    }

    increaseQty(line: OrderCartLine): void {
        line.quantity = Number(((line.quantity || 0) + 1).toFixed(2));
    }

    decreaseQty(line: OrderCartLine): void {
        const next = Number(((line.quantity || 0) - 1).toFixed(2));
        if (next <= 0) {
            this.removeFromCart(line.productId);
            return;
        }
        line.quantity = next;
    }

    removeFromCart(productId: number): void {
        this.cart = this.cart.filter((l) => l.productId !== productId);
    }

    clearCart(): void {
        this.cart = [];
    }

    lineTotal(line: OrderCartLine): number {
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
        if (!this.order.customerId) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Validation',
                detail: 'Customer is required',
            });
            return;
        }
        if (!this.order.orderDate) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Validation',
                detail: 'Order date is required',
            });
            return;
        }

        const lines: CreateCustomerOrderLineDto[] = this.cart
            .filter((line) => line.productId && (line.quantity || 0) > 0)
            .map((line) => ({
                productId: line.productId,
                quantity: line.quantity,
                unitPrice: line.unitPrice || 0,
            }));

        if (!lines.length) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Validation',
                detail: 'Add at least one product',
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
        this.customerOrderService
            .create({
                customerId: this.order.customerId,
                orderDate: this.order.orderDate,
                notes: this.order.notes?.trim() || undefined,
                lines,
            })
            .then(() => {
                this.messageService.add({
                    severity: 'success',
                    summary: 'Success',
                    detail: 'Customer order created successfully',
                });
                this.saved.emit();
                this.onHide();
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to create customer order',
                });
            })
            .finally(() => {
                this.saving = false;
            });
    }

    private getUnitPriceForCustomer(product: ProductDto): number {
        const customer = this.customers.find((c) => c.id === this.order.customerId);
        const isWholesaler = customer?.customerType === CustomerType.Wholesaler;
        if (isWholesaler) {
            return product.wholesalePrice > 0
                ? product.wholesalePrice
                : product.price || 0;
        }
        return product.price || 0;
    }

    private emptyOrder(): CreateCustomerOrderDto {
        return {
            customerId: null as any,
            orderDate: this.toDateInputValue(),
            notes: '',
            lines: [],
        };
    }

    private resetForm(): void {
        this.order = this.emptyOrder();
        this.cart = [];
        this.productSearch = '';
        this.filteredProducts = [];
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
                this.products = products.items || [];
                this.filteredProducts = [...this.products];
                this.customers = customers.items || [];
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
