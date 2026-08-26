import {Component, OnDestroy, OnInit} from '@angular/core';
import { Subscription, debounceTime } from 'rxjs';
import { LayoutService } from 'src/app/layout/service/app.layout.service';
import { DashboardService } from 'src/app/demo/service/dashboard.service';
import { PermissionService } from 'src/app/demo/service/permission.service';
import { PermissionNames } from 'src/app/demo/api/permission-names';
import {
    MonthlyCashFlowDto,
    DailyOverviewDto,
    DashboardProductRowDto,
    DashboardTimelineEventDto,
} from 'src/app/demo/api/dashboard';

interface LatestListItem {
    title: string;
    subtitle: string;
    initials: string;
    avatarStyle: Record<string, string>;
}

interface TopProductItem {
    name: string;
    categoryName: string;
    unitsSold: number;
    rank: number;
}

interface SalesGroupRow {
    name: string;
    salesAmount: number;
    growthPercent: number;
    isGrowthPositive: boolean;
}

interface StockHighlightItem {
    name: string;
    categoryName: string;
    stockRemaining: number;
    unitsSold: number;
    isLowStock: boolean;
    statusLabel: string;
}

interface TimelineEvent {
    type: 'sale' | 'purchase' | 'expense' | 'stock';
    title: string;
    amountLabel: string;
    timeAgo: string;
    occurredAt: number;
    icon: string;
    iconClass: string;
    lineClass: string;
    detailBg: string;
    detailBgDark: string;
}

@Component({
    templateUrl: './saas.dashboard.component.html',
    styleUrls: ['./saas.dashboard.component.scss']
})
export class SaaSDashboardComponent implements OnInit, OnDestroy {

    overviewChartData: any;

    overviewChartOptions: any;

    overviewWeeks: any;

    selectedOverviewWeek: any ;

    revenueChartData: any;

    revenueChartOptions: any;

    trend15DaysChartData: any;

    trend15DaysChartOptions: any;

    trendMetricOptions = [
        { label: 'Sales', value: 'sales' },
        { label: 'Purchases', value: 'purchases' },
        { label: 'Expenses', value: 'expenses' }
    ];

    selectedTrendMetric: 'sales' | 'purchases' | 'expenses' = 'sales';

    trendChartTypes = [
        { label: 'Line', value: 'line' },
        { label: 'Bar', value: 'bar' }
    ];

    selectedTrendChartType = 'bar';

    subscription: Subscription;

    todaySales = 0;
    todayPurchases = 0;
    todayExpenses = 0;
    todayProfit = 0;

    cashFlow: MonthlyCashFlowDto[] = [];
    last7Days: DailyOverviewDto[] = [];
    last15Days: DailyOverviewDto[] = [];

    topProducts: TopProductItem[] = [];
    salesByCategory: SalesGroupRow[] = [];
    salesByBrand: SalesGroupRow[] = [];

    averageProfitMargin = 0;
    previousAverageProfitMargin = 0;
    growthPercentage = 0;
    isGrowthPositive = true;

    stockHighlights: StockHighlightItem[] = [];
    stockHighlightIndex = 0;

    latestListTitle = 'Latest Customers';
    latestListItems: LatestListItem[] = [];

    timelineEvents: TimelineEvent[] = [];
    timelineVisibleCount = 5;

    lowStockCount = 0;
    pendingOrdersCount = 0;
    todaySalesCount = 0;
    branchCount = 0;

    private readonly avatarStyles: Record<string, string>[] = [
        {'background-color':'rgba(101, 214, 173, 0.1)', 'color': '#27AB83', 'border': '1px solid #65D6AD'},
        {'background-color':'rgba(250, 219, 95, 0.1)', 'color': '#DE911D', 'border': '1px solid #FADB5F'},
        {'background-color':'rgba(94, 208, 250, 0.1)', 'color': '#1992D4', 'border': '1px solid #5ED0FA'},
        {'background-color':'rgba(43, 176, 237, 0.1)', 'color': '#127FBF', 'border': '1px solid #2BB0ED'},
        {'background-color':'rgba(255, 155, 155, 0.1)', 'color': '#CF1124', 'border': '1px solid #FF9B9B'},
        {'background-color':'rgba(250, 219, 95, 0.1)', 'color': '#DE911D', 'border': '1px solid #FADB5F'},
    ];

    constructor(
        public layoutService: LayoutService,
        private dashboardService: DashboardService,
        private permissionService: PermissionService
    ) {
        this.subscription = this.layoutService.configUpdate$
        .pipe(debounceTime(25))
        .subscribe((config) => {
            this.initCharts();
        });
    }

    canOpenPos(): boolean {
        return this.permissionService.isGranted(PermissionNames.Sales);
    }

    ngOnInit() {
        this.overviewWeeks = [
            { name: 'Last 7 Days', code: 'last-7-days' },
            { name: 'Today (Day Wise)', code: 'today' },
            { name: 'This Month', code: 'this-month' }
        ];
        this.selectedOverviewWeek = this.overviewWeeks[0];

        this.initCharts();
        this.loadDashboard();
    }

    get visibleTimelineEvents(): TimelineEvent[] {
        return this.timelineEvents.slice(0, this.timelineVisibleCount);
    }

    get hasMoreTimelineEvents(): boolean {
        return this.timelineEvents.length > this.timelineVisibleCount;
    }

    showMoreTimelineEvents() {
        this.timelineVisibleCount = Math.min(
            this.timelineVisibleCount + 5,
            this.timelineEvents.length
        );
    }

    async loadDashboard() {
        try {
            const data = await this.dashboardService.get();
            this.todaySales = data.todaySales ?? 0;
            this.todayPurchases = data.todayPurchases ?? 0;
            this.todayExpenses = data.todayExpenses ?? 0;
            this.todayProfit = data.todayProfit ?? 0;
            this.averageProfitMargin = data.averageProfitMargin ?? 0;
            this.previousAverageProfitMargin =
                data.previousAverageProfitMargin ?? 0;
            this.growthPercentage = data.growthPercentage ?? 0;
            this.isGrowthPositive = data.isGrowthPositive ?? true;
            this.last7Days = data.last7Days ?? [];
            this.last15Days = data.last15Days ?? [];
            this.cashFlow = data.cashFlow ?? [];
            const products = data.products ?? [];
            this.topProducts = this.buildTopProducts(products);
            this.salesByCategory = this.buildSalesGroups(products, 'category');
            this.salesByBrand = this.buildSalesGroups(products, 'brand');
            this.stockHighlights = this.buildStockHighlights(products);
            this.stockHighlightIndex = 0;

            const quick = data.quickActions;
            this.lowStockCount =
                quick?.lowStockCount ?? data.lowStockCount ?? 0;
            this.pendingOrdersCount = quick?.pendingOrdersCount ?? 0;
            this.todaySalesCount = quick?.todaySalesCount ?? 0;
            this.branchCount = quick?.branchCount ?? 0;

            this.latestListTitle = data.latestListTitle || 'Latest Sales';
            this.latestListItems = (data.latestListItems || []).map(
                (item, index) => ({
                    title: item.title,
                    subtitle: item.subtitle,
                    initials: item.initials || '?',
                    avatarStyle:
                        this.avatarStyles[index % this.avatarStyles.length],
                })
            );

            this.timelineEvents = this.mapTimelineEvents(data.timeline || []);
            this.timelineVisibleCount = 5;
        } catch {
            this.todaySales = 0;
            this.todayPurchases = 0;
            this.todayExpenses = 0;
            this.todayProfit = 0;
            this.averageProfitMargin = 0;
            this.previousAverageProfitMargin = 0;
            this.growthPercentage = 0;
            this.isGrowthPositive = true;
            this.cashFlow = [];
            this.topProducts = [];
            this.salesByCategory = [];
            this.salesByBrand = [];
            this.stockHighlights = [];
            this.stockHighlightIndex = 0;
            this.lowStockCount = 0;
            this.pendingOrdersCount = 0;
            this.todaySalesCount = 0;
            this.branchCount = 0;
            this.latestListTitle = 'Latest Sales';
            this.latestListItems = [];
            this.timelineEvents = [];
        }
        this.buildOverviewChart();
        this.buildRevenueChart();
        this.build15DayTrendChart();
    }

    private mapTimelineEvents(
        events: DashboardTimelineEventDto[]
    ): TimelineEvent[] {
        const styles: Record<
            string,
            Pick<
                TimelineEvent,
                'icon' | 'iconClass' | 'lineClass' | 'detailBg' | 'detailBgDark'
            >
        > = {
            sale: {
                icon: 'pi pi-shopping-cart',
                iconClass: 'bg-blue-100 text-blue-500',
                lineClass: 'bg-blue-100',
                detailBg: 'rgba(227, 248, 255, 0.5)',
                detailBgDark: 'rgba(227, 248, 255, 0.1)',
            },
            purchase: {
                icon: 'pi pi-shopping-bag',
                iconClass: 'bg-yellow-100 text-yellow-500',
                lineClass: 'bg-yellow-100',
                detailBg: 'rgba(255, 249, 230, 0.5)',
                detailBgDark: 'rgba(255, 249, 230, 0.1)',
            },
            expense: {
                icon: 'pi pi-wallet',
                iconClass: 'bg-red-100 text-red-500',
                lineClass: 'bg-red-100',
                detailBg: 'rgba(255, 235, 238, 0.5)',
                detailBgDark: 'rgba(255, 235, 238, 0.1)',
            },
            stock: {
                icon: 'pi pi-sync',
                iconClass: 'bg-green-100 text-green-500',
                lineClass: 'bg-green-100',
                detailBg: 'rgba(232, 245, 233, 0.5)',
                detailBgDark: 'rgba(232, 245, 233, 0.1)',
            },
        };

        return events.map((event) => {
            const type = (
                ['sale', 'purchase', 'expense', 'stock'].includes(event.type)
                    ? event.type
                    : 'sale'
            ) as TimelineEvent['type'];
            const style = styles[type] || styles['sale'];
            const amountLabel =
                type === 'stock'
                    ? event.quantityLabel || 'Stock adjusted'
                    : this.formatPkr(event.amount ?? 0);

            return {
                type,
                title: event.title,
                amountLabel,
                timeAgo: this.formatTimeAgo(event.occurredAt),
                occurredAt: new Date(event.occurredAt).getTime() || 0,
                ...style,
            };
        });
    }

    private formatTimeAgo(value: string | Date): string {
        const date = new Date(value);
        if (isNaN(date.getTime())) {
            return '';
        }
        const seconds = Math.max(
            0,
            Math.floor((Date.now() - date.getTime()) / 1000)
        );
        if (seconds < 60) {
            return 'just now';
        }
        const minutes = Math.floor(seconds / 60);
        if (minutes < 60) {
            return minutes === 1
                ? '1 minute ago'
                : `${minutes} minutes ago`;
        }
        const hours = Math.floor(minutes / 60);
        if (hours < 24) {
            return hours === 1 ? '1 hour ago' : `${hours} hours ago`;
        }
        const days = Math.floor(hours / 24);
        return days === 1 ? '1 day ago' : `${days} days ago`;
    }

    formatQuickActionCount(count: number): string {
        if (count > 99) {
            return '99+';
        }
        return String(count ?? 0);
    }

    get currentStockHighlight(): StockHighlightItem | null {
        if (!this.stockHighlights.length) {
            return null;
        }
        return this.stockHighlights[this.stockHighlightIndex] || null;
    }

    prevStockHighlight() {
        if (!this.stockHighlights.length) {
            return;
        }
        this.stockHighlightIndex =
            (this.stockHighlightIndex - 1 + this.stockHighlights.length) %
            this.stockHighlights.length;
    }

    nextStockHighlight() {
        if (!this.stockHighlights.length) {
            return;
        }
        this.stockHighlightIndex =
            (this.stockHighlightIndex + 1) % this.stockHighlights.length;
    }

    private buildStockHighlights(
        products: DashboardProductRowDto[]
    ): StockHighlightItem[] {
        const items = products.map((product) => {
            const status = (product.status || '').toLowerCase();
            const isLowStock =
                status === 'lowstock' || status === 'outofstock';
            return {
                name: product.name || '—',
                categoryName: product.categoryName || '—',
                stockRemaining: product.units ?? 0,
                // Units sold is not on DashboardProductRowDto; stock Units is remaining qty.
                unitsSold: 0,
                isLowStock,
                statusLabel:
                    status === 'outofstock'
                        ? 'Out of Stock'
                        : status === 'lowstock'
                          ? 'Low Stock'
                          : 'In Stock',
            };
        });

        return items.sort((a, b) => {
            if (a.isLowStock === b.isLowStock) {
                return a.stockRemaining - b.stockRemaining;
            }
            return a.isLowStock ? -1 : 1;
        });
    }

    private buildTopProducts(products: DashboardProductRowDto[]): TopProductItem[] {
        return [...products]
            .sort((a, b) => (b.units ?? 0) - (a.units ?? 0))
            .slice(0, 8)
            .map((product, index) => ({
                name: product.name || '—',
                categoryName: product.categoryName || '—',
                unitsSold: product.units ?? 0,
                rank: index + 1,
            }));
    }

    private buildSalesGroups(
        products: DashboardProductRowDto[],
        groupBy: 'category' | 'brand'
    ): SalesGroupRow[] {
        const groups = new Map<string, number>();

        for (const product of products) {
            const name =
                (groupBy === 'category'
                    ? product.categoryName
                    : product.brandName) || 'Uncategorized';
            const amount = (product.price ?? 0) * (product.units ?? 0);
            groups.set(name, (groups.get(name) || 0) + amount);
        }

        return Array.from(groups.entries())
            .map(([name, salesAmount]) => ({
                name,
                salesAmount,
                growthPercent: this.growthPercentage,
                isGrowthPositive: this.isGrowthPositive,
            }))
            .sort((a, b) => b.salesAmount - a.salesAmount);
    }

    formatPkr(amount: number): string {
        const value = amount ?? 0;
        return (
            'PKR ' +
            value.toLocaleString('en-PK', {
                minimumFractionDigits: 0,
                maximumFractionDigits: 2,
            })
        );
    }

    get total15DaysSales(): number {
        return (this.last15Days || []).reduce((acc, x) => acc + (x.sales ?? 0), 0);
    }

    get total15DaysPurchases(): number {
        return (this.last15Days || []).reduce((acc, x) => acc + (x.purchases ?? 0), 0);
    }

    get total15DaysExpenses(): number {
        return (this.last15Days || []).reduce((acc, x) => acc + (x.expenses ?? 0), 0);
    }

    kpiSubtitle(amount: number): string {
        return (amount ?? 0) === 0 ? 'No data today' : '';
    }

    get salesSubtitle(): string {
        return this.kpiSubtitle(this.todaySales);
    }

    get purchasesSubtitle(): string {
        return this.kpiSubtitle(this.todayPurchases);
    }

    get expensesSubtitle(): string {
        return this.kpiSubtitle(this.todayExpenses);
    }

    get profitSubtitle(): string {
        const noActivity =
            this.todaySales === 0 &&
            this.todayPurchases === 0 &&
            this.todayExpenses === 0;
        if (noActivity) {
            return 'No data today';
        }
        return 'Sales − product cost − expenses';
    }

    initCharts() {
        const documentStyle = getComputedStyle(document.documentElement);
        const textColorSecondary = documentStyle.getPropertyValue('--text-color-secondary');
        const borderColor = documentStyle.getPropertyValue('--surface-border');

        this.overviewChartOptions = {
            plugins: {
                legend: {
                    position: 'bottom',
                    align: 'end',
                    labels: {
                        color: textColorSecondary
                    }
                }
            },
            responsive: true,
            hover: {
                mode: 'index'
            },
            scales: {
                y: {
                    min: 0,
                    ticks: {
                        color: textColorSecondary
                    },
                    grid: {
                        borderDash: [2, 2],
                        color: borderColor,
                        drawBorder: false
                    }
                },
                x: {
                    grid: {
                        display: false,
                    },
                    ticks: {
                        beginAtZero: true,
                        color: textColorSecondary
                    }
                }
            }
        };

        this.buildOverviewChart();
        this.buildRevenueChart();
    }

    buildRevenueChart() {
        const documentStyle = getComputedStyle(document.documentElement);
        const textColorSecondary = documentStyle.getPropertyValue('--text-color-secondary');
        const borderColor = documentStyle.getPropertyValue('--surface-border');

        const labels = (this.cashFlow || []).map((x) => x.monthLabel);
        const sales = (this.cashFlow || []).map((x) => x.income ?? 0);
        const outgoing = (this.cashFlow || []).map(
            (x) => (x.purchases ?? 0) + (x.expense ?? 0)
        );

        this.revenueChartData = {
            labels: labels.length ? labels : ['—'],
            datasets: [
                {
                    label: 'Sales',
                    data: labels.length ? sales : [0],
                    borderColor: '#10b981',
                    backgroundColor: 'rgba(16, 185, 129, 0.15)',
                    pointBorderColor: '#10b981',
                    pointBackgroundColor: '#10b981',
                    fill: false,
                    tension: .4
                },
                {
                    label: 'Outgoing',
                    data: labels.length ? outgoing : [0],
                    borderColor: '#ef4444',
                    backgroundColor: 'rgba(239, 68, 68, 0.15)',
                    pointBorderColor: '#ef4444',
                    pointBackgroundColor: '#ef4444',
                    fill: true,
                    tension: .4
                }
            ]
        };

        this.revenueChartOptions = {
            plugins: {
                legend: {
                    display: true,
                    position: 'bottom',
                    align: 'end',
                    labels: {
                        color: textColorSecondary
                    }
                }
            },
            scales: {
                y: {
                    grid: {
                        color: borderColor
                    },
                    min: 0,
                    ticks: {
                        color: textColorSecondary
                    }
                },
                x: {
                    grid: {
                        color: borderColor
                    },
                    ticks: {
                        color: textColorSecondary,
                        beginAtZero: true
                    }
                }
            }
        };
    }

    build15DayTrendChart() {
        const documentStyle = getComputedStyle(document.documentElement);
        const textColorSecondary = documentStyle.getPropertyValue('--text-color-secondary');
        const borderColor = documentStyle.getPropertyValue('--surface-border');

        const dataList: DailyOverviewDto[] = this.last15Days || [];
        const labels = dataList.map((x: DailyOverviewDto) => x.dayLabel);

        const salesValues = dataList.map((x: DailyOverviewDto) => x.sales ?? 0);
        const purchasesValues = dataList.map((x: DailyOverviewDto) => x.purchases ?? 0);
        const expensesValues = dataList.map((x: DailyOverviewDto) => x.expenses ?? 0);

        // Create canvas gradients for a rich, modern look
        const canvas = document.createElement('canvas');
        const ctx = canvas.getContext('2d');

        let salesGrad: any = 'rgba(59, 130, 246, 0.15)';
        let purchasesGrad: any = 'rgba(245, 158, 11, 0.15)';
        let expensesGrad: any = 'rgba(239, 68, 68, 0.15)';

        if (ctx) {
            salesGrad = ctx.createLinearGradient(0, 0, 0, 300);
            salesGrad.addColorStop(0, 'rgba(59, 130, 246, 0.35)');
            salesGrad.addColorStop(1, 'rgba(59, 130, 246, 0.01)');

            purchasesGrad = ctx.createLinearGradient(0, 0, 0, 300);
            purchasesGrad.addColorStop(0, 'rgba(245, 158, 11, 0.30)');
            purchasesGrad.addColorStop(1, 'rgba(245, 158, 11, 0.01)');

            expensesGrad = ctx.createLinearGradient(0, 0, 0, 300);
            expensesGrad.addColorStop(0, 'rgba(239, 68, 68, 0.30)');
            expensesGrad.addColorStop(1, 'rgba(239, 68, 68, 0.01)');
        }

        const isBar = this.selectedTrendChartType === 'bar';

        this.trend15DaysChartData = {
            labels: labels.length ? labels : ['—'],
            datasets: [
                {
                    label: 'Sales',
                    data: labels.length ? salesValues : [0],
                    borderColor: '#2563eb',
                    backgroundColor: isBar ? '#2563eb' : salesGrad,
                    hoverBackgroundColor: isBar ? '#1d4ed8' : undefined,
                    pointBorderColor: '#2563eb',
                    pointBackgroundColor: '#ffffff',
                    pointHoverBackgroundColor: '#2563eb',
                    pointHoverBorderColor: '#ffffff',
                    pointRadius: isBar ? 0 : 4,
                    pointHoverRadius: isBar ? 0 : 7,
                    borderWidth: isBar ? 0 : 3,
                    borderRadius: isBar ? 6 : 0,
                    barPercentage: 0.6,
                    categoryPercentage: 0.7,
                    fill: !isBar,
                    tension: 0.4
                },
                {
                    label: 'Purchases',
                    data: labels.length ? purchasesValues : [0],
                    borderColor: '#d97706',
                    backgroundColor: isBar ? '#d97706' : purchasesGrad,
                    hoverBackgroundColor: isBar ? '#b45309' : undefined,
                    pointBorderColor: '#d97706',
                    pointBackgroundColor: '#ffffff',
                    pointHoverBackgroundColor: '#d97706',
                    pointHoverBorderColor: '#ffffff',
                    pointRadius: isBar ? 0 : 4,
                    pointHoverRadius: isBar ? 0 : 7,
                    borderWidth: isBar ? 0 : 3,
                    borderRadius: isBar ? 6 : 0,
                    barPercentage: 0.6,
                    categoryPercentage: 0.7,
                    fill: !isBar,
                    tension: 0.4
                },
                {
                    label: 'Expenses',
                    data: labels.length ? expensesValues : [0],
                    borderColor: '#dc2626',
                    backgroundColor: isBar ? '#dc2626' : expensesGrad,
                    hoverBackgroundColor: isBar ? '#b91c1c' : undefined,
                    pointBorderColor: '#dc2626',
                    pointBackgroundColor: '#ffffff',
                    pointHoverBackgroundColor: '#dc2626',
                    pointHoverBorderColor: '#ffffff',
                    pointRadius: isBar ? 0 : 4,
                    pointHoverRadius: isBar ? 0 : 7,
                    borderWidth: isBar ? 0 : 3,
                    borderRadius: isBar ? 6 : 0,
                    barPercentage: 0.6,
                    categoryPercentage: 0.7,
                    fill: !isBar,
                    tension: 0.4
                }
            ]
        };

        this.trend15DaysChartOptions = {
            plugins: {
                legend: {
                    display: true,
                    position: 'top',
                    align: 'end',
                    labels: {
                        color: textColorSecondary,
                        usePointStyle: true,
                        pointStyleWidth: 8,
                        boxWidth: 8,
                        boxHeight: 8,
                        padding: 15,
                        font: {
                            size: 12,
                            weight: '600',
                            family: 'Inter, system-ui, -apple-system, sans-serif'
                        },
                        generateLabels: (chart: any) => {
                            const datasets = chart.data.datasets || [];
                            return datasets.map((ds: any, i: number) => {
                                const total = (ds.data || []).reduce((a: number, b: number) => a + (b || 0), 0);
                                const formattedTotal = 'PKR ' + Number(total).toLocaleString('en-PK');
                                const isHidden = !chart.isDatasetVisible(i);
                                return {
                                    text: `${ds.label} (${formattedTotal})`,
                                    fillStyle: ds.borderColor,
                                    strokeStyle: ds.borderColor,
                                    lineWidth: 0,
                                    pointStyle: 'circle',
                                    hidden: isHidden,
                                    datasetIndex: i,
                                    fontColor: isHidden ? 'rgba(156, 163, 175, 0.6)' : textColorSecondary
                                };
                            });
                        }
                    }
                },
                tooltip: {
                    padding: 12,
                    backgroundColor: 'rgba(15, 23, 42, 0.90)',
                    titleFont: { size: 13, weight: '700' },
                    bodyFont: { size: 13, weight: '500' },
                    cornerRadius: 8,
                    displayColors: true,
                    boxWidth: 10,
                    boxHeight: 10,
                    usePointStyle: true,
                    callbacks: {
                        label: (context: any) => {
                            const label = context.dataset.label || '';
                            const val = context.parsed.y ?? 0;
                            return `  ${label}: PKR ${val.toLocaleString('en-PK', { minimumFractionDigits: 0, maximumFractionDigits: 2 })}`;
                        }
                    }
                }
            },
            responsive: true,
            maintainAspectRatio: false,
            hover: {
                mode: 'index',
                intersect: false
            },
            scales: {
                y: {
                    grid: {
                        color: borderColor,
                        borderDash: [4, 4],
                        drawBorder: false
                    },
                    min: 0,
                    ticks: {
                        color: textColorSecondary,
                        font: { size: 11, weight: '500' },
                        callback: (value: any) => 'PKR ' + Number(value).toLocaleString('en-PK')
                    }
                },
                x: {
                    grid: {
                        display: false
                    },
                    ticks: {
                        color: textColorSecondary,
                        font: { size: 11, weight: '500' }
                    }
                }
            }
        };
    }

    buildOverviewChart() {
        const documentStyle = getComputedStyle(document.documentElement);
        const primaryColor = documentStyle.getPropertyValue('--primary-color');
        const primaryColor300 = documentStyle.getPropertyValue('--primary-200');
        const isDark = this.layoutService.config().colorScheme === 'dark';
        const barColor = isDark ? '#879AAF' : '#E4E7EB';
        const barColorAlt = isDark ? '#5B6B7C' : '#C5CBD3';

        const period = this.selectedOverviewWeek?.code;
        let labels: string[];
        let sales: number[];
        let purchases: number[];
        let expenses: number[];

        if (period === 'last-7-days' && this.last7Days.length) {
            labels = this.last7Days.map((x) => x.dayLabel);
            sales = this.last7Days.map((x) => x.sales ?? 0);
            purchases = this.last7Days.map((x) => x.purchases ?? 0);
            expenses = this.last7Days.map((x) => x.expenses ?? 0);
        } else if (period === 'today') {
            labels = ['Today'];
            sales = [this.todaySales ?? 0];
            purchases = [this.todayPurchases ?? 0];
            expenses = [this.todayExpenses ?? 0];
        } else if (period === 'this-month' && this.cashFlow.length) {
            labels = this.cashFlow.map((x) => x.monthLabel);
            sales = this.cashFlow.map((x) => x.income ?? 0);
            expenses = this.cashFlow.map((x) => x.expense ?? 0);
            purchases = this.cashFlow.map((x) => x.purchases ?? 0);
        } else {
            labels = ['M', 'T', 'W', 'T', 'F', 'S', 'S'];
            sales = [0, 0, 0, 0, 0, 0, 0];
            purchases = [0, 0, 0, 0, 0, 0, 0];
            expenses = [0, 0, 0, 0, 0, 0, 0];
        }

        this.overviewChartData = {
            labels,
            datasets: [
                {
                    label: 'Sales',
                    data: sales,
                    borderColor: [primaryColor],
                    pointBorderColor: 'transparent',
                    pointBackgroundColor: 'transparent',
                    type: 'line',
                    fill: false,
                },
                {
                    label: 'Purchases',
                    data: purchases,
                    backgroundColor: [barColor],
                    hoverBackgroundColor: [primaryColor300],
                    fill: true,
                    borderRadius: 10,
                    borderSkipped: 'top bottom',
                    barPercentage: 0.3
                },
                {
                    label: 'Expenses',
                    data: expenses,
                    backgroundColor: [barColorAlt],
                    hoverBackgroundColor: [primaryColor300],
                    fill: true,
                    borderRadius: 10,
                    borderSkipped: 'top bottom',
                    barPercentage: 0.3
                }
            ]
        };
    }

    changeOverviewWeek() {
        this.buildOverviewChart();
    }

    get colorScheme(): string {
        return this.layoutService.config().colorScheme;
    }

    ngOnDestroy(): void {
        if (this.subscription) {
            this.subscription.unsubscribe();
        }
    }
}
