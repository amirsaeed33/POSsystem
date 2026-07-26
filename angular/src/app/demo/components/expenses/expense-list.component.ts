import { Component, OnInit } from '@angular/core';
import { Table } from 'primeng/table';
import { ConfirmationService, MessageService } from 'primeng/api';
import { ExpenseDto } from 'src/app/demo/api/expense';
import { ExpenseService } from 'src/app/demo/service/expense.service';

@Component({
    templateUrl: './expense-list.component.html',
    providers: [MessageService, ConfirmationService],
})
export class ExpenseListComponent implements OnInit {
    expenses: ExpenseDto[] = [];
    loading = false;
    totalRecords = 0;
    keyword = '';

    dialogVisible = false;
    editingExpenseId: number | null = null;
    printDialogVisible = false;
    printingExpenseId: number | null = null;

    constructor(
        private expenseService: ExpenseService,
        private messageService: MessageService,
        private confirmationService: ConfirmationService
    ) {}

    ngOnInit(): void {
        this.loadExpenses();
    }

    loadExpenses(): void {
        this.loading = true;
        this.expenseService
            .getAll({
                keyword: this.keyword?.trim() || undefined,
                skipCount: 0,
                maxResultCount: 1000,
            })
            .then((result) => {
                this.expenses = result.items;
                this.totalRecords = result.totalCount;
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to load expenses',
                });
            })
            .finally(() => {
                this.loading = false;
            });
    }

    onSearch(): void {
        this.loadExpenses();
    }

    onGlobalFilter(table: Table, event: Event): void {
        table.filterGlobal(
            (event.target as HTMLInputElement).value,
            'contains'
        );
    }

    openCreateDialog(): void {
        this.editingExpenseId = null;
        this.dialogVisible = true;
    }

    openEditDialog(expense: ExpenseDto): void {
        this.editingExpenseId = expense.id;
        this.dialogVisible = true;
    }

    openPrintDialog(expense: ExpenseDto): void {
        this.printingExpenseId = expense.id;
        this.printDialogVisible = true;
    }

    onDialogSaved(): void {
        this.loadExpenses();
    }

    onDelete(expense: ExpenseDto): void {
        const label = expense.referenceNo || `#${expense.id}`;
        this.confirmationService.confirm({
            message: `Are you sure you want to delete expense "${label}"?`,
            header: 'Delete Confirmation',
            icon: 'pi pi-exclamation-triangle',
            acceptButtonStyleClass: 'p-button-danger',
            accept: () => {
                this.expenseService
                    .delete(expense.id)
                    .then(() => {
                        this.messageService.add({
                            severity: 'success',
                            summary: 'Success',
                            detail: 'Expense deleted successfully',
                        });
                        this.loadExpenses();
                    })
                    .catch((error) => {
                        this.messageService.add({
                            severity: 'error',
                            summary: 'Error',
                            detail:
                                error?.message || 'Failed to delete expense',
                        });
                    });
            },
        });
    }
}
