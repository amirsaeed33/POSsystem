export interface DailyOverviewDto {
    date: string;
    dayLabel: string;
    sales: number;
    purchases: number;
    expenses: number;
    costOfGoodsSold?: number;
    profit?: number;
}

export interface DashboardDto {
    userDisplayName: string;
    userImageUrl?: string;
    totalProducts: number;
    inStockCount: number;
    lowStockCount: number;
    outOfStockCount: number;
    inStockUnits: number;
    lowStockUnits: number;
    lowStockThreshold: number;
    todaySales: number;
    todayPurchases: number;
    todayExpenses: number;
    todayProfit: number;
    isRestaurantBranch?: boolean;
    weeklySales?: number;
    weeklyPurchases?: number;
    weeklyExpenses?: number;
    weeklyProfit?: number;
    averageProfitMargin: number;
    previousAverageProfitMargin: number;
    growthPercentage: number;
    isGrowthPositive: boolean;
    last7Days?: DailyOverviewDto[];
    last15Days?: DailyOverviewDto[];
    cashFlow: MonthlyCashFlowDto[];
    products: DashboardProductRowDto[];
    quickActions: DashboardQuickActionCountsDto;
    latestListTitle: string;
    latestListItems: DashboardLatestListItemDto[];
    timeline: DashboardTimelineEventDto[];
}

export interface DashboardQuickActionCountsDto {
    lowStockCount: number;
    pendingOrdersCount: number;
    todaySalesCount: number;
    branchCount: number;
}

export interface DashboardLatestListItemDto {
    title: string;
    subtitle: string;
    initials: string;
}

export interface DashboardTimelineEventDto {
    type: 'sale' | 'purchase' | 'expense' | 'stock' | string;
    title: string;
    amount: number;
    quantityLabel?: string;
    occurredAt: string;
}

export interface MonthlyCashFlowDto {
    year: number;
    month: number;
    monthLabel: string;
    income: number;
    expense: number;
    purchases: number;
    profit?: number;
}

export interface DashboardProductRowDto {
    id: number;
    name: string;
    sku: string;
    categoryName?: string;
    brandName?: string;
    unitName?: string;
    units: number;
    price: number;
    costPrice: number;
    profitPerUnit: number;
    status: string;
    imagePath?: string;
}
