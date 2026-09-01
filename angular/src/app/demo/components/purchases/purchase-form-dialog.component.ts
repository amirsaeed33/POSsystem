import {
    Component,
    EventEmitter,
    HostListener,
    Input,
    OnChanges,
    Output,
    SimpleChanges,
} from '@angular/core';
import { MessageService } from 'primeng/api';
import { CreatePurchaseDto, CreatePurchaseLineDto } from 'src/app/demo/api/purchase';
import { ProductDto } from 'src/app/demo/api/product';
import { SupplierDto } from 'src/app/demo/api/supplier';
import { PurchaseService } from 'src/app/demo/service/purchase.service';
import { ProductService } from 'src/app/demo/service/product.service';
import { SupplierService } from 'src/app/demo/service/supplier.service';

@Component({
    selector: 'app-purchase-form-dialog',
    templateUrl: './purchase-form-dialog.component.html',
})
export class PurchaseFormDialogComponent implements OnChanges {
    @Input() visible = false;
    @Output() visibleChange = new EventEmitter<boolean>();
    @Output() saved = new EventEmitter<void>();

    purchase: CreatePurchaseDto = this.emptyPurchase();
    products: ProductDto[] = [];
    suppliers: SupplierDto[] = [];
    saving = false;
    loading = false;

    printDialogVisible = false;
    printingPurchaseId: number | null = null;
    printAutoPrint = false;

    constructor(
        private purchaseService: PurchaseService,
        private productService: ProductService,
        private supplierService: SupplierService,
        private messageService: MessageService
    ) {}

    get grandTotal(): number {
        return (this.purchase.lines || []).reduce(
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

    @HostListener('document:keydown', ['$event'])
    handleKeyboard(event: KeyboardEvent): void {
        if (!this.visible || this.saving || this.loading) {
            return;
        }
        if (!(event.ctrlKey || event.metaKey)) {
            return;
        }

        const key = (event.key || '').toLowerCase();
        if (key === 's') {
            event.preventDefault();
            this.save();
        } else if (key === 'p') {
            event.preventDefault();
            this.saveAndPrint();
        }
    }

    addLine(): void {
        this.purchase.lines.push(this.emptyLine());
    }

    removeLine(index: number): void {
        if (this.purchase.lines.length > 1) {
            this.purchase.lines.splice(index, 1);
        }
    }

    onProductSelected(line: CreatePurchaseLineDto): void {
        if (!line.productId) {
            return;
        }
        const product = this.products.find((p) => p.id === line.productId);
        if (product) {
            line.unitCost = (product.costPrice ?? product.price) || 0;
        }
        this.mergeDuplicateProductLine(line);
    }

    lineTotal(line: CreatePurchaseLineDto): number {
        return (line.quantity || 0) * (line.unitCost || 0);
    }

    save(): void {
        this.saveInternal(false);
    }

    saveAndPrint(): void {
        this.saveInternal(true);
    }

    private saveInternal(printAfter: boolean): void {
        if (!this.purchase.supplierId) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Validation',
                detail: 'Supplier is required',
            });
            return;
        }
        if (!this.purchase.purchaseDate) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Validation',
                detail: 'Purchase date is required',
            });
            return;
        }

        const lines = this.mergeLinesByProduct(
            (this.purchase.lines || []).filter(
                (line) => line.productId && (line.quantity || 0) > 0
            )
        );
        this.purchase.lines = lines.length ? [...lines] : [this.emptyLine()];
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
            if (line.unitCost == null || line.unitCost < 0) {
                this.messageService.add({
                    severity: 'warn',
                    summary: 'Validation',
                    detail: 'Unit cost must be zero or greater',
                });
                return;
            }
        }

        this.saving = true;
        this.purchaseService
            .create({
                supplierId: this.purchase.supplierId,
                purchaseDate: this.purchase.purchaseDate,
                notes: this.purchase.notes?.trim() || undefined,
                lines: lines.map((line) => ({
                    productId: line.productId,
                    quantity: line.quantity,
                    unitCost: line.unitCost || 0,
                })),
            })
            .then((created) => {
                this.messageService.add({
                    severity: 'success',
                    summary: 'Success',
                    detail: 'Purchase created successfully',
                });
                this.saved.emit();
                this.onHide();

                // Match angular-old: close create dialog, then open invoice print dialog.
                if (printAfter && created?.id) {
                    setTimeout(() => {
                        this.openInvoicePrint(created.id, true);
                    }, 0);
                }
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to create purchase',
                });
            })
            .finally(() => {
                this.saving = false;
            });
    }

    private openInvoicePrint(purchaseId: number, autoPrint: boolean): void {
        this.printingPurchaseId = purchaseId;
        this.printAutoPrint = autoPrint;
        this.printDialogVisible = true;
    }

    private mergeDuplicateProductLine(line: CreatePurchaseLineDto): void {
        const lines = this.purchase.lines || [];
        const existing = lines.find(
            (l) => l !== line && l.productId === line.productId
        );
        if (!existing) {
            return;
        }

        existing.quantity = Number(
            ((existing.quantity || 0) + (line.quantity || 0)).toFixed(2)
        );
        const product = this.products.find((p) => p.id === existing.productId);
        if (product) {
            existing.unitCost = (product.costPrice ?? product.price) || 0;
        }

        const index = lines.indexOf(line);
        if (index < 0) {
            return;
        }
        if (lines.length > 1) {
            lines.splice(index, 1);
        } else {
            Object.assign(line, this.emptyLine());
        }

        this.messageService.add({
            severity: 'info',
            summary: 'Merged',
            detail: 'Same product combined into one line',
        });
    }

    private mergeLinesByProduct(
        lines: CreatePurchaseLineDto[]
    ): CreatePurchaseLineDto[] {
        const merged = new Map<number, CreatePurchaseLineDto>();
        for (const line of lines) {
            const existing = merged.get(line.productId);
            if (existing) {
                existing.quantity = Number(
                    ((existing.quantity || 0) + (line.quantity || 0)).toFixed(2)
                );
            } else {
                merged.set(line.productId, {
                    productId: line.productId,
                    quantity: line.quantity || 0,
                    unitCost: line.unitCost || 0,
                });
            }
        }
        return Array.from(merged.values());
    }

    private emptyLine(): CreatePurchaseLineDto {
        return {
            productId: null as any,
            quantity: 1,
            unitCost: 0,
        };
    }

    private emptyPurchase(): CreatePurchaseDto {
        return {
            supplierId: null as any,
            purchaseDate: this.toDateInputValue(),
            notes: '',
            lines: [this.emptyLine()],
        };
    }

    private resetForm(): void {
        this.purchase = this.emptyPurchase();
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
            this.supplierService.getLookup(),
        ])
            .then(([products, suppliers]) => {
                this.products = products.items;
                this.suppliers = suppliers;
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
