import {
  Component,
  Injector,
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  OnInit,
  ViewChild,
  ElementRef,
  AfterViewChecked
} from '@angular/core';
import { Router } from '@angular/router';
import { AppComponentBase } from '@shared/app-component-base';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppConsts } from '@shared/AppConsts';
import {
  DashboardDto,
  DashboardProductRowDto,
  DashboardServiceProxy,
  MonthlyCashFlowDto
} from '@shared/service-proxies/service-proxies';

@Component({
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.less'],
  animations: [appModuleAnimation()],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class HomeComponent extends AppComponentBase implements OnInit, AfterViewChecked {
  @ViewChild('barsScroll') barsScroll?: ElementRef<HTMLDivElement>;

  loading = true;
  data: DashboardDto = new DashboardDto();
  searchText = '';
  hoveredMonth: MonthlyCashFlowDto | null = null;
  maxCashFlow = 1;
  private shouldScrollToActiveMonth = false;

  constructor(
    injector: Injector,
    private _dashboardService: DashboardServiceProxy,
    public cdr: ChangeDetectorRef,
    private _router: Router
  ) {
    super(injector);
  }

  ngOnInit(): void {
    this.load();
  }

  ngAfterViewChecked(): void {
    if (!this.shouldScrollToActiveMonth || !this.barsScroll?.nativeElement || !this.hoveredMonth) {
      return;
    }

    const scroller = this.barsScroll.nativeElement;
    const active = scroller.querySelector('.bar-group.active') as HTMLElement | null;
    if (active) {
      const left = active.offsetLeft - scroller.clientWidth / 2 + active.clientWidth / 2;
      scroller.scrollLeft = Math.max(0, left);
    } else {
      scroller.scrollLeft = scroller.scrollWidth;
    }
    this.shouldScrollToActiveMonth = false;
  }

  load(): void {
    this.loading = true;
    this._dashboardService.get().subscribe({
      next: (result) => {
        this.data = result;
        const values = (result.cashFlow || []).flatMap((m) => [m.income || 0, m.expense || 0]);
        this.maxCashFlow = Math.max(1, ...values);
        this.hoveredMonth =
          (result.cashFlow || []).find((m) => (m.income || 0) + (m.expense || 0) > 0) ||
          (result.cashFlow || [])[result.cashFlow.length - 1] ||
          null;
        // Prefer current calendar month when present
        const now = new Date();
        const current = (result.cashFlow || []).find(
          (m) => m.year === now.getFullYear() && m.month === now.getMonth() + 1
        );
        if (current) {
          this.hoveredMonth = current;
        }
        this.shouldScrollToActiveMonth = true;
        this.loading = false;
        this.cdr.markForCheck();
      },
      error: () => {
        this.loading = false;
        this.cdr.markForCheck();
      }
    });
  }

  get greeting(): string {
    const name = this.data?.userDisplayName || this.appSession.user?.name || 'User';
    return this.l('DashboardGreeting', name);
  }

  get userImageUrl(): string {
    const path =
      this.data?.userImageUrl || this.appSession.user?.userImageUrl || '';
    if (path) {
      return AppConsts.remoteServiceBaseUrl + path;
    }
    return 'assets/img/user.png';
  }

  get filteredProducts(): DashboardProductRowDto[] {
    const list = this.data?.products || [];
    const q = (this.searchText || '').trim().toLowerCase();
    if (!q) {
      return list;
    }
    return list.filter(
      (p) =>
        (p.name || '').toLowerCase().includes(q) ||
        (p.sku || '').toLowerCase().includes(q) ||
        (p.categoryName || '').toLowerCase().includes(q)
    );
  }

  get inStockPercent(): number {
    return this.percentOf(this.data?.inStockCount || 0);
  }

  get lowStockPercent(): number {
    return this.percentOf(this.data?.lowStockCount || 0);
  }

  get outOfStockPercent(): number {
    return this.percentOf(this.data?.outOfStockCount || 0);
  }

  get donutBackground(): string {
    const inStock = this.inStockPercent;
    const low = this.lowStockPercent;
    const out = this.outOfStockPercent;
    if (inStock + low + out <= 0) {
      return 'conic-gradient(#e5e7eb 0deg 360deg)';
    }
    const a = inStock * 3.6;
    const b = a + low * 3.6;
    return `conic-gradient(#22c55e 0deg ${a}deg, #f472b6 ${a}deg ${b}deg, #d1d5db ${b}deg 360deg)`;
  }

  barHeight(value: number): string {
    const pct = Math.max(4, Math.round(((value || 0) / this.maxCashFlow) * 100));
    return pct + '%';
  }

  selectMonth(month: MonthlyCashFlowDto): void {
    this.hoveredMonth = month;
    this.cdr.markForCheck();
  }

  formatMoney(value: number): string {
    return (value || 0).toLocaleString(undefined, {
      minimumFractionDigits: 0,
      maximumFractionDigits: 2
    });
  }

  formatNumber(value: number): string {
    return (value || 0).toLocaleString();
  }

  statusLabel(status: string | undefined): string {
    if (status === 'InStock') {
      return this.l('InStock');
    }
    if (status === 'LowStock') {
      return this.l('LowStock');
    }
    return this.l('OutOfStock');
  }

  getImageUrl(product: DashboardProductRowDto): string {
    if (!product?.imagePath) {
      return '';
    }
    return AppConsts.remoteServiceBaseUrl + product.imagePath;
  }

  goToProducts(): void {
    this._router.navigate(['/app/products']);
  }

  goToSales(): void {
    this._router.navigate(['/app/sales']);
  }

  goToPurchases(): void {
    this._router.navigate(['/app/purchases']);
  }

  private percentOf(count: number): number {
    const total = this.data?.totalProducts || 0;
    if (!total) {
      return 0;
    }
    return Math.round((count / total) * 100);
  }
}
