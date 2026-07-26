import { Component, OnInit } from '@angular/core';
import { MessageService } from 'primeng/api';
import { StockReportDto } from 'src/app/demo/api/report';
import { ReportService } from 'src/app/demo/service/report.service';

@Component({
    templateUrl: './stock-report.component.html',
    providers: [MessageService],
})
export class StockReportComponent implements OnInit {
    loading = false;
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

    printReport(): void {
        window.print();
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
