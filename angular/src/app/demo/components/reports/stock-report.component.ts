import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { MessageService } from 'primeng/api';
import { StockReportDto } from 'src/app/demo/api/report';
import { ReportService } from 'src/app/demo/service/report.service';

@Component({
    templateUrl: './stock-report.component.html',
    providers: [MessageService],
})
export class StockReportComponent implements OnInit {
    loading = false;
    printing = false;
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
        private messageService: MessageService,
        private cd: ChangeDetectorRef
    ) {}

    ngOnInit(): void {
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
}
