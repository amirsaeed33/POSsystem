import { Component, OnInit } from '@angular/core';
import { Table } from 'primeng/table';
import { ConfirmationService, MessageService } from 'primeng/api';
import { BranchDto } from 'src/app/demo/api/branch';
import { BranchService } from 'src/app/demo/service/branch.service';

@Component({
    selector: 'app-branch-list',
    templateUrl: './branch-list.component.html',
    providers: [MessageService, ConfirmationService],
})
export class BranchListComponent implements OnInit {
    branches: BranchDto[] = [];
    loading = false;
    totalRecords = 0;
    keyword = '';

    dialogVisible = false;
    editingBranchId: number | null = null;

    constructor(
        private branchService: BranchService,
        private messageService: MessageService,
        private confirmationService: ConfirmationService
    ) {}

    ngOnInit(): void {
        this.loadBranches();
    }

    loadBranches(): void {
        this.loading = true;
        this.branchService
            .getAll({
                keyword: this.keyword?.trim() || undefined,
                skipCount: 0,
                maxResultCount: 1000,
            })
            .then((result) => {
                this.branches = result.items;
                this.totalRecords = result.totalCount;
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to load branches',
                });
            })
            .finally(() => {
                this.loading = false;
            });
    }

    onSearch(): void {
        this.loadBranches();
    }

    onGlobalFilter(table: Table, event: Event): void {
        table.filterGlobal((event.target as HTMLInputElement).value, 'contains');
    }

    openCreateDialog(): void {
        this.editingBranchId = null;
        this.dialogVisible = true;
    }

    openEditDialog(branch: BranchDto): void {
        this.editingBranchId = branch.id;
        this.dialogVisible = true;
    }

    onDialogSaved(): void {
        this.loadBranches();
    }

    onDelete(branch: BranchDto): void {
        if (branch.isDefault) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Not allowed',
                detail: 'The default branch cannot be deleted.',
            });
            return;
        }

        this.confirmationService.confirm({
            message: `Are you sure you want to delete branch "${branch.name}"?`,
            header: 'Delete Confirmation',
            icon: 'pi pi-exclamation-triangle',
            acceptButtonStyleClass: 'p-button-danger',
            accept: () => {
                this.branchService
                    .delete(branch.id)
                    .then(() => {
                        this.messageService.add({
                            severity: 'success',
                            summary: 'Success',
                            detail: 'Branch deleted successfully',
                        });
                        this.loadBranches();
                    })
                    .catch((error) => {
                        this.messageService.add({
                            severity: 'error',
                            summary: 'Error',
                            detail: error?.message || 'Failed to delete branch',
                        });
                    });
            },
        });
    }
}
