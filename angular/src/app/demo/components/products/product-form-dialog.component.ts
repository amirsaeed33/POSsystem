import {
    Component,
    EventEmitter,
    Input,
    OnChanges,
    Output,
    SimpleChanges,
} from '@angular/core';
import { MessageService } from 'primeng/api';
import { CreateProductDto, ProductDto } from 'src/app/demo/api/product';
import { CategoryDto } from 'src/app/demo/api/category';
import { BrandDto } from 'src/app/demo/api/brand';
import { UnitDto } from 'src/app/demo/api/unit';
import { ProductService } from 'src/app/demo/service/product.service';
import { CategoryService } from 'src/app/demo/service/category.service';
import { BrandService } from 'src/app/demo/service/brand.service';
import { UnitService } from 'src/app/demo/service/unit.service';

@Component({
    selector: 'app-product-form-dialog',
    templateUrl: './product-form-dialog.component.html',
})
export class ProductFormDialogComponent implements OnChanges {
    @Input() visible = false;
    @Input() productId: number | null = null;
    @Output() visibleChange = new EventEmitter<boolean>();
    @Output() saved = new EventEmitter<void>();

    product: ProductDto = this.emptyProduct();
    categories: CategoryDto[] = [];
    brands: BrandDto[] = [];
    units: UnitDto[] = [];
    imagePreview = '';
    saving = false;
    loading = false;

    constructor(
        private productService: ProductService,
        private categoryService: CategoryService,
        private brandService: BrandService,
        private unitService: UnitService,
        private messageService: MessageService
    ) {}

    get dialogTitle(): string {
        return this.productId ? 'Edit Product' : 'Create Product';
    }

    get profitPerUnit(): number {
        return (this.product.price || 0) - (this.product.costPrice || 0);
    }

    get profitMarginPercent(): number | null {
        if (!this.product.price) {
            return null;
        }
        return (this.profitPerUnit / this.product.price) * 100;
    }

    ngOnChanges(changes: SimpleChanges): void {
        if (changes['visible'] && this.visible) {
            this.resetForm();
            this.loadLookups().then(() => {
                if (this.productId) {
                    this.loadProduct(this.productId);
                }
            });
        }
    }

    onVisibleChange(visible: boolean): void {
        this.visible = visible;
        this.visibleChange.emit(visible);
    }

    onHide(): void {
        this.onVisibleChange(false);
    }

    onFileSelected(event: Event): void {
        const input = event.target as HTMLInputElement;
        if (!input.files?.length) {
            return;
        }

        const file = input.files[0];
        const reader = new FileReader();
        reader.onload = () => {
            const result = reader.result as string;
            if (!result?.startsWith('data:image')) {
                return;
            }
            this.imagePreview = result;
            this.product.imageBase64 = result;
        };
        reader.readAsDataURL(file);
    }

    save(): void {
        const name = (this.product.name || '').trim();
        if (!name) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Validation',
                detail: 'Name is required',
            });
            return;
        }
        if (!this.product.categoryId || !this.product.brandId || !this.product.unitId) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Validation',
                detail: 'Category, Brand and Unit are required',
            });
            return;
        }

        const price = this.product.price || 0;
        const wholesalePrice = this.product.wholesalePrice || 0;
        const costPrice = this.product.costPrice || 0;
        if (price < costPrice) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Validation',
                detail: 'Selling price cannot be lower than the cost price.',
            });
            return;
        }
        if (price < wholesalePrice) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Validation',
                detail: 'Selling price cannot be lower than the wholesale price.',
            });
            return;
        }

        this.saving = true;
        const imageBase64 =
            this.product.imageBase64?.startsWith('data:image')
                ? this.product.imageBase64
                : undefined;

        const request = this.productId
            ? this.productService.update({
                  ...this.product,
                  id: this.productId,
                  name,
                  description: this.product.description?.trim() || undefined,
                  barcode: this.product.barcode?.trim() || undefined,
                  imageBase64,
              })
            : this.productService.create({
                  name,
                  description: this.product.description?.trim() || undefined,
                  barcode: this.product.barcode?.trim() || undefined,
                  price: this.product.price || 0,
                  wholesalePrice: this.product.wholesalePrice || 0,
                  costPrice: this.product.costPrice || 0,
                  alertQuantityLimit: this.product.alertQuantityLimit || 0,
                  categoryId: this.product.categoryId,
                  brandId: this.product.brandId,
                  unitId: this.product.unitId,
                  imageBase64,
              } as CreateProductDto);

        request
            .then(() => {
                this.messageService.add({
                    severity: 'success',
                    summary: 'Success',
                    detail: this.productId
                        ? 'Product updated successfully'
                        : 'Product created successfully',
                });
                this.saved.emit();
                this.onHide();
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to save product',
                });
            })
            .finally(() => {
                this.saving = false;
            });
    }

    private emptyProduct(): ProductDto {
        return {
            id: 0,
            name: '',
            description: '',
            barcode: '',
            price: 0,
            wholesalePrice: 0,
            costPrice: 0,
            profitPerUnit: 0,
            stockProfit: 0,
            stockQuantity: 0,
            alertQuantityLimit: 10,
            categoryId: null as any,
            brandId: null as any,
            unitId: null as any,
            imagePath: undefined,
            imageBase64: undefined,
        };
    }

    private resetForm(): void {
        this.product = this.emptyProduct();
        this.imagePreview = '';
        this.saving = false;
        this.loading = false;
    }

    private async loadLookups(): Promise<void> {
        this.loading = true;
        try {
            const [categories, brands, units] = await Promise.all([
                this.categoryService.getAll({ skipCount: 0, maxResultCount: 1000 }),
                this.brandService.getAll({ skipCount: 0, maxResultCount: 1000 }),
                this.unitService.getAll({ skipCount: 0, maxResultCount: 1000 }),
            ]);
            this.categories = categories.items;
            this.brands = brands.items;
            this.units = units.items;
        } catch (error: any) {
            this.messageService.add({
                severity: 'error',
                summary: 'Error',
                detail: error?.message || 'Failed to load lookup data',
            });
        } finally {
            if (!this.productId) {
                this.loading = false;
            }
        }
    }

    private loadProduct(id: number): void {
        this.loading = true;
        this.productService
            .get(id)
            .then((product) => {
                this.product = { ...product, imageBase64: undefined };
                this.imagePreview = this.productService.getImageUrl(product.imagePath);
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to load product',
                });
                this.onHide();
            })
            .finally(() => {
                this.loading = false;
            });
    }
}
