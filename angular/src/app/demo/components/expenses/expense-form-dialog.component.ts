import {
    Component,
    EventEmitter,
    Input,
    OnChanges,
    Output,
    SimpleChanges,
} from '@angular/core';
import { MessageService } from 'primeng/api';
import { CreateExpenseDto } from 'src/app/demo/api/expense';
import { BusinessAccountDto } from 'src/app/demo/api/business-account';
import { ExpenseService } from 'src/app/demo/service/expense.service';
import { BranchContextService } from 'src/app/demo/service/branch-context.service';
import { BusinessAccountService } from 'src/app/demo/service/business-account.service';

@Component({
    selector: 'app-expense-form-dialog',
    templateUrl: './expense-form-dialog.component.html',
})
export class ExpenseFormDialogComponent implements OnChanges {
    @Input() visible = false;
    @Input() expenseId: number | null = null;
    @Output() visibleChange = new EventEmitter<boolean>();
    @Output() saved = new EventEmitter<void>();

    expense: CreateExpenseDto = this.emptyExpense();
    paymentAccounts: BusinessAccountDto[] = [];
    saving = false;
    loading = false;

    private readonly excludedAccountTypes = [
        'Purchase',
        'Sale',
        'Expense',
        'Customer',
        'Supplier',
    ];

    constructor(
        private expenseService: ExpenseService,
        private businessAccountService: BusinessAccountService,
        private branchContext: BranchContextService,
        private messageService: MessageService
    ) {}

    get dialogTitle(): string {
        return this.expenseId ? 'Edit Expense' : 'Create Expense';
    }

    ngOnChanges(changes: SimpleChanges): void {
        if (changes['visible'] && this.visible) {
            this.resetForm();
            this.loadPaymentAccounts().then(() => {
                if (this.expenseId) {
                    this.loadExpense(this.expenseId);
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

    accountOptionLabel(account: BusinessAccountDto): string {
        return account.accountType
            ? `${account.name} (${account.accountType})`
            : account.name;
    }

    save(): void {
        if (!this.expense.expenseDate) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Validation',
                detail: 'Expense date is required',
            });
            return;
        }
        if (!this.expense.paymentAccountId) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Validation',
                detail: 'Payment account is required',
            });
            return;
        }
        if (!this.expense.amount || this.expense.amount <= 0) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Validation',
                detail: 'Amount must be greater than zero',
            });
            return;
        }

        let branchId: number;
        try {
            branchId = this.branchContext.requireBranchId();
        } catch (e: any) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Validation',
                detail: e?.message || 'Please select a branch from the top navigation.',
            });
            return;
        }

        const payload: CreateExpenseDto = {
            branchId,
            expenseDate: this.expense.expenseDate,
            amount: this.expense.amount,
            referenceNo: this.expense.referenceNo?.trim() || undefined,
            description: this.expense.description?.trim() || undefined,
            paymentAccountId: this.expense.paymentAccountId,
        };

        this.saving = true;
        const request = this.expenseId
            ? this.expenseService.replace(this.expenseId, payload)
            : this.expenseService.create(payload);

        request
            .then(() => {
                this.messageService.add({
                    severity: 'success',
                    summary: 'Success',
                    detail: this.expenseId
                        ? 'Expense updated successfully'
                        : 'Expense created successfully',
                });
                this.saved.emit();
                this.onHide();
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail:
                        error?.message ||
                        (this.expenseId
                            ? 'Failed to update expense'
                            : 'Failed to create expense'),
                });
            })
            .finally(() => {
                this.saving = false;
            });
    }

    private emptyExpense(): CreateExpenseDto {
        return {
            branchId: 0,
            expenseDate: this.toDateInputValue(),
            amount: 0,
            referenceNo: '',
            description: '',
            paymentAccountId: null as any,
        };
    }

    private resetForm(): void {
        this.expense = this.emptyExpense();
        this.saving = false;
        this.loading = false;
    }

    private toDateInputValue(value?: string | Date): string {
        const date = value ? new Date(value) : new Date();
        if (isNaN(date.getTime())) {
            return this.toDateInputValue();
        }
        const year = date.getFullYear();
        const month = `${date.getMonth() + 1}`.padStart(2, '0');
        const day = `${date.getDate()}`.padStart(2, '0');
        return `${year}-${month}-${day}`;
    }

    private async loadPaymentAccounts(): Promise<void> {
        this.loading = true;
        try {
            const result = await this.businessAccountService.getAll({
                skipCount: 0,
                maxResultCount: 1000,
            });
            const items = result.items || [];
            let accounts = items.filter(
                (a) =>
                    a.isActive !== false &&
                    this.excludedAccountTypes.indexOf(a.accountType || '') < 0
            );
            if (!accounts.length) {
                accounts = items.filter((a) => a.isActive !== false);
            }
            this.paymentAccounts = accounts;

            if (!this.expenseId && this.paymentAccounts.length === 1) {
                this.expense.paymentAccountId = this.paymentAccounts[0].id;
            }
        } catch (error: any) {
            this.messageService.add({
                severity: 'error',
                summary: 'Error',
                detail: error?.message || 'Failed to load payment accounts',
            });
        } finally {
            if (!this.expenseId) {
                this.loading = false;
            }
        }
    }

    private loadExpense(id: number): void {
        this.loading = true;
        this.expenseService
            .get(id)
            .then((item) => {
                this.expense = {
                    branchId: item.branchId ?? 0,
                    expenseDate: this.toDateInputValue(item.expenseDate),
                    amount: item.amount,
                    referenceNo: item.referenceNo || '',
                    description: item.description || '',
                    paymentAccountId: item.paymentAccountId,
                };
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to load expense',
                });
                this.onHide();
            })
            .finally(() => {
                this.loading = false;
            });
    }
}
