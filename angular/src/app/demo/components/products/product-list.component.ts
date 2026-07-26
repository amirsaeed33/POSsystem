import { Component, OnInit } from '@angular/core';
import { Table } from 'primeng/table';
import { ConfirmationService, MessageService } from 'primeng/api';
import { ProductDto } from 'src/app/demo/api/product';
import { ProductService } from 'src/app/demo/service/product.service';

@Component({
    templateUrl: './product-list.component.html',
    providers: [MessageService, ConfirmationService],
})
export class ProductListComponent implements OnInit {
    products: ProductDto[] = [];
    loading = false;
    totalRecords = 0;
    keyword = '';

    dialogVisible = false;
    editingProductId: number | null = null;

    imageDialogVisible = false;
    selectedImageUrl = '';
    selectedImageName = '';

    constructor(
        private productService: ProductService,
        private messageService: MessageService,
        private confirmationService: ConfirmationService
    ) {}

    ngOnInit(): void {
        this.loadProducts();
    }

    loadProducts(): void {
        this.loading = true;
        this.productService
            .getAll({
                keyword: this.keyword?.trim() || undefined,
                skipCount: 0,
                maxResultCount: 1000,
            })
            .then((result) => {
                this.products = result.items;
                this.totalRecords = result.totalCount;
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

    onSearch(): void {
        this.loadProducts();
    }

    onGlobalFilter(table: Table, event: Event): void {
        table.filterGlobal((event.target as HTMLInputElement).value, 'contains');
    }

    getImageUrl(product: ProductDto): string {
        return this.productService.getImageUrl(product.imagePath);
    }

    viewImage(product: ProductDto): void {
        if (!product.imagePath) {
            return;
        }
        this.selectedImageUrl = this.getImageUrl(product);
        this.selectedImageName = product.name;
        this.imageDialogVisible = true;
    }

    openCreateDialog(): void {
        this.editingProductId = null;
        this.dialogVisible = true;
    }

    openEditDialog(product: ProductDto): void {
        this.editingProductId = product.id;
        this.dialogVisible = true;
    }

    onDialogSaved(): void {
        this.loadProducts();
    }

    onDelete(product: ProductDto): void {
        this.confirmationService.confirm({
            message: `Are you sure you want to delete product "${product.name}"?`,
            header: 'Delete Confirmation',
            icon: 'pi pi-exclamation-triangle',
            acceptButtonStyleClass: 'p-button-danger',
            accept: () => {
                this.productService
                    .delete(product.id)
                    .then(() => {
                        this.messageService.add({
                            severity: 'success',
                            summary: 'Success',
                            detail: 'Product deleted successfully',
                        });
                        this.loadProducts();
                    })
                    .catch((error) => {
                        this.messageService.add({
                            severity: 'error',
                            summary: 'Error',
                            detail: error?.message || 'Failed to delete product',
                        });
                    });
            },
        });
    }
}
