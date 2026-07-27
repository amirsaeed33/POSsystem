import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import {
    CreateSaleReturnDto,
    PagedResultDto,
    PagedSaleReturnResultRequestDto,
    SaleReturnDto,
    SaleReturnLineDto,
    SaleReturnableDto,
    SaleReturnableLineDto,
} from '../api/sale-return';
import { environment } from '../../../environments/environment';

@Injectable({
    providedIn: 'root',
})
export class SaleReturnService {
    private readonly apiUrl = `${environment.apiUrl}/api/services/app/SaleReturn`;

    constructor(private http: HttpClient) {}

    async getAll(
        input?: PagedSaleReturnResultRequestDto
    ): Promise<PagedResultDto<SaleReturnDto>> {
        const params: any = {};
        if (input?.keyword) {
            params.Keyword = input.keyword;
        }
        if (input?.saleId !== undefined && input?.saleId !== null) {
            params.SaleId = input.saleId;
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
        const result = this.unwrap(res, 'Failed to load sale returns');
        const items = result.items || result.Items || [];
        return {
            items: (Array.isArray(items) ? items : []).map((item) =>
                this.mapReturn(item)
            ),
            totalCount: result.totalCount ?? result.TotalCount ?? items.length,
        };
    }

    async get(id: number): Promise<SaleReturnDto> {
        const res: any = await firstValueFrom(
            this.http.get<any>(`${this.apiUrl}/Get`, { params: { Id: id } })
        );
        return this.mapReturn(this.unwrap(res, 'Failed to load sale return'));
    }

    async getReturnableSale(saleId: number): Promise<SaleReturnableDto> {
        const res: any = await firstValueFrom(
            this.http.get<any>(`${this.apiUrl}/GetReturnableSale`, {
                params: { Id: saleId },
            })
        );
        return this.mapReturnable(
            this.unwrap(res, 'Failed to load returnable sale')
        );
    }

    async create(input: CreateSaleReturnDto): Promise<SaleReturnDto> {
        const res: any = await firstValueFrom(
            this.http.post<any>(`${this.apiUrl}/Create`, {
                saleId: input.saleId,
                returnDate: this.toApiDate(input.returnDate),
                notes: input.notes,
                lines: (input.lines || []).map((line) => ({
                    saleLineId: line.saleLineId,
                    quantity: line.quantity,
                })),
            })
        );
        return this.mapReturn(this.unwrap(res, 'Failed to create sale return'));
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
        this.unwrap(res, 'Failed to delete sale return');
    }

    private toApiDate(value?: string | Date): string | undefined {
        if (!value) {
            return undefined;
        }
        if (value instanceof Date) {
            const year = value.getFullYear();
            const month = `${value.getMonth() + 1}`.padStart(2, '0');
            const day = `${value.getDate()}`.padStart(2, '0');
            return `${year}-${month}-${day}T00:00:00.000Z`;
        }
        const datePart = value.includes('T') ? value.split('T')[0] : value;
        if (/^\d{4}-\d{2}-\d{2}$/.test(datePart)) {
            return `${datePart}T00:00:00.000Z`;
        }
        return value;
    }

    private unwrap(res: any, fallback: string): any {
        if (!res) {
            throw new Error('No response from server');
        }
        if (res.success === false || res.error) {
            throw new Error(
                res.error?.message || res.error?.details || fallback
            );
        }
        return res.result ?? res;
    }

    private mapReturn(item: any): SaleReturnDto {
        const lines = item.lines || item.Lines || [];
        return {
            id: item.id ?? item.Id,
            saleId: item.saleId ?? item.SaleId,
            saleInvoiceNo: item.saleInvoiceNo ?? item.SaleInvoiceNo,
            customerName: item.customerName ?? item.CustomerName,
            returnDate: item.returnDate ?? item.ReturnDate,
            totalAmount: item.totalAmount ?? item.TotalAmount ?? 0,
            notes: item.notes ?? item.Notes,
            lines: (Array.isArray(lines) ? lines : []).map((line: any) =>
                this.mapLine(line)
            ),
        };
    }

    private mapLine(item: any): SaleReturnLineDto {
        return {
            id: item.id ?? item.Id,
            saleReturnId: item.saleReturnId ?? item.SaleReturnId,
            saleLineId: item.saleLineId ?? item.SaleLineId,
            productId: item.productId ?? item.ProductId,
            productName: item.productName ?? item.ProductName,
            quantity: item.quantity ?? item.Quantity ?? 0,
            unitPrice: item.unitPrice ?? item.UnitPrice ?? 0,
            lineTotal: item.lineTotal ?? item.LineTotal ?? 0,
        };
    }

    private mapReturnable(item: any): SaleReturnableDto {
        const lines = item.lines || item.Lines || [];
        return {
            id: item.id ?? item.Id,
            customerId: item.customerId ?? item.CustomerId,
            customerName: item.customerName ?? item.CustomerName,
            invoiceNo: item.invoiceNo ?? item.InvoiceNo,
            saleDate: item.saleDate ?? item.SaleDate,
            lines: (Array.isArray(lines) ? lines : []).map(
                (line: any): SaleReturnableLineDto => ({
                    id: line.id ?? line.Id,
                    saleLineId: line.saleLineId ?? line.SaleLineId,
                    productId: line.productId ?? line.ProductId,
                    productName: line.productName ?? line.ProductName,
                    soldQuantity: line.soldQuantity ?? line.SoldQuantity ?? 0,
                    returnedQuantity:
                        line.returnedQuantity ?? line.ReturnedQuantity ?? 0,
                    returnableQuantity:
                        line.returnableQuantity ?? line.ReturnableQuantity ?? 0,
                    unitPrice: line.unitPrice ?? line.UnitPrice ?? 0,
                })
            ),
        };
    }
}
