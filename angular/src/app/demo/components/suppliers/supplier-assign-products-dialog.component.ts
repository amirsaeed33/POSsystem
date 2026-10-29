import {
    Component,
    EventEmitter,
    Input,
    OnChanges,
    Output,
    SimpleChanges,
} from '@angular/core';
import { MessageService } from 'primeng/api';
import { ProductDto } from 'src/app/demo/api/product';
import { ProductService } from 'src/app/demo/service/product.service';
import { SupplierService } from 'src/app/demo/service/supplier.service';
import { BranchContextService } from 'src/app/demo/service/branch-context.service';

@Component({
    selector: 'app-supplier-assign-products-dialog',
    templateUrl: './supplier-assign-products-dialog.component.html',
})
export class SupplierAssignProductsDialogComponent implements OnChanges {
    @Input() visible = false;
    @Input() supplierId: number | null = null;
    @Input() supplierName = '';
    @Output() visibleChange = new EventEmitter<boolean>();
    @Output() saved = new EventEmitter<void>();

    products: ProductDto[] = [];
    selectedProductIds: number[] = [];
    productFilter = '';
    loading = false;
    saving = false;

    get isRestaurant(): boolean {
        return this.branchContext.isRestaurant();
    }

    get dialogTitle(): string {
        return this.supplierName
            ? `Assign Products — ${this.supplierName}`
            : 'Assign Products';
    }

    get filteredProducts(): ProductDto[] {
        const keyword = (this.productFilter || '').trim().toLowerCase();
        if (!keyword) {
            return this.products;
        }
        return this.products.filter((p) => {
            const name = (p.name || '').toLowerCase();
            const barcode = (p.barcode || '').toLowerCase();
            const category = (p.categoryName || '').toLowerCase();
            return (
                name.includes(keyword) ||
                barcode.includes(keyword) ||
                category.includes(keyword)
            );
        });
    }

    constructor(
        private supplierService: SupplierService,
        private productService: ProductService,
        private branchContext: BranchContextService,
        private messageService: MessageService
    ) {}

    ngOnChanges(changes: SimpleChanges): void {
        if (changes['visible'] && this.visible && this.supplierId) {
            this.loadData();
        }
    }

    onVisibleChange(visible: boolean): void {
        this.visible = visible;
        this.visibleChange.emit(visible);
    }

    onHide(): void {
        this.onVisibleChange(false);
    }

    isSelected(productId: number): boolean {
        return this.selectedProductIds.includes(productId);
    }

    toggleProduct(productId: number, checked: boolean): void {
        if (checked) {
            if (!this.selectedProductIds.includes(productId)) {
                this.selectedProductIds = [...this.selectedProductIds, productId];
            }
            return;
        }
        this.selectedProductIds = this.selectedProductIds.filter(
            (id) => id !== productId
        );
    }

    selectAllVisible(): void {
        const ids = new Set(this.selectedProductIds);
        for (const product of this.filteredProducts) {
            ids.add(product.id);
        }
        this.selectedProductIds = Array.from(ids);
    }

    clearSelection(): void {
        this.selectedProductIds = [];
    }

    save(): void {
        if (!this.supplierId) {
            return;
        }

        this.saving = true;
        this.supplierService
            .setAssignedProducts(this.supplierId, this.selectedProductIds || [])
            .then(() => {
                this.messageService.add({
                    severity: 'success',
                    summary: 'Success',
                    detail: 'Products assigned successfully',
                });
                this.saved.emit();
                this.onHide();
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to assign products',
                });
            })
            .finally(() => {
                this.saving = false;
            });
    }

    private isPermanentGroceryProduct(product: ProductDto): boolean {
        return (
            (product.categoryName || '').trim().toLowerCase() ===
            'permanent grocery'
        );
    }

    private loadData(): void {
        if (!this.supplierId) {
            return;
        }

        this.loading = true;
        this.selectedProductIds = [];
        this.productFilter = '';

        Promise.all([
            this.productService.getAll({ skipCount: 0, maxResultCount: 1000 }),
            this.supplierService.getAssignedProductIds(this.supplierId),
        ])
            .then(([products, assignedIds]) => {
                const items = products.items || [];
                // Restaurant: assign Permanent Grocery products for purchasing.
                // Non-restaurant: exclude that category.
                this.products = this.isRestaurant
                    ? items.filter((p) => this.isPermanentGroceryProduct(p))
                    : items.filter((p) => !this.isPermanentGroceryProduct(p));

                const availableIds = new Set(this.products.map((p) => p.id));
                this.selectedProductIds = (assignedIds || []).filter((id) =>
                    availableIds.has(id)
                );
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to load products',
                });
            })
            .finally(() => {
                this.loading = false;
            });
    }
}
