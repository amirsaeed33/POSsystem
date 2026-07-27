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
    averageProfitMargin: number;
    previousAverageProfitMargin: number;
    growthPercentage: number;
    isGrowthPositive: boolean;
    cashFlow: MonthlyCashFlowDto[];
    products: DashboardProductRowDto[];
}

export interface MonthlyCashFlowDto {
    year: number;
    month: number;
    monthLabel: string;
    income: number;
    expense: number;
    purchases: number;
}

export interface DashboardProductRowDto {
    id: number;
    name: string;
    sku: string;
    categoryName?: string;
    brandName?: string;
    units: number;
    price: number;
    costPrice: number;
    profitPerUnit: number;
    status: string;
    imagePath?: string;
}
