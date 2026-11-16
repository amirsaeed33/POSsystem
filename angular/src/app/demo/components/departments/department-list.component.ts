import { Component, Input, OnInit } from '@angular/core';
import { Table } from 'primeng/table';
import { ConfirmationService, MessageService } from 'primeng/api';
import { DepartmentDto } from 'src/app/demo/api/department';
import { DepartmentService } from 'src/app/demo/service/department.service';
import { RowActionItem } from 'src/app/shared/components/row-actions/row-actions.component';

@Component({
    selector: 'app-department-list',
    templateUrl: './department-list.component.html',
    providers: [MessageService, ConfirmationService],
})
export class DepartmentListComponent implements OnInit {
    /** When true, omit outer card wrapper (used inside Product Settings tabs). */
    @Input() embedded = false;

    departments: DepartmentDto[] = [];
    loading = false;
    totalRecords = 0;
    keyword = '';

    dialogVisible = false;
    editingDepartmentId: number | null = null;

    getRowActions(department: DepartmentDto): RowActionItem[] {
        return [
            {
                label: 'Edit',
                icon: 'pi pi-pencil',
                color: 'blue',
                action: () => this.openEditDialog(department),
            },
            {
                label: 'Delete',
                icon: 'pi pi-trash',
                color: 'red',
                action: () => this.onDelete(department),
            },
        ];
    }

    constructor(
        private departmentService: DepartmentService,
        private messageService: MessageService,
        private confirmationService: ConfirmationService
    ) {}

    ngOnInit(): void {
        this.loadDepartments();
    }

    loadDepartments(): void {
        this.loading = true;
        this.departmentService
            .getAll({
                keyword: this.keyword?.trim() || undefined,
                skipCount: 0,
                maxResultCount: 1000,
            })
            .then((result) => {
                this.departments = result.items;
                this.totalRecords = result.totalCount;
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to load departments',
                });
            })
            .finally(() => {
                this.loading = false;
            });
    }

    onSearch(): void {
        this.loadDepartments();
    }

    onGlobalFilter(table: Table, event: Event): void {
        table.filterGlobal(
            (event.target as HTMLInputElement).value,
            'contains'
        );
    }

    openCreateDialog(): void {
        this.editingDepartmentId = null;
        this.dialogVisible = true;
    }

    openEditDialog(department: DepartmentDto): void {
        this.editingDepartmentId = department.id;
        this.dialogVisible = true;
    }

    onDialogSaved(): void {
        this.loadDepartments();
    }

    onDelete(department: DepartmentDto): void {
        this.confirmationService.confirm({
            message: `Are you sure you want to delete department "${department.name}"? Categories assigned to this department will remain.`,
            header: 'Delete Confirmation',
            icon: 'pi pi-exclamation-triangle',
            acceptButtonStyleClass: 'p-button-danger',
            accept: () => {
                this.loading = true;
                this.departmentService
                    .delete(department.id)
                    .then(() => {
                        this.messageService.add({
                            severity: 'success',
                            summary: 'Success',
                            detail: 'Department deleted successfully',
                        });
                        this.loadDepartments();
                    })
                    .catch((error) => {
                        this.messageService.add({
                            severity: 'error',
                            summary: 'Error',
                            detail: error?.message || 'Failed to delete department',
                        });
                    })
                    .finally(() => {
                        this.loading = false;
                    });
            },
        });
    }
}

