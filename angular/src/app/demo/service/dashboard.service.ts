import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import {
    DashboardDto,
    DashboardLatestListItemDto,
    DashboardProductRowDto,
    DashboardQuickActionCountsDto,
    DashboardTimelineEventDto,
    MonthlyCashFlowDto,
} from '../api/dashboard';
import { environment } from '../../../environments/environment';

@Injectable({
    providedIn: 'root',
})
export class DashboardService {
    private readonly apiUrl = `${environment.apiUrl}/api/services/app/Dashboard`;

    constructor(private http: HttpClient) {}

    async get(): Promise<DashboardDto> {
        const res: any = await firstValueFrom(
            this.http.get<any>(`${this.apiUrl}/Get`)
        );
        return this.mapDashboard(
            this.unwrap(res, 'Failed to load dashboard')
        );
    }

    private unwrap(res: any, fallbackMessage: string): any {
        if (!res) {
            throw new Error('No response from server');
        }
        if (res.success === false || res.error) {
            throw new Error(
                res.error?.message || res.error?.details || fallbackMessage
            );
        }
        return res.result ?? res;
    }

    private mapDashboard(data: any): DashboardDto {
        const cashFlow = data.cashFlow || data.CashFlow || [];
        const products = data.products || data.Products || [];
        const quickActions =
            data.quickActions || data.QuickActions || {};
        const latestListItems =
            data.latestListItems || data.LatestListItems || [];
        const timeline = data.timeline || data.Timeline || [];

        return {
            userDisplayName:
                data.userDisplayName ?? data.UserDisplayName ?? '',
            userImageUrl: data.userImageUrl ?? data.UserImageUrl,
            totalProducts: data.totalProducts ?? data.TotalProducts ?? 0,
            inStockCount: data.inStockCount ?? data.InStockCount ?? 0,
            lowStockCount: data.lowStockCount ?? data.LowStockCount ?? 0,
            outOfStockCount:
                data.outOfStockCount ?? data.OutOfStockCount ?? 0,
            inStockUnits: data.inStockUnits ?? data.InStockUnits ?? 0,
            lowStockUnits: data.lowStockUnits ?? data.LowStockUnits ?? 0,
            lowStockThreshold:
                data.lowStockThreshold ?? data.LowStockThreshold ?? 10,
            todaySales: data.todaySales ?? data.TodaySales ?? 0,
            todayPurchases: data.todayPurchases ?? data.TodayPurchases ?? 0,
            todayExpenses: data.todayExpenses ?? data.TodayExpenses ?? 0,
            todayProfit: data.todayProfit ?? data.TodayProfit ?? 0,
            averageProfitMargin:
                data.averageProfitMargin ?? data.AverageProfitMargin ?? 0,
            previousAverageProfitMargin:
                data.previousAverageProfitMargin ??
                data.PreviousAverageProfitMargin ??
                0,
            growthPercentage:
                data.growthPercentage ?? data.GrowthPercentage ?? 0,
            isGrowthPositive:
                data.isGrowthPositive ?? data.IsGrowthPositive ?? true,
            last7Days: (Array.isArray(data.last7Days || data.Last7Days) ? (data.last7Days || data.Last7Days) : []).map(
                (item: any) => ({
                    date: item.date ?? item.Date ?? '',
                    dayLabel: item.dayLabel ?? item.DayLabel ?? '',
                    sales: item.sales ?? item.Sales ?? 0,
                    purchases: item.purchases ?? item.Purchases ?? 0,
                    expenses: item.expenses ?? item.Expenses ?? 0,
                })
            ),
            last15Days: (Array.isArray(data.last15Days || data.Last15Days) ? (data.last15Days || data.Last15Days) : []).map(
                (item: any) => ({
                    date: item.date ?? item.Date ?? '',
                    dayLabel: item.dayLabel ?? item.DayLabel ?? '',
                    sales: item.sales ?? item.Sales ?? 0,
                    purchases: item.purchases ?? item.Purchases ?? 0,
                    expenses: item.expenses ?? item.Expenses ?? 0,
                })
            ),
            cashFlow: (Array.isArray(cashFlow) ? cashFlow : []).map(
                (item: any): MonthlyCashFlowDto => ({
                    year: item.year ?? item.Year ?? 0,
                    month: item.month ?? item.Month ?? 0,
                    monthLabel: item.monthLabel ?? item.MonthLabel ?? '',
                    income: item.income ?? item.Income ?? 0,
                    expense: item.expense ?? item.Expense ?? 0,
                    purchases: item.purchases ?? item.Purchases ?? 0,
                })
            ),
            products: (Array.isArray(products) ? products : []).map(
                (item: any): DashboardProductRowDto => ({
                    id: item.id ?? item.Id,
                    name: item.name ?? item.Name ?? '',
                    sku: item.sku ?? item.Sku ?? '',
                    categoryName: item.categoryName ?? item.CategoryName,
                    brandName: item.brandName ?? item.BrandName,
                    units: item.units ?? item.Units ?? 0,
                    price: item.price ?? item.Price ?? 0,
                    costPrice: item.costPrice ?? item.CostPrice ?? 0,
                    profitPerUnit:
                        item.profitPerUnit ?? item.ProfitPerUnit ?? 0,
                    status: item.status ?? item.Status ?? '',
                    imagePath: item.imagePath ?? item.ImagePath,
                })
            ),
            quickActions: this.mapQuickActions(quickActions),
            latestListTitle:
                data.latestListTitle ?? data.LatestListTitle ?? 'Latest Sales',
            latestListItems: (Array.isArray(latestListItems)
                ? latestListItems
                : []
            ).map(
                (item: any): DashboardLatestListItemDto => ({
                    title: item.title ?? item.Title ?? '',
                    subtitle: item.subtitle ?? item.Subtitle ?? '',
                    initials: item.initials ?? item.Initials ?? '?',
                })
            ),
            timeline: (Array.isArray(timeline) ? timeline : []).map(
                (item: any): DashboardTimelineEventDto => ({
                    type: (item.type ?? item.Type ?? '').toLowerCase(),
                    title: item.title ?? item.Title ?? '',
                    amount: item.amount ?? item.Amount ?? 0,
                    quantityLabel:
                        item.quantityLabel ?? item.QuantityLabel,
                    occurredAt: item.occurredAt ?? item.OccurredAt,
                })
            ),
        };
    }

    private mapQuickActions(data: any): DashboardQuickActionCountsDto {
        return {
            lowStockCount: data.lowStockCount ?? data.LowStockCount ?? 0,
            pendingOrdersCount:
                data.pendingOrdersCount ?? data.PendingOrdersCount ?? 0,
            todaySalesCount:
                data.todaySalesCount ?? data.TodaySalesCount ?? 0,
            branchCount:
                data.branchCount ?? data.BranchCount ?? 0,
        };
    }
}
