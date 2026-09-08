import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { MessageService } from 'primeng/api';
import { BalanceSheetReportDto } from 'src/app/demo/api/report';
import { ReportService } from 'src/app/demo/service/report.service';

@Component({
    templateUrl: './balance-sheet-report.component.html',
    providers: [MessageService],
})
export class BalanceSheetReportComponent implements OnInit {
    loading = false;
    printing = false;
    fromDate = '';
    toDate = '';
    keyword = '';
    report: BalanceSheetReportDto = {
        totalAssets: 0,
        totalLiabilities: 0,
        totalEquity: 0,
        netProfitOrLoss: 0,
        initialCapital: 0,
        isBalanced: true,
        assetCategories: [],
        liabilityCategories: [],
        equityCategories: [],
    };

    constructor(
        private reportService: ReportService,
        private messageService: MessageService,
        private cd: ChangeDetectorRef
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
            .getBalanceSheetReport({
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
                    detail: error?.message || 'Failed to load balance sheet report',
                });
            })
            .finally(() => {
                this.loading = false;
            });
    }

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

    private toDateInputValue(date: Date = new Date()): string {
        const year = date.getFullYear();
        const month = `${date.getMonth() + 1}`.padStart(2, '0');
        const day = `${date.getDate()}`.padStart(2, '0');
        return `${year}-${month}-${day}`;
    }
}
