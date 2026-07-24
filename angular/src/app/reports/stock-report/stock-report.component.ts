import { ChangeDetectorRef, Component, Injector, OnInit } from '@angular/core';
import { AppComponentBase } from '@shared/app-component-base';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import {
  ReportServiceProxy,
  StockReportDto,
  StockReportRowDto
} from '@shared/service-proxies/service-proxies';

@Component({
  templateUrl: './stock-report.component.html',
  animations: [appModuleAnimation()]
})
export class StockReportComponent extends AppComponentBase implements OnInit {
  loading = false;
  keyword = '';
  report: StockReportDto = new StockReportDto();

  constructor(
    injector: Injector,
    private _reportService: ReportServiceProxy,
    private cd: ChangeDetectorRef
  ) {
    super(injector);
  }

  ngOnInit(): void {
    this.generate();
  }

  generate(): void {
    this.loading = true;
    this._reportService.getStockReport(this.keyword || undefined).subscribe({
      next: (result) => {
        this.report = result || new StockReportDto();
        this.loading = false;
        this.cd.detectChanges();
      },
      error: () => {
        this.loading = false;
        this.cd.detectChanges();
      }
    });
  }

  print(): void {
    window.print();
  }

  formatMoney(value: number): string {
    return (value || 0).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  }

  formatNumber(value: number): string {
    return (value || 0).toLocaleString();
  }

  statusLabel(status: string | undefined): string {
    if (status === 'InStock') return this.l('InStock');
    if (status === 'LowStock') return this.l('LowStock');
    return this.l('OutOfStock');
  }

  trackById(_: number, item: StockReportRowDto): number {
    return item.id;
  }
}
