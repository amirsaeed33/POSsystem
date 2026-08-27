import { Component, OnInit } from '@angular/core';
import { Table } from 'primeng/table';
import { ConfirmationService, MessageService } from 'primeng/api';
import { ProductDto } from 'src/app/demo/api/product';
import { ProductService } from 'src/app/demo/service/product.service';
import * as XLSX from 'xlsx';

@Component({
    templateUrl: './product-list.component.html',
    styleUrls: ['./product-list.component.scss'],
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

    barcodeDialogVisible = false;
    barcodeTargetProduct: ProductDto | null = null;
    selectedProducts: ProductDto[] = [];

    constructor(
        private productService: ProductService,
        private messageService: MessageService,
        private confirmationService: ConfirmationService
    ) {}

    openBarcodeDialog(product?: ProductDto): void {
        if (product) {
            this.barcodeTargetProduct = product;
        } else {
            this.barcodeTargetProduct = null;
        }
        this.barcodeDialogVisible = true;
    }

    openBulkBarcodeDialog(): void {
        this.barcodeTargetProduct = null;
        if (!this.selectedProducts?.length) {
            // If no individual rows selected with checkbox, pass all loaded products
            this.selectedProducts = [...this.products];
        }
        this.barcodeDialogVisible = true;
    }

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

    getProductInitials(name?: string | null): string {
        return this.productService.getProductInitials(name);
    }

    onProductImageError(event: Event): void {
        const img = event.target as HTMLImageElement | null;
        if (img) {
            img.style.display = 'none';
            const fallback = img.nextElementSibling as HTMLElement | null;
            if (fallback?.classList.contains('product-initials')) {
                fallback.hidden = false;
            }
        }
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

    downloadExcelTemplate(): void {
        const sampleData = [
            {
                'Product Name': 'Sample Shampoo 200ml',
                'Barcode': '8901234567890',
                'Price (PKR)': 450,
                'Wholesale Price (PKR)': 380,
                'Cost Price (PKR)': 320,
                'Stock Quantity': 50,
                'Alert Limit': 10,
                'Category Name': 'Cosmetics',
                'Brand Name': 'Sunsilk',
                'Unit Name': 'Pcs',
                'Location': 'Shelf A-1',
                'Description': '200ml anti-dandruff shampoo'
            },
            {
                'Product Name': 'Crispy Biscuit 100g',
                'Barcode': '8909876543210',
                'Price (PKR)': 60,
                'Wholesale Price (PKR)': 48,
                'Cost Price (PKR)': 40,
                'Stock Quantity': 100,
                'Alert Limit': 20,
                'Category Name': 'Bakery',
                'Brand Name': 'LU',
                'Unit Name': 'Pack',
                'Location': 'Rack B-3',
                'Description': '100g fresh biscuit pack'
            }
        ];

        const worksheet = XLSX.utils.json_to_sheet(sampleData);
        const workbook = XLSX.utils.book_new();
        XLSX.utils.book_append_sheet(workbook, worksheet, 'Products_Template');
        XLSX.writeFile(workbook, 'Product_Import_Template.xlsx');
    }

    async onExcelFileSelected(event: any): Promise<void> {
        const file = event.target.files?.[0];
        if (!file) return;

        const reader = new FileReader();
        reader.onload = async (e: any) => {
            try {
                const data = new Uint8Array(e.target.result);
                const workbook = XLSX.read(data, { type: 'array' });
                const firstSheetName = workbook.SheetNames[0];
                const worksheet = workbook.Sheets[firstSheetName];
                const rawRows: any[] = XLSX.utils.sheet_to_json(worksheet);

                if (!rawRows.length) {
                    this.messageService.add({
                        severity: 'warn',
                        summary: 'Empty File',
                        detail: 'No product rows found in the selected Excel file.'
                    });
                    return;
                }

                const parsedRows = rawRows.map((r: any) => ({
                    Name: r['Product Name'] || r['Name'] || r['name'] || '',
                    Barcode: String(r['Barcode'] || r['barcode'] || ''),
                    Price: Number(r['Price (PKR)'] || r['Price'] || r['price'] || 0),
                    WholesalePrice: Number(r['Wholesale Price (PKR)'] || r['WholesalePrice'] || r['wholesalePrice'] || 0),
                    CostPrice: Number(r['Cost Price (PKR)'] || r['CostPrice'] || r['costPrice'] || 0),
                    StockQuantity: Number(r['Stock Quantity'] || r['StockQuantity'] || r['stockQuantity'] || 0),
                    AlertQuantityLimit: Number(r['Alert Limit'] || r['AlertQuantityLimit'] || r['alertQuantityLimit'] || 10),
                    CategoryName: r['Category Name'] || r['Category'] || r['category'] || '',
                    BrandName: r['Brand Name'] || r['Brand'] || r['brand'] || '',
                    UnitName: r['Unit Name'] || r['Unit'] || r['unit'] || '',
                    Location: r['Location'] || r['location'] || '',
                    Description: r['Description'] || r['description'] || ''
                }));

                this.loading = true;
                const importRes = await this.productService.importProducts(parsedRows);
                this.loadProducts();

                if (importRes.errorCount > 0) {
                    this.messageService.add({
                        severity: 'warn',
                        summary: `Imported ${importRes.successCount} Products`,
                        detail: `${importRes.errorCount} rows failed. ${importRes.errorMessages[0] || ''}`,
                        life: 8000
                    });
                } else {
                    this.messageService.add({
                        severity: 'success',
                        summary: 'Success',
                        detail: `Successfully imported ${importRes.successCount} products!`
                    });
                }
            } catch (err: any) {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Import Error',
                    detail: err?.message || 'Failed to parse Excel file.'
                });
            } finally {
                this.loading = false;
                event.target.value = '';
            }
        };
        reader.readAsArrayBuffer(file);
    }
}
