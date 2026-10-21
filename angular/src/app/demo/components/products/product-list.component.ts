import { Component, OnInit } from '@angular/core';
import { Table } from 'primeng/table';
import { ConfirmationService, MessageService } from 'primeng/api';
import { ProductDto } from 'src/app/demo/api/product';
import { ProductService } from 'src/app/demo/service/product.service';
import * as XLSX from 'xlsx';
import { PermissionNames } from 'src/app/demo/api/permission-names';
import { PermissionService } from 'src/app/demo/service/permission.service';
import { RowActionItem } from 'src/app/shared/components/row-actions/row-actions.component';

import { ProductModifierGroupDto } from 'src/app/demo/api/product-modifier';
import { ProductModifierService } from 'src/app/demo/service/product-modifier.service';

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

    // Product Modifiers management
    modifierDialogVisible = false;
    modifierTargetProduct: ProductDto | null = null;
    loadingModifiers = false;
    modifierGroups: ProductModifierGroupDto[] = [];
    newGroupName = '';
    newGroupIsRequired = false;
    newGroupAllowMultiple = false;
    creatingGroup = false;
    newOptionName: Record<number, string> = {};
    newOptionPrice: Record<number, number> = {};
    addingOption: Record<number, boolean> = {};

    canCreate = false;
    canEdit = false;
    canDelete = false;

    constructor(
        private productService: ProductService,
        private productModifierService: ProductModifierService,
        private permissionService: PermissionService,
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

    getRowActions(product: ProductDto): RowActionItem[] {
        const actions: RowActionItem[] = [
            {
                label: 'Print barcode',
                icon: 'pi pi-print',
                color: 'blue',
                action: () => this.openBarcodeDialog(product)
            },
            {
                label: 'Modifiers & Add-ons',
                icon: 'pi pi-list',
                color: 'blue',
                action: () => this.openModifierDialog(product)
            }
        ];
        if (this.canEdit) {
            actions.push({
                label: 'Edit',
                icon: 'pi pi-pencil',
                color: 'blue',
                action: () => this.openEditDialog(product)
            });
        }
        if (this.canDelete) {
            actions.push({
                label: 'Delete',
                icon: 'pi pi-trash',
                color: 'red',
                action: () => this.onDelete(product)
            });
        }
        return actions;
    }

    async openModifierDialog(product: ProductDto): Promise<void> {
        this.modifierTargetProduct = product;
        this.modifierDialogVisible = true;
        this.newGroupName = '';
        this.newGroupIsRequired = false;
        this.newGroupAllowMultiple = false;
        await this.loadModifiers(product.id);
    }

    async loadModifiers(productId: number): Promise<void> {
        this.loadingModifiers = true;
        try {
            this.modifierGroups = await this.productModifierService.getByProduct(productId);
        } catch (err: any) {
            this.messageService.add({
                severity: 'error',
                summary: 'Error',
                detail: err?.message || 'Failed to load modifiers',
            });
        } finally {
            this.loadingModifiers = false;
        }
    }

    async createModifierGroup(): Promise<void> {
        if (!this.modifierTargetProduct || !this.newGroupName?.trim()) return;
        this.creatingGroup = true;
        try {
            await this.productModifierService.createGroup({
                productId: this.modifierTargetProduct.id,
                name: this.newGroupName.trim(),
                isRequired: this.newGroupIsRequired,
                minSelection: this.newGroupIsRequired ? 1 : 0,
                maxSelection: this.newGroupAllowMultiple ? 10 : 1,
                options: [],
            });
            this.newGroupName = '';
            this.newGroupIsRequired = false;
            this.newGroupAllowMultiple = false;
            await this.loadModifiers(this.modifierTargetProduct.id);
            this.messageService.add({
                severity: 'success',
                summary: 'Created',
                detail: 'Modifier group added',
            });
        } catch (err: any) {
            this.messageService.add({
                severity: 'error',
                summary: 'Error',
                detail: err?.message || 'Failed to create group',
            });
        } finally {
            this.creatingGroup = false;
        }
    }

    async deleteModifierGroup(groupId: number): Promise<void> {
        if (!this.modifierTargetProduct) return;
        try {
            await this.productModifierService.deleteGroup(groupId);
            await this.loadModifiers(this.modifierTargetProduct.id);
            this.messageService.add({
                severity: 'info',
                summary: 'Deleted',
                detail: 'Modifier group deleted',
            });
        } catch (err: any) {
            this.messageService.add({
                severity: 'error',
                summary: 'Error',
                detail: err?.message || 'Failed to delete group',
            });
        }
    }

    async addModifierOption(groupId: number): Promise<void> {
        const name = (this.newOptionName[groupId] || '').trim();
        const price = Number(this.newOptionPrice[groupId]) || 0;
        if (!name || !this.modifierTargetProduct) return;

        this.addingOption[groupId] = true;
        try {
            await this.productModifierService.addOption(groupId, {
                name,
                price,
            });
            this.newOptionName[groupId] = '';
            this.newOptionPrice[groupId] = 0;
            await this.loadModifiers(this.modifierTargetProduct.id);
            this.messageService.add({
                severity: 'success',
                summary: 'Added',
                detail: 'Option added',
            });
        } catch (err: any) {
            this.messageService.add({
                severity: 'error',
                summary: 'Error',
                detail: err?.message || 'Failed to add option',
            });
        } finally {
            this.addingOption[groupId] = false;
        }
    }

    async deleteModifierOption(optionId: number): Promise<void> {
        if (!this.modifierTargetProduct) return;
        try {
            await this.productModifierService.deleteOption(optionId);
            await this.loadModifiers(this.modifierTargetProduct.id);
            this.messageService.add({
                severity: 'info',
                summary: 'Deleted',
                detail: 'Option removed',
            });
        } catch (err: any) {
            this.messageService.add({
                severity: 'error',
                summary: 'Error',
                detail: err?.message || 'Failed to delete option',
            });
        }
    }

    ngOnInit(): void {
        this.canCreate = this.permissionService.isGranted(PermissionNames.ProductsCreate);
        this.canEdit = this.permissionService.isGranted(PermissionNames.ProductsEdit);
        this.canDelete = this.permissionService.isGranted(PermissionNames.ProductsDelete);
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
        if (img && img.parentElement) {
            const parent = img.parentElement;
            img.remove();
            const fallback = document.createElement('div');
            fallback.className = 'product-list-thumb product-initials';
            fallback.innerText = this.getProductInitials(img.alt);
            parent.appendChild(fallback);
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
                'Purchase Price (PKR)': 320,
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
                'Purchase Price (PKR)': 40,
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
                    CostPrice: Number(r['Purchase Price (PKR)'] || r['Cost Price (PKR)'] || r['CostPrice'] || r['costPrice'] || 0),
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
