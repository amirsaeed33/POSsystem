import { Component, OnInit, ViewEncapsulation } from '@angular/core';
import { MessageService } from 'primeng/api';
import { StockReportDto, StockReportRowDto } from 'src/app/demo/api/report';
import { ReportService } from 'src/app/demo/service/report.service';
import { NotificationEmailService } from 'src/app/demo/service/notification-email.service';

@Component({
    templateUrl: './stock-report.component.html',
    styleUrls: ['./stock-report.component.css'],
    providers: [MessageService],
    encapsulation: ViewEncapsulation.None,
})
export class StockReportComponent implements OnInit {
    loading = false;
    sendingEmail = false;
    keyword = '';
    report: StockReportDto = {
        totalProducts: 0,
        inStockCount: 0,
        lowStockCount: 0,
        outOfStockCount: 0,
        totalStockUnits: 0,
        totalStockCostValue: 0,
        totalStockSellValue: 0,
        totalStockProfit: 0,
        items: [],
    };

    constructor(
        private reportService: ReportService,
        private notificationEmailService: NotificationEmailService,
        private messageService: MessageService
    ) {}

    ngOnInit(): void {
        this.generate();
    }

    generate(): void {
        this.loading = true;
        this.reportService
            .getStockReport(this.keyword?.trim() || undefined)
            .then((result) => {
                this.report = result;
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to load stock report',
                });
            })
            .finally(() => {
                this.loading = false;
            });
    }

    /** Match angular-old: page-level print of the dedicated print layout. */
    printReport(): void {
        window.print();
    }

    formatMoney(value: number | null | undefined): string {
        return (value || 0).toLocaleString(undefined, {
            minimumFractionDigits: 2,
            maximumFractionDigits: 2,
        });
    }

    formatNumber(value: number | null | undefined): string {
        return (value || 0).toLocaleString();
    }

    statusLabel(status?: string): string {
        if (status === 'InStock') {
            return 'In Stock';
        }
        if (status === 'LowStock') {
            return 'Low Stock';
        }
        return 'Out of Stock';
    }

    statusSeverity(
        status?: string
    ): 'success' | 'warning' | 'danger' | 'info' | 'secondary' | 'contrast' {
        if (status === 'InStock') {
            return 'success';
        }
        if (status === 'LowStock') {
            return 'warning';
        }
        return 'danger';
    }

    trackById(_: number, item: StockReportRowDto): number {
        return item.id;
    }

    sendEmailAlert(): void {
        this.sendingEmail = true;
        this.notificationEmailService.sendLowStockReport({})
            .subscribe({
                next: () => {
                    this.sendingEmail = false;
                    this.messageService.add({
                        severity: 'success',
                        summary: 'Email Dispatched',
                        detail: 'Low stock alert report has been sent to your email.'
                    });
                },
                error: (err) => {
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
