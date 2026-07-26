import { Component, OnInit } from '@angular/core';
import { MessageService } from 'primeng/api';
import { PurchaseReportDto } from 'src/app/demo/api/report';
import { ReportService } from 'src/app/demo/service/report.service';

@Component({
    templateUrl: './purchase-report.component.html',
    providers: [MessageService],
})
export class PurchaseReportComponent implements OnInit {
    loading = false;
    fromDate = '';
    toDate = '';
    keyword = '';
    report: PurchaseReportDto = { totalAmount: 0, items: [] };

    printDialogVisible = false;
    printingPurchaseId: number | null = null;

    constructor(
        private reportService: ReportService,
        private messageService: MessageService
    ) {}

    ngOnInit(): void {
        const now = new Date();
        this.fromDate = `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}-01`;
        this.toDate = this.toDateInputValue();
        this.generate();
    }

    generate(): void {
        this.loading = true;
        this.reportService
            .getPurchaseReport({
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
                    detail: error?.message || 'Failed to load purchase report',
                });
            })
            .finally(() => {
                this.loading = false;
            });
    }

    printReport(): void {
        window.print();
    }

    printInvoice(purchaseId: number): void {
        this.printingPurchaseId = purchaseId;
        this.printDialogVisible = true;
    }

    private toDateInputValue(date: Date = new Date()): string {
        const year = date.getFullYear();
        const month = `${date.getMonth() + 1}`.padStart(2, '0');
        const day = `${date.getDate()}`.padStart(2, '0');
        return `${year}-${month}-${day}`;
    }
}
