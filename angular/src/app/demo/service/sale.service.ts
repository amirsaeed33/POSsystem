import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import {
    CreateSaleDto,
    PagedResultDto,
    PagedSaleResultRequestDto,
    SaleDto,
    SaleLineDto,
} from '../api/sale';
import { ProductDto } from '../api/product';
import { environment } from '../../../environments/environment';

@Injectable({
    providedIn: 'root',
})
export class SaleService {
    private readonly apiUrl = `${environment.apiUrl}/api/services/app/Sale`;

    constructor(private http: HttpClient) {}

    async getAll(
        input?: PagedSaleResultRequestDto
    ): Promise<PagedResultDto<SaleDto>> {
        const params: any = {};
        if (input?.keyword) {
            params.Keyword = input.keyword;
        }
        if (input?.customerId !== undefined) {
            params.CustomerId = input.customerId;
        }
        if (input?.skipCount !== undefined) {
            params.SkipCount = input.skipCount;
        }
        if (input?.maxResultCount !== undefined) {
            params.MaxResultCount = input.maxResultCount;
        }

        const res: any = await firstValueFrom(
            this.http.get<any>(`${this.apiUrl}/GetAll`, { params })
        );
        const result = this.unwrap(res, 'Failed to load sales');
        const items = result.items || result.Items || [];
        const totalCount = result.totalCount ?? result.TotalCount ?? items.length;

        return {
            items: (Array.isArray(items) ? items : []).map((item: any) =>
                this.mapSale(item)
            ),
            totalCount,
        };
    }

    async get(id: number): Promise<SaleDto> {
        const res: any = await firstValueFrom(
            this.http.get<any>(`${this.apiUrl}/Get`, { params: { Id: id } })
        );
        return this.mapSale(this.unwrap(res, 'Failed to load sale'));
    }

    async create(input: CreateSaleDto): Promise<SaleDto> {
        const res: any = await firstValueFrom(
            this.http.post<any>(`${this.apiUrl}/Create`, {
                customerId: input.customerId,
                saleDate: this.toApiDate(input.saleDate),
                invoiceNo: input.invoiceNo,
                notes: input.notes,
                discountAmount: input.discountAmount || 0,
                discountPercent: input.discountPercent || 0,
                taxPercent: input.taxPercent || 0,
                paymentType: input.paymentType,
                cashAmount: input.cashAmount || 0,
                cardAmount: input.cardAmount || 0,
                lines: (input.lines || []).map((line) => ({
                    productId: line.productId,
                    quantity: line.quantity,
                    unitPrice: line.unitPrice,
                })),
            })
        );
        return this.mapSale(this.unwrap(res, 'Failed to create sale'));
    }

    async delete(id: number): Promise<void> {
        const res: any = await firstValueFrom(
            this.http.delete<any>(`${this.apiUrl}/Delete`, {
                params: { Id: id },
            })
        );
        if (res == null) {
            return;
        }
        this.unwrap(res, 'Failed to delete sale');
    }

    async getProductByBarcode(barcode: string): Promise<ProductDto> {
        return this.findPosProduct(barcode);
    }

    async findPosProduct(keyword: string): Promise<ProductDto> {
        try {
            const res: any = await firstValueFrom(
                this.http.get<any>(`${this.apiUrl}/GetPosProduct`, {
                    params: { keyword },
                })
            );
            return this.mapProduct(
                this.unwrap(res, 'Failed to find product')
            );
        } catch (error) {
            throw new Error(
                this.extractErrorMessage(error, 'Product not found')
            );
        }
    }

    async searchPosProducts(keyword: string): Promise<ProductDto[]> {
        try {
            const params: any = {};
            if (keyword?.trim()) {
                params.keyword = keyword.trim();
            }
            const res: any = await firstValueFrom(
                this.http.get<any>(`${this.apiUrl}/GetPosProductSuggestions`, {
                    params,
                })
            );
            const result = this.unwrap(res, 'Failed to search products');
            const items = result.items || result.Items || result || [];
            return (Array.isArray(items) ? items : []).map((item: any) =>
                this.mapProduct(item)
            );
        } catch (error) {
            throw new Error(
                this.extractErrorMessage(error, 'Failed to search products')
            );
        }
    }

    private mapProduct(item: any): ProductDto {
        return {
            id: item.id ?? item.Id,
            name: item.name ?? item.Name,
            description: item.description ?? item.Description,
            barcode: item.barcode ?? item.Barcode,
            price: item.price ?? item.Price ?? 0,
            wholesalePrice: item.wholesalePrice ?? item.WholesalePrice ?? 0,
            costPrice: item.costPrice ?? item.CostPrice ?? 0,
            profitPerUnit: item.profitPerUnit ?? item.ProfitPerUnit ?? 0,
            profitMarginPercent:
                item.profitMarginPercent ?? item.ProfitMarginPercent,
            stockProfit: item.stockProfit ?? item.StockProfit ?? 0,
            stockQuantity: item.stockQuantity ?? item.StockQuantity ?? 0,
            alertQuantityLimit:
                item.alertQuantityLimit ?? item.AlertQuantityLimit ?? 0,
            categoryId: item.categoryId ?? item.CategoryId,
            categoryName: item.categoryName ?? item.CategoryName,
            brandId: item.brandId ?? item.BrandId,
            brandName: item.brandName ?? item.BrandName,
            unitId: item.unitId ?? item.UnitId,
            unitName: item.unitName ?? item.UnitName,
            imagePath: item.imagePath ?? item.ImagePath,
        };
    }

    private extractErrorMessage(error: any, fallbackMessage: string): string {
        const abpError = error?.error;
        return (
            abpError?.error?.message ||
            abpError?.message ||
            abpError?.error?.details ||
            error?.message ||
            fallbackMessage
        );
    }

    private toApiDate(value: string | Date | undefined): string | undefined {
        if (!value) {
            return undefined;
        }
        if (value instanceof Date) {
            return value.toISOString();
        }
        const parsed = new Date(value);
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

    private mapSale(item: any): SaleDto {
        const lines = item.lines || item.Lines || [];
        return {
            id: item.id ?? item.Id,
            customerId: item.customerId ?? item.CustomerId,
            customerName: item.customerName ?? item.CustomerName,
            saleDate: item.saleDate ?? item.SaleDate,
            invoiceNo: item.invoiceNo ?? item.InvoiceNo,
            subTotal: item.subTotal ?? item.SubTotal ?? 0,
            discountAmount: item.discountAmount ?? item.DiscountAmount ?? 0,
            discountPercent: item.discountPercent ?? item.DiscountPercent ?? 0,
            taxPercent: item.taxPercent ?? item.TaxPercent ?? 0,
            taxAmount: item.taxAmount ?? item.TaxAmount ?? 0,
            totalAmount: item.totalAmount ?? item.TotalAmount ?? 0,
            paymentType: item.paymentType ?? item.PaymentType ?? 0,
            cashAmount: item.cashAmount ?? item.CashAmount ?? 0,
            cardAmount: item.cardAmount ?? item.CardAmount ?? 0,
            creditAmount: item.creditAmount ?? item.CreditAmount ?? 0,
            notes: item.notes ?? item.Notes,
            hasReturns: !!(item.hasReturns ?? item.HasReturns),
            returnCount: item.returnCount ?? item.ReturnCount ?? 0,
            lines: (Array.isArray(lines) ? lines : []).map((line: any) =>
                this.mapLine(line)
            ),
        };
    }

    private mapLine(item: any): SaleLineDto {
        return {
            id: item.id ?? item.Id,
            saleId: item.saleId ?? item.SaleId,
            productId: item.productId ?? item.ProductId,
            productName: item.productName ?? item.ProductName,
            quantity: item.quantity ?? item.Quantity ?? 0,
            unitPrice: item.unitPrice ?? item.UnitPrice ?? 0,
            lineTotal: item.lineTotal ?? item.LineTotal ?? 0,
        };
    }
}
