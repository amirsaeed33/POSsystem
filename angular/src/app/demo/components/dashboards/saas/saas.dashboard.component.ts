import { Component, OnDestroy, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { Subscription, debounceTime, interval } from 'rxjs';
import { LayoutService } from 'src/app/layout/service/app.layout.service';
import { DashboardService } from 'src/app/demo/service/dashboard.service';
import { PermissionService } from 'src/app/demo/service/permission.service';
import { PermissionNames } from 'src/app/demo/api/permission-names';
import { BranchContextService } from 'src/app/demo/service/branch-context.service';
import { RestaurantTableService } from 'src/app/demo/service/restaurant-table.service';
import { RestaurantTableDto, TableStatuses } from 'src/app/demo/api/restaurant-table';
import { RestaurantOrderService } from 'src/app/demo/service/restaurant-order.service';
import { RestaurantOrderDto, RestaurantOrderStatuses } from 'src/app/demo/api/restaurant-order';
import {
    MonthlyCashFlowDto,
    DailyOverviewDto,
    DashboardProductRowDto,
    DashboardTimelineEventDto,
    // ONLINE-PIPELINE START
    OnlineOrderPipelineDto,
    // ONLINE-PIPELINE END
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
    unitName?: string;
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
    amount?: number;
    timeAgo: string;
    occurredAt: number;
    icon: string;
    iconClass: string;
    lineClass: string;
    detailBg: string;
    detailBgDark: string;
}

export interface UnpaidBillItem {
    id: number;
    orderNo: string;
    tableOrLocation: string;
    waiterName?: string;
    grandTotal: number;
    billedTime: string | null;
    creationTime: string;
    elapsedMinutes: number;
    elapsedFormatted: string;
    hasValidBilledTime: boolean;
    isOverdue: boolean;
}

@Component({
    templateUrl: './saas.dashboard.component.html',
    styleUrls: ['./saas.dashboard.component.scss']
})
export class SaaSDashboardComponent implements OnInit, OnDestroy {

    overviewChartData: any;

    overviewChartOptions: any;

    overviewWeeks: any;

    selectedOverviewWeek: any;

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

    trendPeriodOptions = [
        { label: 'Hour', value: 'hour' },
        { label: 'Day', value: 'day' },
        { label: 'Month', value: 'month' },
        { label: 'Year', value: 'year' }
    ];

    selectedTrendPeriod: 'hour' | 'day' | 'month' | 'year' = 'day';

    trendChartTypes = [
        { label: 'Line', value: 'line' },
        { label: 'Bar', value: 'bar' }
    ];

    selectedTrendChartType = 'bar';

    get trendChartTitle(): string {
        if (this.selectedTrendPeriod === 'hour') return '24-Hour Financial Trend';
        if (this.selectedTrendPeriod === 'month') return '15-Month Financial Trend';
        if (this.selectedTrendPeriod === 'year') return '15-Year Financial Trend';
        return '15-Day Financial Trend';
    }

    get trendChartSubtitle(): string {
        const periodUnit = this.selectedTrendPeriod === 'hour' ? 'hourly' : this.selectedTrendPeriod === 'month' ? 'monthly' : this.selectedTrendPeriod === 'year' ? 'yearly' : 'daily';
        return this.isRestaurant
            ? `Compare ${periodUnit} Sales, Purchases & Expenses (click legend items to toggle visibility)`
            : `Compare ${periodUnit} Sales, Purchases, Expenses & Profit (click legend items to toggle visibility)`;
    }

    subscription: Subscription;
    private branchSubscription?: Subscription;

    todaySales = 0;
    todayPurchases = 0;
    todayExpenses = 0;
    todayProfit = 0;

    // Animated display counters for first-load numbers
    displayedTodaySales = 0;
    displayedTodayPurchases = 0;
    displayedTodayExpenses = 0;
    displayedTodayProfit = 0;
    private counterAnimationId: number | null = null;

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

    // Visibility toggles for dashboard charts (hidden per user request)
    showSalesOverview = false;
    showMonthlySalesGrowth = false;

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
        { 'background-color': 'rgba(101, 214, 173, 0.1)', 'color': '#27AB83', 'border': '1px solid #65D6AD' },
        { 'background-color': 'rgba(250, 219, 95, 0.1)', 'color': '#DE911D', 'border': '1px solid #FADB5F' },
        { 'background-color': 'rgba(94, 208, 250, 0.1)', 'color': '#1992D4', 'border': '1px solid #5ED0FA' },
        { 'background-color': 'rgba(43, 176, 237, 0.1)', 'color': '#127FBF', 'border': '1px solid #2BB0ED' },
        { 'background-color': 'rgba(255, 155, 155, 0.1)', 'color': '#CF1124', 'border': '1px solid #FF9B9B' },
        { 'background-color': 'rgba(250, 219, 95, 0.1)', 'color': '#DE911D', 'border': '1px solid #FADB5F' },
    ];

    // Restaurant Table Occupancy Widget (Step 2)
    tablesLoading = false;
    tablesError: string | null = null;
    totalTables = 0;
    occupiedTables = 0;
    availableTables = 0;
    billedTables = 0;
    reservedTables = 0;
    occupancyPercentage = 0;
    restaurantTables: RestaurantTableDto[] = [];

    // Restaurant Running Orders Widget state (Step 3)
    ordersLoading = false;
    ordersError: string | null = null;
    openOrdersCount = 0;
    activeRestaurantOrders: RestaurantOrderDto[] = [];

    // Restaurant Longest Waiting Order state (Step 4)
    oldestOrder: RestaurantOrderDto | null = null;
    longestWaitingMinutes = 0;
    longestWaitingFormatted = '';
    private waitingTimerSubscription?: Subscription;

    // ONLINE-PIPELINE START
    onlinePipelineData: OnlineOrderPipelineDto | null = null;
    onlinePipelineLoading = false;
    onlinePipelineError: string | null = null;
    // ONLINE-PIPELINE END

    // Restaurant Unpaid Bills state (Step 5)
    unpaidBills: UnpaidBillItem[] = [];
    unpaidBillsCount = 0;
    unpaidBillsTotalAmount = 0;

    constructor(
        public layoutService: LayoutService,
        private dashboardService: DashboardService,
        private permissionService: PermissionService,
        private branchContext: BranchContextService,
        private restaurantTableService: RestaurantTableService,
        private restaurantOrderService: RestaurantOrderService,
        private router: Router
    ) {
        this.subscription = this.layoutService.configUpdate$
            .pipe(debounceTime(25))
            .subscribe((config) => {
                this.initCharts();
            });

        this.branchSubscription = this.branchContext.currentBranch$.subscribe(() => {
            this.initCharts();
            this.loadDashboard();
        });
    }

    get isRestaurant(): boolean {
        return this.branchContext.isRestaurant();
    }

    canOpenPos(): boolean {
        return this.permissionService.isGranted(PermissionNames.Sales);
    }

    ngOnInit() {
        this.loadUserPreferences();
        this.overviewWeeks = [
            { name: 'Last 7 Days', code: 'last-7-days' },
            { name: 'Today (Day Wise)', code: 'today' },
            { name: 'This Month', code: 'this-month' }
        ];
        this.selectedOverviewWeek = this.overviewWeeks[0];

        this.initCharts();
        this.loadDashboard();
    }

    private loadUserPreferences(): void {
        try {
            const savedPeriod = localStorage.getItem('pos_trend_period');
            if (savedPeriod && ['hour', 'day', 'month', 'year'].includes(savedPeriod)) {
                this.selectedTrendPeriod = savedPeriod as 'hour' | 'day' | 'month' | 'year';
            }

            const savedChartType = localStorage.getItem('pos_trend_chart_type');
            if (savedChartType && ['line', 'bar'].includes(savedChartType)) {
                this.selectedTrendChartType = savedChartType;
            }
        } catch (e) {
            console.warn('Could not read user dashboard preferences', e);
        }
    }

    onTrendPreferenceChange(): void {
        try {
            localStorage.setItem('pos_trend_period', this.selectedTrendPeriod);
            localStorage.setItem('pos_trend_chart_type', this.selectedTrendChartType);
        } catch (e) {
            console.warn('Could not save user dashboard preferences', e);
        }
        this.build15DayTrendChart();
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
        this.loadTableOccupancy();
        this.loadRunningOrders();
        this.loadOnlineOrderPipeline();
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
            this.timelineEvents = [];
        }
        this.animateCounters();
        this.buildOverviewChart();
        this.buildRevenueChart();
        this.build15DayTrendChart();
    }

    animateCounters(): void {
        if (this.counterAnimationId) {
            cancelAnimationFrame(this.counterAnimationId);
            this.counterAnimationId = null;
        }

        const startSales = this.displayedTodaySales;
        const endSales = this.todaySales;
        const startPurchases = this.displayedTodayPurchases;
        const endPurchases = this.todayPurchases;
        const startExpenses = this.displayedTodayExpenses;
        const endExpenses = this.todayExpenses;
        const startProfit = this.displayedTodayProfit;
        const endProfit = this.todayProfit;

        const duration = 1200;
        const startTime = performance.now();

        const step = (currentTime: number) => {
            const elapsed = currentTime - startTime;
            const progress = Math.min(elapsed / duration, 1);
            // Smooth easeOutQuart
            const ease = 1 - Math.pow(1 - progress, 4);

            this.displayedTodaySales = Math.round(startSales + (endSales - startSales) * ease);
            this.displayedTodayPurchases = Math.round(startPurchases + (endPurchases - startPurchases) * ease);
            this.displayedTodayExpenses = Math.round(startExpenses + (endExpenses - startExpenses) * ease);
            this.displayedTodayProfit = Math.round(startProfit + (endProfit - startProfit) * ease);

            if (progress < 1) {
                this.counterAnimationId = requestAnimationFrame(step);
            } else {
                this.displayedTodaySales = endSales;
                this.displayedTodayPurchases = endPurchases;
                this.displayedTodayExpenses = endExpenses;
                this.displayedTodayProfit = endProfit;
                this.counterAnimationId = null;
            }
        };

        this.counterAnimationId = requestAnimationFrame(step);
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
                amount: event.amount ?? 0,
                timeAgo: this.formatTimeAgo(event.occurredAt),
                occurredAt: new Date(event.occurredAt).getTime() || 0,
                ...style,
            };
        });
    }

    private formatTimeAgo(value: string | Date): string {
        if (!value) return '';
        const strVal = typeof value === 'string' ? value : value.toISOString();
        const date = new Date(strVal);
        if (isNaN(date.getTime())) {
            return '';
        }
        const diffMs = Date.now() - date.getTime();
        if (diffMs < 0 && Math.abs(diffMs) < 60000) {
            return 'just now';
        }
        const seconds = Math.floor(diffMs / 1000);
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
                unitName: product.unitName || 'Pcs',
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

    get total15DaysProfit(): number {
        return (this.last15Days || []).reduce((acc, x) => acc + (x.profit ?? ((x.sales ?? 0) - (x.costOfGoodsSold ?? 0) - (x.expenses ?? 0))), 0);
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
            animation: {
                duration: 1100,
                easing: 'easeOutQuart',
                delay: (context: any) => {
                    let delay = 0;
                    if (context.type === 'data' && context.mode === 'default') {
                        delay = context.dataIndex * 40 + context.datasetIndex * 80;
                    }
                    return delay;
                }
            },
            transitions: {
                active: {
                    animation: {
                        duration: 300
                    }
                }
            },
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
            animation: {
                duration: 1300,
                easing: 'easeOutCubic',
                delay: (context: any) => {
                    let delay = 0;
                    if (context.type === 'data' && context.mode === 'default') {
                        delay = context.dataIndex * 35;
                    }
                    return delay;
                }
            },
            transitions: {
                active: {
                    animation: {
                        duration: 300
                    }
                }
            },
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

        let labels: string[] = [];
        let salesValues: number[] = [];
        let purchasesValues: number[] = [];
        let expensesValues: number[] = [];
        let profitValues: number[] = [];

        if (this.selectedTrendPeriod === 'hour') {
            // Aggregate real transaction data into last 24 Hours
            const now = new Date();
            const currentHour = now.getHours();
            const hoursList: string[] = [];
            const hourIndexes = new Map<number, number>();

            for (let i = 23; i >= 0; i--) {
                const h = (currentHour - i + 24) % 24;
                const formatted = (h < 10 ? '0' : '') + h + ':00';
                hoursList.push(formatted);
                hourIndexes.set(h, 23 - i);
            }
            labels = hoursList;

            salesValues = new Array(24).fill(0);
            purchasesValues = new Array(24).fill(0);
            expensesValues = new Array(24).fill(0);

            let trackedSales = 0;
            let trackedPurchases = 0;
            let trackedExpenses = 0;

            if (this.timelineEvents && this.timelineEvents.length) {
                const todayStart = new Date(now.getFullYear(), now.getMonth(), now.getDate()).getTime();
                const todayEnd = todayStart + 86400000;

                for (const event of this.timelineEvents) {
                    if (event.occurredAt >= todayStart && event.occurredAt < todayEnd) {
                        const eventHour = new Date(event.occurredAt).getHours();
                        const idx = hourIndexes.get(eventHour);
                        if (idx !== undefined) {
                            const val = event.amount ?? 0;
                            if (event.type === 'sale') {
                                salesValues[idx] += val;
                                trackedSales += val;
                            } else if (event.type === 'purchase') {
                                purchasesValues[idx] += val;
                                trackedPurchases += val;
                            } else if (event.type === 'expense') {
                                expensesValues[idx] += val;
                                trackedExpenses += val;
                            }
                        }
                    }
                }
            }

            const currentHourIndex = hourIndexes.get(currentHour) ?? 23;
            const untrackedSales = Math.max(0, (this.todaySales || 0) - trackedSales);
            const untrackedPurchases = Math.max(0, (this.todayPurchases || 0) - trackedPurchases);
            const untrackedExpenses = Math.max(0, (this.todayExpenses || 0) - trackedExpenses);

            if (untrackedSales > 0) {
                salesValues[currentHourIndex] += untrackedSales;
            }
            if (untrackedPurchases > 0) {
                purchasesValues[currentHourIndex] += untrackedPurchases;
            }
            if (untrackedExpenses > 0) {
                expensesValues[currentHourIndex] += untrackedExpenses;
            }

            // Calculate today's gross profit from sales (before expenses were deducted on the backend)
            // so subtracting hourly expenses does not double-deduct expenses.
            const todayNetProfit = (this.todayProfit !== undefined && this.todayProfit !== null) ? this.todayProfit : 0;
            const todayGrossProfit = todayNetProfit + (this.todayExpenses || 0);

            const grossProfitRatio = (this.todaySales && this.todaySales > 0)
                ? (todayGrossProfit / this.todaySales)
                : (this.averageProfitMargin > 0 ? this.averageProfitMargin / 100 : 0.2);

            profitValues = salesValues.map((s, i) => {
                const profitFromSales = Math.round(s * grossProfitRatio);
                return Math.max(0, profitFromSales - expensesValues[i]);
            });
        } else if (this.selectedTrendPeriod === 'month') {
            // Aggregate data into last 15 Months using cashFlow + current month
            const months = (this.cashFlow || []).slice(-15);
            labels = months.map(m => m.monthLabel || `${m.month}/${m.year}`);
            salesValues = months.map(m => m.income ?? 0);
            purchasesValues = months.map(m => m.purchases ?? 0);
            expensesValues = months.map(m => m.expense ?? 0);
            // Use backend-computed profit (Sales − COGS − Expenses). Fall back to margin approximation if absent.
            const marginRatio = this.averageProfitMargin > 0 ? (this.averageProfitMargin / 100) : 0.2;
            profitValues = months.map(m => m.profit != null
                ? m.profit
                : Math.round(((m.income ?? 0) * marginRatio) - (m.expense ?? 0)));
        } else if (this.selectedTrendPeriod === 'year') {
            // Aggregate cashFlow data into last 15 Years
            const yearlyMap = new Map<number, { sales: number; purchases: number; expenses: number }>();
            (this.cashFlow || []).forEach(m => {
                const yr = m.year || new Date().getFullYear();
                const curr = yearlyMap.get(yr) || { sales: 0, purchases: 0, expenses: 0 };
                curr.sales += (m.income ?? 0);
                curr.purchases += (m.purchases ?? 0);
                curr.expenses += (m.expense ?? 0);
                yearlyMap.set(yr, curr);
            });

            // Ensure last 15 years are represented
            const currentYear = new Date().getFullYear();
            const yearsList: number[] = [];
            for (let y = currentYear - 14; y <= currentYear; y++) {
                yearsList.push(y);
            }
            labels = yearsList.map(y => y.toString());
            salesValues = yearsList.map(y => yearlyMap.get(y)?.sales ?? 0);
            purchasesValues = yearsList.map(y => yearlyMap.get(y)?.purchases ?? 0);
            expensesValues = yearsList.map(y => yearlyMap.get(y)?.expenses ?? 0);
            // Aggregate per-year profit from cashFlow which already has backend-computed profit per month.
            const yearlyProfitMap = new Map<number, number>();
            (this.cashFlow || []).forEach(m => {
                const yr = m.year || new Date().getFullYear();
                yearlyProfitMap.set(yr, (yearlyProfitMap.get(yr) ?? 0) + (m.profit ?? 0));
            });
            const marginRatio2 = this.averageProfitMargin > 0 ? (this.averageProfitMargin / 100) : 0.2;
            profitValues = yearsList.map(y => {
                const cached = yearlyProfitMap.get(y);
                if (cached != null) return cached;
                // Fallback approximation when no cashFlow entry exists for that year
                return Math.round(((yearlyMap.get(y)?.sales ?? 0) * marginRatio2) - (yearlyMap.get(y)?.expenses ?? 0));
            });
        } else {
            // Default 15 Days
            const dataList: DailyOverviewDto[] = this.last15Days || [];
            labels = dataList.map((x: DailyOverviewDto) => x.dayLabel);
            salesValues = dataList.map((x: DailyOverviewDto) => x.sales ?? 0);
            purchasesValues = dataList.map((x: DailyOverviewDto) => x.purchases ?? 0);
            expensesValues = dataList.map((x: DailyOverviewDto) => x.expenses ?? 0);
            profitValues = dataList.map((x: DailyOverviewDto) => x.profit ?? ((x.sales ?? 0) - (x.costOfGoodsSold ?? 0) - (x.expenses ?? 0)));
        }

        // Create canvas gradients for a rich, modern look
        const canvas = document.createElement('canvas');
        const ctx = canvas.getContext('2d');

        let salesGrad: any = 'rgba(59, 130, 246, 0.15)';
        let purchasesGrad: any = 'rgba(245, 158, 11, 0.15)';
        let expensesGrad: any = 'rgba(239, 68, 68, 0.15)';
        let profitGrad: any = 'rgba(168, 85, 247, 0.15)';

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

            profitGrad = ctx.createLinearGradient(0, 0, 0, 300);
            profitGrad.addColorStop(0, 'rgba(168, 85, 247, 0.35)');
            profitGrad.addColorStop(1, 'rgba(168, 85, 247, 0.01)');
        }

        const isBar = this.selectedTrendChartType === 'bar';

        let hiddenDatasetIndexes: number[] = [];
        try {
            const savedHidden = localStorage.getItem('pos_trend_hidden_datasets');
            if (savedHidden) {
                hiddenDatasetIndexes = JSON.parse(savedHidden) || [];
            }
        } catch (err) {
            console.warn('Could not read hidden dataset preferences', err);
        }

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
                    tension: 0.4,
                    hidden: hiddenDatasetIndexes.includes(0)
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
                    tension: 0.4,
                    hidden: hiddenDatasetIndexes.includes(1)
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
                    tension: 0.4,
                    hidden: hiddenDatasetIndexes.includes(2)
                },
                ...(!this.isRestaurant ? [{
                    label: 'Profit',
                    data: labels.length ? profitValues : [0],
                    borderColor: '#9333ea',
                    backgroundColor: isBar ? '#9333ea' : profitGrad,
                    hoverBackgroundColor: isBar ? '#7e22ce' : undefined,
                    pointBorderColor: '#9333ea',
                    pointBackgroundColor: '#ffffff',
                    pointHoverBackgroundColor: '#9333ea',
                    pointHoverBorderColor: '#ffffff',
                    pointRadius: isBar ? 0 : 4,
                    pointHoverRadius: isBar ? 0 : 7,
                    borderWidth: isBar ? 0 : 3,
                    borderRadius: isBar ? 6 : 0,
                    barPercentage: 0.6,
                    categoryPercentage: 0.7,
                    fill: !isBar,
                    tension: 0.4,
                    hidden: hiddenDatasetIndexes.includes(3)
                }] : [])
            ]
        };

        this.trend15DaysChartOptions = {
            animation: {
                duration: 1200,
                easing: 'easeOutQuart',
                delay: (context: any) => {
                    let delay = 0;
                    if (context.type === 'data' && context.mode === 'default') {
                        delay = context.dataIndex * 30 + context.datasetIndex * 70;
                    }
                    return delay;
                }
            },
            transitions: {
                active: {
                    animation: {
                        duration: 300
                    }
                }
            },
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
                        onClick: (e: any, legendItem: any, legend: any) => {
                            const index = legendItem.datasetIndex;
                            const ci = legend.chart;
                            if (ci.isDatasetVisible(index)) {
                                ci.hide(index);
                                legendItem.hidden = true;
                            } else {
                                ci.show(index);
                                legendItem.hidden = false;
                            }
                            // Save hidden dataset state in localStorage
                            try {
                                const hiddenIndexes: number[] = [];
                                ci.data.datasets.forEach((_: any, i: number) => {
                                    if (!ci.isDatasetVisible(i)) {
                                        hiddenIndexes.push(i);
                                    }
                                });
                                localStorage.setItem('pos_trend_hidden_datasets', JSON.stringify(hiddenIndexes));
                            } catch (err) {
                                console.warn('Could not save dataset visibility', err);
                            }
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

    
    async loadTableOccupancy(): Promise<void> {
        if (!this.isRestaurant) {
            this.restaurantTables = [];
            this.totalTables = 0;
            this.occupiedTables = 0;
            this.availableTables = 0;
            this.billedTables = 0;
            this.reservedTables = 0;
            this.occupancyPercentage = 0;
            this.tablesError = null;
            this.tablesLoading = false;
            return;
        }

        const branchId = this.branchContext.getBranchId();
        if (!branchId) {
            this.tablesLoading = false;
            this.tablesError = 'No branch selected';
            return;
        }

        this.tablesLoading = true;
        this.tablesError = null;

        try {
            const tables = await this.restaurantTableService.getTables(branchId);
            // Display only active tables (isActive !== false)
            const activeTables = (tables || []).filter((t) => t.isActive !== false);

            this.restaurantTables = activeTables;
            this.totalTables = activeTables.length;

            this.occupiedTables = activeTables.filter(
                (t) => t.status === TableStatuses.Occupied || (t.status as any) === 1
            ).length;

            this.availableTables = activeTables.filter(
                (t) => t.status === TableStatuses.Available || (t.status as any) === 0
            ).length;

            this.billedTables = activeTables.filter(
                (t) => t.status === TableStatuses.Billed || (t.status as any) === 2
            ).length;

            this.reservedTables = activeTables.filter(
                (t) => t.status === TableStatuses.Reserved || (t.status as any) === 3
            ).length;

            this.occupancyPercentage =
                this.totalTables > 0
                    ? Math.round((this.occupiedTables / this.totalTables) * 1000) / 10
                    : 0;
        } catch (error: any) {
            console.error('Failed to load table occupancy:', error);
            this.tablesError = error?.message || 'Failed to load table occupancy';
        } finally {
            this.tablesLoading = false;
        }
    }

    getOccupancySeverityClass(): string {
        if (this.occupancyPercentage >= 80) return 'pill-danger';
        if (this.occupancyPercentage >= 50) return 'pill-warning';
        return 'pill-success';
    }

    getOccupancyTextClass(): string {
        if (this.occupancyPercentage >= 80) return 'text-red-600';
        if (this.occupancyPercentage >= 50) return 'text-orange-600';
        return 'text-green-600';
    }

    getProgressBarClass(): string {
        if (this.occupancyPercentage >= 80) return 'occupancy-progress-danger';
        if (this.occupancyPercentage >= 50) return 'occupancy-progress-warning';
        return 'occupancy-progress-success';
    }

    
    refreshRestaurantFloor(): void {
        this.loadTableOccupancy();
        this.loadRunningOrders();
        this.loadOnlineOrderPipeline();
    }

    async loadRunningOrders(): Promise<void> {
        if (!this.isRestaurant) {
            this.activeRestaurantOrders = [];
            this.openOrdersCount = 0;
            this.oldestOrder = null;
            this.longestWaitingMinutes = 0;
            this.longestWaitingFormatted = '';
            this.unpaidBills = [];
            this.unpaidBillsCount = 0;
            this.unpaidBillsTotalAmount = 0;
            this.stopWaitingTimer();
            this.ordersError = null;
            this.ordersLoading = false;
            return;
        }

        const branchId = this.branchContext.getBranchId();
        if (!branchId) {
            this.ordersLoading = false;
            this.ordersError = 'No branch selected';
            return;
        }

        this.ordersLoading = true;
        this.ordersError = null;
        this.stopWaitingTimer();

        try {
            const orders = await this.restaurantOrderService.getActiveOrders(branchId);

            // Deduplicate by order.id to avoid counting the same order more than once
            const uniqueMap = new Map<number, RestaurantOrderDto>();
            for (const order of orders || []) {
                if (order && order.id && !uniqueMap.has(order.id)) {
                    uniqueMap.set(order.id, order);
                }
            }
            const uniqueOrders = Array.from(uniqueMap.values());
            this.activeRestaurantOrders = uniqueOrders;

            // 1. OPEN ORDERS: Total number of strictly open orders (excluding Billed orders)
            this.openOrdersCount = uniqueOrders.filter(
                (order) =>
                    order.status === RestaurantOrderStatuses.Open ||
                    (order.status as any) === 0
            ).length;

            // Step 4: Evaluate Longest Waiting Order
            this.evaluateOldestWaitingOrder();

            // Step 5: Evaluate Unpaid Bills
            this.evaluateUnpaidBills();

            // Manage 60s live timer for Step 4 & Step 5
            this.checkAndManageTimer();



        } catch (error: any) {
            console.error('Failed to load active restaurant orders:', error);
            this.ordersError = error?.message || 'Failed to load active orders';
        } finally {
            this.ordersLoading = false;
        }
    }

    
    evaluateOldestWaitingOrder(): void {
        // Filter strictly open orders (Status = 0) with valid CreationTime
        const openOrders = (this.activeRestaurantOrders || []).filter((order) => {
            const isOpen =
                order.status === RestaurantOrderStatuses.Open ||
                (order.status as any) === 0;
            if (!isOpen) {
                return false;
            }
            if (!order.creationTime) {
                return false;
            }
            const timeMs = new Date(order.creationTime).getTime();
            return !isNaN(timeMs) && timeMs > 0;
        });

        if (openOrders.length === 0) {
            this.oldestOrder = null;
            this.longestWaitingMinutes = 0;
            this.longestWaitingFormatted = '';
            return;
        }

        // Sort ascending by creation time to find the earliest (oldest)
        openOrders.sort(
            (a, b) =>
                new Date(a.creationTime).getTime() -
                new Date(b.creationTime).getTime()
        );

        this.oldestOrder = openOrders[0];
        this.calculateWaitingDuration();
    }

    calculateWaitingDuration(): void {
        if (!this.oldestOrder || !this.oldestOrder.creationTime) {
            this.longestWaitingMinutes = 0;
            this.longestWaitingFormatted = '';
            return;
        }

        const createdMs = new Date(this.oldestOrder.creationTime).getTime();
        const nowMs = Date.now();
        const diffMinutes = Math.max(0, Math.floor((nowMs - createdMs) / 60000));
        this.longestWaitingMinutes = diffMinutes;

        if (diffMinutes < 60) {
            this.longestWaitingFormatted = `${diffMinutes} mins`;
        } else {
            const hours = Math.floor(diffMinutes / 60);
            const mins = diffMinutes % 60;
            this.longestWaitingFormatted = `${hours}h ${mins}m`;
        }
    }

    evaluateUnpaidBills(): void {
        const billedOrders = (this.activeRestaurantOrders || []).filter((order) => {
            const isBilled =
                order.status === RestaurantOrderStatuses.Billed ||
                (order.status as any) === 1;
            return isBilled;
        });

        // Sort ascending by billedTime (earliest printed bill first)
        // Orders with missing BilledTime are sorted by CreationTime as secondary sort
        billedOrders.sort((a, b) => {
            const timeA = a.billedTime
                ? new Date(a.billedTime).getTime()
                : (a.creationTime ? new Date(a.creationTime).getTime() : 0);
            const timeB = b.billedTime
                ? new Date(b.billedTime).getTime()
                : (b.creationTime ? new Date(b.creationTime).getTime() : 0);
            return timeA - timeB;
        });

        const nowMs = Date.now();
        let totalAmount = 0;

        this.unpaidBills = billedOrders.map((order) => {
            const grandTotal = Number(order.grandTotal) || 0;
            totalAmount += grandTotal;

            const timeStr = order.billedTime;
            const timeMs = timeStr ? new Date(timeStr).getTime() : NaN;
            const hasValidBilledTime = !isNaN(timeMs) && timeMs > 0;
            const elapsedMinutes = hasValidBilledTime
                ? Math.max(0, Math.floor((nowMs - timeMs) / 60000))
                : 0;

            return {
                id: order.id,
                orderNo: order.orderNo,
                tableOrLocation: this.getOrderLocationLabel(order),
                waiterName: order.waiterName,
                grandTotal,
                billedTime: order.billedTime || null,
                creationTime: order.creationTime,
                elapsedMinutes,
                elapsedFormatted: hasValidBilledTime
                    ? this.formatBillElapsed(elapsedMinutes)
                    : 'Time N/A',
                hasValidBilledTime,
                isOverdue: hasValidBilledTime && elapsedMinutes >= 20,
            };
        });

        this.unpaidBillsCount = this.unpaidBills.length;
        this.unpaidBillsTotalAmount = totalAmount;
    }

    recalculateUnpaidBillDurations(): void {
        if (!this.unpaidBills || this.unpaidBills.length === 0) {
            return;
        }
        const nowMs = Date.now();
        for (const bill of this.unpaidBills) {
            if (!bill.hasValidBilledTime || !bill.billedTime) {
                continue;
            }
            const timeMs = new Date(bill.billedTime).getTime();
            const elapsedMinutes =
                !isNaN(timeMs) && timeMs > 0
                    ? Math.max(0, Math.floor((nowMs - timeMs) / 60000))
                    : 0;
            bill.elapsedMinutes = elapsedMinutes;
            bill.elapsedFormatted = this.formatBillElapsed(elapsedMinutes);
            bill.isOverdue = elapsedMinutes >= 20;
        }
    }

    formatBillElapsed(diffMinutes: number): string {
        if (diffMinutes < 60) {
            return `${diffMinutes} mins`;
        }
        const hours = Math.floor(diffMinutes / 60);
        const mins = diffMinutes % 60;
        return `${hours}h ${mins}m`;
    }

    getOrderLocationLabel(order: RestaurantOrderDto): string {
        if (order.tableNo) {
            return `Table ${order.tableNo}`;
        }
        if (order.carName || order.carNumber) {
            return [order.carName, order.carNumber].filter(Boolean).join(' - ');
        }
        return order.orderTypeName || 'Order';
    }

    checkAndManageTimer(): void {
        const needsTimer =
            !!this.oldestOrder ||
            (this.unpaidBills && this.unpaidBills.length > 0);
        if (needsTimer) {
            if (!this.waitingTimerSubscription) {
                this.startWaitingTimer();
            }
        } else {
            this.stopWaitingTimer();
        }
    }

    startWaitingTimer(): void {
        this.stopWaitingTimer();
        this.waitingTimerSubscription = interval(60000).subscribe(() => {
            if (this.oldestOrder) {
                this.calculateWaitingDuration();
            }
            if (this.unpaidBills && this.unpaidBills.length > 0) {
                this.recalculateUnpaidBillDurations();
            }
        });
    }

    stopWaitingTimer(): void {
        if (this.waitingTimerSubscription) {
            this.waitingTimerSubscription.unsubscribe();
            this.waitingTimerSubscription = undefined;
        }
    }

    getWaitingSeverity(): 'normal' | 'warning' | 'critical' {
        if (this.longestWaitingMinutes >= 30) return 'critical';
        if (this.longestWaitingMinutes >= 20) return 'warning';
        return 'normal';
    }

    getWaitingStatusLabel(): string {
        const severity = this.getWaitingSeverity();
        if (severity === 'critical') return 'Critical Delay';
        if (severity === 'warning') return 'Attention Needed';
        return 'Normal Pace';
    }

    // ONLINE-PIPELINE START
    async loadOnlineOrderPipeline(): Promise<void> {
        this.onlinePipelineLoading = true;
        this.onlinePipelineError = null;
        try {
            const branchId = this.branchContext.getBranchId() ?? undefined;
            this.onlinePipelineData = await this.dashboardService.getOnlineOrderPipeline(branchId);
        } catch (error: any) {
            console.error('Failed to load online order pipeline', error);
            this.onlinePipelineError = error?.message || 'Failed to load online orders';
        } finally {
            this.onlinePipelineLoading = false;
        }
    }

    navigateToOnlineOrders(status: number): void {
        this.router.navigate(['/customer-orders'], { queryParams: { status } });
    }

    navigateToAllOnlineOrders(): void {
        this.router.navigate(['/customer-orders']);
    }
    // ONLINE-PIPELINE END

    ngOnDestroy(): void {
        this.stopWaitingTimer();
        if (this.subscription) {
            this.subscription.unsubscribe();
        }
        if (this.branchSubscription) {
            this.branchSubscription.unsubscribe();
        }
        if (this.counterAnimationId) {
            cancelAnimationFrame(this.counterAnimationId);
            this.counterAnimationId = null;
        }
    }
}
