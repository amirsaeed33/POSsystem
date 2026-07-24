import { ChangeDetectorRef, Component, Injector, OnInit } from '@angular/core';
import { AppComponentBase } from '@shared/app-component-base';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import {
  ReportServiceProxy,
  ExpenseReportDto,
  ExpenseReportRowDto
} from '@shared/service-proxies/service-proxies';

@Component({
  templateUrl: './expense-report.component.html',
  animations: [appModuleAnimation()]
})
export class ExpenseReportComponent extends AppComponentBase implements OnInit {
  loading = false;
  fromDate: string;
  toDate: string;
  keyword = '';
  report: ExpenseReportDto = new ExpenseReportDto();

  constructor(
    injector: Injector,
    private _reportService: ReportServiceProxy,
    private cd: ChangeDetectorRef
  ) {
    super(injector);
  }

  ngOnInit(): void {
    const now = new Date();
    this.fromDate = `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}-01`;
    this.toDate = this.toDateInputValue();
    this.generate();
  }

  generate(): void {
    this.loading = true;
    this._reportService
      .getExpenseReport(this.toDateObj(this.fromDate), this.toDateObj(this.toDate), this.keyword || undefined)
      .subscribe({
        next: (result) => {
          this.report = result || new ExpenseReportDto();
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

  formatDate(value: Date | string): string {
    if (!value) return '';
    const d = value instanceof Date ? value : new Date(value);
    return isNaN(d.getTime()) ? '' : d.toLocaleDateString();
  }

  trackById(_: number, item: ExpenseReportRowDto): number {
    return item.id;
  }

  private toDateObj(value: string): Date | undefined {
    if (!value) return undefined;
    return new Date(value + 'T00:00:00');
  }
}
