import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import {
    ExpenseReportDto,
    ExpenseReportRowDto,
    PurchaseReportDto,
    PurchaseReportRowDto,
    ReportDateRangeInput,
    SaleReportDto,
    SaleReportRowDto,
    StockReportDto,
    StockReportRowDto,
    ProductProfitReportDto,
    ProductProfitReportRowDto,
} from '../api/report';
import { environment } from '../../../environments/environment';

@Injectable({
    providedIn: 'root',
})
export class ReportService {
    private readonly apiUrl = `${environment.apiUrl}/api/services/app/Report`;

    constructor(private http: HttpClient) {}

    async getSaleReport(input?: ReportDateRangeInput): Promise<SaleReportDto> {
        const res: any = await firstValueFrom(
            this.http.get<any>(`${this.apiUrl}/GetSaleReport`, {
                params: this.toParams(input),
            })
        );
        return this.mapSaleReport(
            this.unwrap(res, 'Failed to load sale report')
        );
    }

    async getPurchaseReport(
        input?: ReportDateRangeInput
    ): Promise<PurchaseReportDto> {
        const res: any = await firstValueFrom(
            this.http.get<any>(`${this.apiUrl}/GetPurchaseReport`, {
                params: this.toParams(input),
            })
        );
        return this.mapPurchaseReport(
            this.unwrap(res, 'Failed to load purchase report')
        );
    }

    async getExpenseReport(
        input?: ReportDateRangeInput
    ): Promise<ExpenseReportDto> {
        const res: any = await firstValueFrom(
            this.http.get<any>(`${this.apiUrl}/GetExpenseReport`, {
                params: this.toParams(input),
            })
        );
        return this.mapExpenseReport(
            this.unwrap(res, 'Failed to load expense report')
        );
    }

    async getStockReport(keyword?: string): Promise<StockReportDto> {
        const params: any = {};
        if (keyword) {
            params.Keyword = keyword;
        }
        const res: any = await firstValueFrom(
            this.http.get<any>(`${this.apiUrl}/GetStockReport`, { params })
        );
        return this.mapStockReport(
            this.unwrap(res, 'Failed to load stock report')
        );
    }

    async getProductProfitReport(
        input?: ReportDateRangeInput
    ): Promise<ProductProfitReportDto> {
        const res: any = await firstValueFrom(
            this.http.get<any>(`${this.apiUrl}/GetProductProfitReport`, {
                params: this.toParams(input),
            })
        );
        return this.mapProductProfitReport(
            this.unwrap(res, 'Failed to load product profit report')
        );
    }

    private toParams(input?: ReportDateRangeInput): any {
        const params: any = {};
        const fromDate = this.toApiDate(input?.fromDate);
        const toDate = this.toApiDate(input?.toDate);
        if (fromDate) {
            params.FromDate = fromDate;
        }
        if (toDate) {
            params.ToDate = toDate;
        }
        if (input?.keyword) {
            params.Keyword = input.keyword;
        }
        return params;
    }

    private toApiDate(value?: string | Date): string | undefined {
        if (!value) {
            return undefined;
        }
        if (value instanceof Date) {
            return value.toISOString();
        }
        const parsed = new Date(
            value.includes('T') ? value : `${value}T00:00:00`
        );
        if (!isNaN(parsed.getTime())) {
            return parsed.toISOString();
        }
        return value;
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

    private mapSaleReport(data: any): SaleReportDto {
        const items = data.items || data.Items || [];
        return {
            totalAmount: data.totalAmount ?? data.TotalAmount ?? 0,
            items: (Array.isArray(items) ? items : []).map(
                (item: any): SaleReportRowDto => ({
                    id: item.id ?? item.Id,
                    invoiceNo: item.invoiceNo ?? item.InvoiceNo,
                    saleDate: item.saleDate ?? item.SaleDate,
                    customerName: item.customerName ?? item.CustomerName,
                    totalAmount: item.totalAmount ?? item.TotalAmount ?? 0,
                    notes: item.notes ?? item.Notes,
                })
            ),
        };
    }

    private mapPurchaseReport(data: any): PurchaseReportDto {
        const items = data.items || data.Items || [];
        return {
            totalAmount: data.totalAmount ?? data.TotalAmount ?? 0,
            items: (Array.isArray(items) ? items : []).map(
                (item: any): PurchaseReportRowDto => ({
                    id: item.id ?? item.Id,
                    invoiceNo: item.invoiceNo ?? item.InvoiceNo,
                    purchaseDate: item.purchaseDate ?? item.PurchaseDate,
                    supplierName: item.supplierName ?? item.SupplierName,
                    totalAmount: item.totalAmount ?? item.TotalAmount ?? 0,
                    notes: item.notes ?? item.Notes,
                })
            ),
        };
    }

    private mapExpenseReport(data: any): ExpenseReportDto {
        const items = data.items || data.Items || [];
        return {
            totalAmount: data.totalAmount ?? data.TotalAmount ?? 0,
            items: (Array.isArray(items) ? items : []).map(
                (item: any): ExpenseReportRowDto => ({
                    id: item.id ?? item.Id,
                    expenseDate: item.expenseDate ?? item.ExpenseDate,
                    referenceNo: item.referenceNo ?? item.ReferenceNo,
                    description: item.description ?? item.Description,
                    paymentAccountName:
                        item.paymentAccountName ?? item.PaymentAccountName,
                    amount: item.amount ?? item.Amount ?? 0,
                })
            ),
        };
    }

    private mapStockReport(data: any): StockReportDto {
        const items = data.items || data.Items || [];
        return {
            totalProducts: data.totalProducts ?? data.TotalProducts ?? 0,
            inStockCount: data.inStockCount ?? data.InStockCount ?? 0,
            lowStockCount: data.lowStockCount ?? data.LowStockCount ?? 0,
            outOfStockCount: data.outOfStockCount ?? data.OutOfStockCount ?? 0,
            totalStockUnits: data.totalStockUnits ?? data.TotalStockUnits ?? 0,
            totalStockCostValue:
                data.totalStockCostValue ?? data.TotalStockCostValue ?? 0,
            totalStockSellValue:
                data.totalStockSellValue ?? data.TotalStockSellValue ?? 0,
            totalStockProfit:
                data.totalStockProfit ?? data.TotalStockProfit ?? 0,
            items: (Array.isArray(items) ? items : []).map(
                (item: any): StockReportRowDto => ({
                    id: item.id ?? item.Id,
                    name: item.name ?? item.Name,
                    barcode: item.barcode ?? item.Barcode,
                    categoryName: item.categoryName ?? item.CategoryName,
                    brandName: item.brandName ?? item.BrandName,
                    unitName: item.unitName ?? item.UnitName,
                    price: item.price ?? item.Price ?? 0,
                    costPrice: item.costPrice ?? item.CostPrice ?? 0,
                    profitPerUnit: item.profitPerUnit ?? item.ProfitPerUnit ?? 0,
                    profitMarginPercent:
                        item.profitMarginPercent ?? item.ProfitMarginPercent,
                    stockProfit: item.stockProfit ?? item.StockProfit ?? 0,
                    stockQuantity: item.stockQuantity ?? item.StockQuantity ?? 0,
                    alertQuantityLimit:
                        item.alertQuantityLimit ?? item.AlertQuantityLimit ?? 0,
                    status: item.status ?? item.Status,
                })
            ),
        };
    }

    private mapProductProfitReport(data: any): ProductProfitReportDto {
        const items = data.items || data.Items || [];
        return {
            totalProductsSold:
                data.totalProductsSold ?? data.TotalProductsSold ?? 0,
            totalQuantitySold:
                data.totalQuantitySold ?? data.TotalQuantitySold ?? 0,
            totalCost: data.totalCost ?? data.TotalCost ?? 0,
            totalRevenue: data.totalRevenue ?? data.TotalRevenue ?? 0,
            totalProfit: data.totalProfit ?? data.TotalProfit ?? 0,
            averageProfitMarginPercent:
                data.averageProfitMarginPercent ??
                data.AverageProfitMarginPercent,
            items: (Array.isArray(items) ? items : []).map(
                (item: any): ProductProfitReportRowDto => ({
                    id: item.id ?? item.Id,
                    name: item.name ?? item.Name,
                    barcode: item.barcode ?? item.Barcode,
                    categoryName: item.categoryName ?? item.CategoryName,
                    unitName: item.unitName ?? item.UnitName,
                    quantitySold: item.quantitySold ?? item.QuantitySold ?? 0,
                    costPrice: item.costPrice ?? item.CostPrice ?? 0,
                    sellingPrice: item.sellingPrice ?? item.SellingPrice ?? 0,
                    profitPerUnit: item.profitPerUnit ?? item.ProfitPerUnit ?? 0,
                    totalCost: item.totalCost ?? item.TotalCost ?? 0,
                    totalRevenue: item.totalRevenue ?? item.TotalRevenue ?? 0,
                    totalProfit: item.totalProfit ?? item.TotalProfit ?? 0,
                    profitMarginPercent:
                        item.profitMarginPercent ?? item.ProfitMarginPercent,
                })
            ),
        };
    }
}
