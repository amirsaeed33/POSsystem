import {
    Component,
    EventEmitter,
    Input,
    OnChanges,
    Output,
    SimpleChanges,
} from '@angular/core';
import { MessageService } from 'primeng/api';
import { ExpenseDto } from 'src/app/demo/api/expense';
import { CompanyProfileDto } from 'src/app/demo/api/company-profile';
import { ExpenseService } from 'src/app/demo/service/expense.service';
import { CompanyProfileService } from 'src/app/demo/service/company-profile.service';

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
    company: CompanyProfileDto | null = null;
    loading = false;
    today = new Date().toLocaleString();

    constructor(
        private expenseService: ExpenseService,
        private companyProfileService: CompanyProfileService,
        private messageService: MessageService
    ) {}

    get companyLogoUrl(): string {
        return this.companyProfileService.getImageUrl(this.company?.imagePath);
    }

    ngOnChanges(changes: SimpleChanges): void {
        if (changes['visible'] && this.visible && this.expenseId) {
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
            this.companyProfileService.getCurrent(),
        ])
            .then(([expense, company]) => {
                this.expense = expense;
                this.company = company;
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to load expense invoice',
                });
                this.onHide();
            })
            .finally(() => {
                this.loading = false;
            });
    }
}
