import { Component, OnInit } from '@angular/core';
import { Table } from 'primeng/table';
import { ConfirmationService, MessageService } from 'primeng/api';
import { BrandDto } from 'src/app/demo/api/brand';
import { BrandService } from 'src/app/demo/service/brand.service';

@Component({
    templateUrl: './brand-list.component.html',
    providers: [MessageService, ConfirmationService],
})
export class BrandListComponent implements OnInit {
    brands: BrandDto[] = [];
    loading = false;
    totalRecords = 0;
    keyword = '';

    dialogVisible = false;
    editingBrandId: number | null = null;

    constructor(
        private brandService: BrandService,
        private messageService: MessageService,
        private confirmationService: ConfirmationService
    ) {}

    ngOnInit(): void {
        this.loadBrands();
    }

    loadBrands(): void {
        this.loading = true;
        this.brandService
            .getAll({
                keyword: this.keyword?.trim() || undefined,
                skipCount: 0,
                maxResultCount: 1000,
            })
            .then((result) => {
                this.brands = result.items;
                this.totalRecords = result.totalCount;
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to load brands',
                });
            })
            .finally(() => {
                this.loading = false;
            });
    }

    onSearch(): void {
        this.loadBrands();
    }

    onGlobalFilter(table: Table, event: Event): void {
        table.filterGlobal((event.target as HTMLInputElement).value, 'contains');
    }

    openCreateDialog(): void {
        this.editingBrandId = null;
        this.dialogVisible = true;
    }

    openEditDialog(brand: BrandDto): void {
        this.editingBrandId = brand.id;
        this.dialogVisible = true;
    }

    onDialogSaved(): void {
        this.loadBrands();
    }

    onDelete(brand: BrandDto): void {
        this.confirmationService.confirm({
            message: `Are you sure you want to delete brand "${brand.name}"?`,
            header: 'Delete Confirmation',
            icon: 'pi pi-exclamation-triangle',
            acceptButtonStyleClass: 'p-button-danger',
            accept: () => {
                this.brandService
                    .delete(brand.id)
                    .then(() => {
                        this.messageService.add({
                            severity: 'success',
                            summary: 'Success',
                            detail: 'Brand deleted successfully',
                        });
                        this.loadBrands();
                    })
                    .catch((error) => {
                        this.messageService.add({
                            severity: 'error',
                            summary: 'Error',
                            detail: error?.message || 'Failed to delete brand',
                        });
                    });
            },
        });
    }
}
