import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { MessageService } from 'primeng/api';
import { SaleReportDto } from 'src/app/demo/api/report';
import { ReportService } from 'src/app/demo/service/report.service';
import { NotificationEmailService } from '../../service/notification-email.service';

@Component({
    selector: 'app-sale-report',
    templateUrl: './sale-report.component.html',
    providers: [MessageService],
})
export class SaleReportComponent implements OnInit {
    loading = false;
    printing = false;
    sendingEmail = false;
    fromDate = '';
    toDate = '';
    keyword = '';
    report: SaleReportDto = { totalAmount: 0, items: [] };

    printDialogVisible = false;
    printingSaleId: number | null = null;

    constructor(
        private reportService: ReportService,
        private notificationEmailService: NotificationEmailService,
        private messageService: MessageService,
        private cd: ChangeDetectorRef
    ) {}

    ngOnInit(): void {
        const now = new Date();
        this.fromDate = `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}-01`;
        this.toDate = this.toDateInputValue();
        this.generate();
    }

    get tableRows(): number {
        if (this.printing) {
            return Math.max(this.report.items?.length || 0, 1);
        }
        return 25;
    }

    generate(): void {
        this.loading = true;
        this.reportService
            .getSaleReport({
                fromDate: this.fromDate || undefined,
                toDate: this.toDate || undefined,
                keyword: this.keyword?.trim() || undefined,
            })
            .then((result) => {
                this.report = result;
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to load sale report',
                });
            })
            .finally(() => {
                this.loading = false;
            });
    }

    /** Match angular-old: print full generated dataset (not just current page). */
    printReport(): void {
        this.printing = true;
        this.cd.detectChanges();
        setTimeout(() => {
            const cleanup = () => {
                this.printing = false;
                this.cd.detectChanges();
                window.removeEventListener('afterprint', cleanup);
            };
            window.addEventListener('afterprint', cleanup);
            window.print();
            setTimeout(cleanup, 1000);
        }, 100);
    }

    printInvoice(saleId: number): void {
        this.printingSaleId = saleId;
        this.printDialogVisible = true;
    }

    private toDateInputValue(date: Date = new Date()): string {
        const year = date.getFullYear();
        const month = `${date.getMonth() + 1}`.padStart(2, '0');
        const day = `${date.getDate()}`.padStart(2, '0');
        return `${year}-${month}-${day}`;
    }

    sendDailySummaryEmail(): void {
        this.sendingEmail = true;
        this.notificationEmailService.sendDailyBusinessSummary({})
            .subscribe({
                next: () => {
                    this.sendingEmail = false;
                    this.messageService.add({
                        severity: 'success',
                        summary: 'Email Dispatched',
                        detail: 'Daily business summary report has been sent to your email.'
                    });
                },
                error: (err: any) => {
                    this.sendingEmail = false;
                    this.messageService.add({
                        severity: 'error',
                        summary: 'Email Failed',
                        detail: err?.error?.error?.message || err?.message || 'Failed to send email. Check SMTP settings.'
                    });
                }
            });
    }
}
