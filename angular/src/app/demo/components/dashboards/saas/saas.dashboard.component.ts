import {Component, OnDestroy, OnInit} from '@angular/core';
import { Subscription, debounceTime } from 'rxjs';
import { LayoutService } from 'src/app/layout/service/app.layout.service';
import { DashboardService } from 'src/app/demo/service/dashboard.service';
import { PermissionService } from 'src/app/demo/service/permission.service';
import { PermissionNames } from 'src/app/demo/api/permission-names';
import {
    MonthlyCashFlowDto,
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

    subscription: Subscription;

    todaySales = 0;
    todayPurchases = 0;
    todayExpenses = 0;
    todayProfit = 0;

    cashFlow: MonthlyCashFlowDto[] = [];

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
    companyProfileCount = 0;

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
            {name: 'This Week', code: 'this-week'},
            {name: 'Last Week', code: 'last-week'},
            {name: 'This Month', code: 'this-month'}
        ];
        this.selectedOverviewWeek = this.overviewWeeks[2];

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
            this.companyProfileCount = quick?.companyProfileCount ?? 0;

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
            this.companyProfileCount = 0;
            this.latestListTitle = 'Latest Sales';
            this.latestListItems = [];
            this.timelineEvents = [];
        }
        this.buildOverviewChart();
        this.buildRevenueChart();
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
                    borderColor: 'rgba(25, 146, 212, 0.5)',
                    pointBorderColor: 'transparent',
                    pointBackgroundColor: 'transparent',
                    fill: false,
                    tension: .4
                },
                {
                    label: 'Outgoing',
                    data: labels.length ? outgoing : [0],
                    backgroundColor: 'rgba(25, 146, 212, 0.2)',
                    borderColor: 'rgba(25, 146, 212, 0.5)',
                    pointBorderColor: 'transparent',
                    pointBackgroundColor: 'transparent',
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

        if (period === 'this-month' && this.cashFlow.length) {
            // Cash flow is monthly: Income = sales, Expense = expenses (no purchases in API)
            labels = this.cashFlow.map((x) => x.monthLabel);
            sales = this.cashFlow.map((x) => x.income ?? 0);
            expenses = this.cashFlow.map((x) => x.expense ?? 0);
            purchases = this.cashFlow.map((x) => x.purchases ?? 0);
        } else {
            // This Week / Last Week: API has no weekly cash-flow breakdown
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
