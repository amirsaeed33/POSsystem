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

@Component({
    selector: 'app-customer-order-form-dialog',
    templateUrl: './customer-order-form-dialog.component.html',
})
export class CustomerOrderFormDialogComponent implements OnChanges {
    @Input() visible = false;
    @Output() visibleChange = new EventEmitter<boolean>();
    @Output() saved = new EventEmitter<void>();

    order: CreateCustomerOrderDto = this.emptyOrder();
    products: ProductDto[] = [];
    customers: CustomerDto[] = [];
    saving = false;
    loading = false;

    constructor(
        private customerOrderService: CustomerOrderService,
        private productService: ProductService,
        private customerService: CustomerService,
        private messageService: MessageService
    ) {}

    get grandTotal(): number {
        return (this.order.lines || []).reduce(
            (sum, line) => sum + this.lineTotal(line),
            0
        );
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
        this.order.lines.push(this.emptyLine());
    }

    removeLine(index: number): void {
        if (this.order.lines.length > 1) {
            this.order.lines.splice(index, 1);
        }
    }

    onCustomerSelected(): void {
        (this.order.lines || []).forEach((line) => {
            if (line.productId) {
                this.onProductSelected(line);
            }
        });
    }

    onProductSelected(line: CreateCustomerOrderLineDto): void {
        const product = this.products.find((p) => p.id === line.productId);
        if (product) {
            line.unitPrice = this.getUnitPriceForCustomer(product);
        }
    }

    lineTotal(line: CreateCustomerOrderLineDto): number {
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

        const lines = (this.order.lines || []).filter(
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
        this.customerOrderService
            .create({
                customerId: this.order.customerId,
                orderDate: this.order.orderDate,
                notes: this.order.notes?.trim() || undefined,
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

    private emptyLine(): CreateCustomerOrderLineDto {
        return {
            productId: null as any,
            quantity: 1,
            unitPrice: 0,
        };
    }

    private emptyOrder(): CreateCustomerOrderDto {
        return {
            customerId: null as any,
            orderDate: this.toDateInputValue(),
            notes: '',
            lines: [this.emptyLine()],
        };
    }

    private resetForm(): void {
        this.order = this.emptyOrder();
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
