import {
    ChangeDetectorRef,
    Component,
    EventEmitter,
    Input,
    OnChanges,
    Output,
    SimpleChanges,
} from '@angular/core';
import { MessageService } from 'primeng/api';
import { BranchDto } from 'src/app/demo/api/branch';
import { ExpenseDto } from 'src/app/demo/api/expense';
import { BranchService } from 'src/app/demo/service/branch.service';
import { ExpenseService } from 'src/app/demo/service/expense.service';

@Component({
    selector: 'app-print-expense-invoice-dialog',
    templateUrl: './print-expense-invoice-dialog.component.html',
    styleUrls: ['./print-expense-invoice-dialog.component.css'],
})
export class PrintExpenseInvoiceDialogComponent implements OnChanges {
    @Input() visible = false;
    @Input() expenseId: number | null = null;
    @Output() visibleChange = new EventEmitter<boolean>();

    expense: ExpenseDto | null = null;
    company: BranchDto | null = null;
    loading = false;
    today = new Date().toLocaleString();

    constructor(
        private expenseService: ExpenseService,
        private branchService: BranchService,
        private messageService: MessageService,
        private cd: ChangeDetectorRef
    ) {}

    get companyLogoUrl(): string {
        return this.branchService.getImageUrl(this.company?.imagePath);
    }

    ngOnChanges(changes: SimpleChanges): void {
        const becameVisible = changes['visible']?.currentValue === true;
        const expenseIdChanged = !!changes['expenseId'] && this.visible;
        if ((becameVisible || expenseIdChanged) && this.visible && this.expenseId) {
            this.load(this.expenseId);
        }
    }

    onVisibleChange(visible: boolean): void {
        this.visible = visible;
        this.visibleChange.emit(visible);
        if (!visible) {
            this.expense = null;
            this.company = null;
        }
    }

    onHide(): void {
        this.onVisibleChange(false);
    }

    print(): void {
        setTimeout(() => {
            document.body.classList.add('printing-invoice');
            const cleanup = () => {
                document.body.classList.remove('printing-invoice');
                window.removeEventListener('afterprint', cleanup);
            };
            window.addEventListener('afterprint', cleanup);
            window.print();
            setTimeout(cleanup, 1000);
        }, 100);
    }

    private load(id: number): void {
        this.loading = true;
        this.expense = null;
        this.company = null;
        this.today = new Date().toLocaleString();

        Promise.all([
            this.expenseService.get(id),
            this.branchService.getInvoiceInfo(),
        ])
            .then(([expense, company]) => {
                this.expense = expense;
                this.company = company;
                this.loading = false;
                this.cd.detectChanges();
            })
            .catch((error) => {
                this.loading = false;
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to load expense invoice',
                });
                this.onHide();
            });
    }
}
