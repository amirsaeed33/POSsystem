import { Component, Input, OnInit } from '@angular/core';
import { Table } from 'primeng/table';
import { ConfirmationService, MessageService } from 'primeng/api';
import { CategoryDto } from 'src/app/demo/api/category';
import { CategoryService } from 'src/app/demo/service/category.service';

@Component({
    selector: 'app-category-list',
    templateUrl: './category-list.component.html',
    providers: [MessageService, ConfirmationService],
})
export class CategoryListComponent implements OnInit {
    /** When true, omit outer card wrapper (used inside Product Settings tabs). */
    @Input() embedded = false;

    categories: CategoryDto[] = [];
    loading = false;
    totalRecords = 0;
    keyword = '';

    dialogVisible = false;
    editingCategoryId: number | null = null;

    constructor(
        private categoryService: CategoryService,
        private messageService: MessageService,
        private confirmationService: ConfirmationService
    ) {}

    ngOnInit(): void {
        this.loadCategories();
    }

    loadCategories(): void {
        this.loading = true;
        this.categoryService
            .getAll({
                keyword: this.keyword?.trim() || undefined,
                skipCount: 0,
                maxResultCount: 1000,
            })
            .then((result) => {
                this.categories = result.items;
                this.totalRecords = result.totalCount;
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to load categories',
                });
            })
            .finally(() => {
                this.loading = false;
            });
    }

    onSearch(): void {
        this.loadCategories();
    }

    onGlobalFilter(table: Table, event: Event): void {
        table.filterGlobal((event.target as HTMLInputElement).value, 'contains');
    }

    openCreateDialog(): void {
        this.editingCategoryId = null;
        this.dialogVisible = true;
    }

    openEditDialog(category: CategoryDto): void {
        this.editingCategoryId = category.id;
        this.dialogVisible = true;
    }

    onDialogSaved(): void {
        this.loadCategories();
    }

    onDelete(category: CategoryDto): void {
        this.confirmationService.confirm({
            message: `Are you sure you want to delete category "${category.name}"?`,
            header: 'Delete Confirmation',
            icon: 'pi pi-exclamation-triangle',
            acceptButtonStyleClass: 'p-button-danger',
            accept: () => {
                this.categoryService
                    .delete(category.id)
                    .then(() => {
                        this.messageService.add({
                            severity: 'success',
                            summary: 'Success',
                            detail: 'Category deleted successfully',
                        });
                        this.loadCategories();
                    })
                    .catch((error) => {
                        this.messageService.add({
                            severity: 'error',
                            summary: 'Error',
                            detail: error?.message || 'Failed to delete category',
                        });
                    });
            },
        });
    }
}
