import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import {
    CreatePurchaseDto,
    PagedPurchaseResultRequestDto,
    PagedResultDto,
    PurchaseDto,
    PurchaseLineDto,
} from '../api/purchase';
import { environment } from '../../../environments/environment';

@Injectable({
    providedIn: 'root',
})
export class PurchaseService {
    private readonly apiUrl = `${environment.apiUrl}/api/services/app/Purchase`;

    constructor(private http: HttpClient) {}

    async getAll(
        input?: PagedPurchaseResultRequestDto
    ): Promise<PagedResultDto<PurchaseDto>> {
        const params: any = {};
        if (input?.keyword) {
            params.Keyword = input.keyword;
        }
        if (input?.supplierId !== undefined) {
            params.SupplierId = input.supplierId;
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
        const result = this.unwrap(res, 'Failed to load purchases');
        const items = result.items || result.Items || [];
        const totalCount = result.totalCount ?? result.TotalCount ?? items.length;

        return {
            items: (Array.isArray(items) ? items : []).map((item: any) =>
                this.mapPurchase(item)
            ),
            totalCount,
        };
    }

    async get(id: number): Promise<PurchaseDto> {
        const res: any = await firstValueFrom(
            this.http.get<any>(`${this.apiUrl}/Get`, { params: { Id: id } })
        );
        return this.mapPurchase(this.unwrap(res, 'Failed to load purchase'));
    }

    async create(input: CreatePurchaseDto): Promise<PurchaseDto> {
        const res: any = await firstValueFrom(
            this.http.post<any>(`${this.apiUrl}/Create`, {
                supplierId: input.supplierId,
                branchId: input.branchId,
                purchaseDate: this.toApiDate(input.purchaseDate),
                invoiceNo: input.invoiceNo,
                notes: input.notes,
                lines: (input.lines || []).map((line) => ({
                    productId: line.productId,
                    quantity: line.quantity,
                    unitCost: line.unitCost,
                })),
            })
        );
        return this.mapPurchase(this.unwrap(res, 'Failed to create purchase'));
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
        this.unwrap(res, 'Failed to delete purchase');
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

    private mapPurchase(item: any): PurchaseDto {
        const lines = item.lines || item.Lines || [];
        return {
            id: item.id ?? item.Id,
            supplierId: item.supplierId ?? item.SupplierId,
            supplierName: item.supplierName ?? item.SupplierName,
            branchId: item.branchId ?? item.BranchId,
            branchName: item.branchName ?? item.BranchName,
            purchaseDate: item.purchaseDate ?? item.PurchaseDate,
            invoiceNo: item.invoiceNo ?? item.InvoiceNo,
            totalAmount: item.totalAmount ?? item.TotalAmount ?? 0,
            notes: item.notes ?? item.Notes,
            lines: (Array.isArray(lines) ? lines : []).map((line: any) =>
                this.mapLine(line)
            ),
        };
    }

    private mapLine(item: any): PurchaseLineDto {
        return {
            id: item.id ?? item.Id,
            purchaseId: item.purchaseId ?? item.PurchaseId,
            productId: item.productId ?? item.ProductId,
            productName: item.productName ?? item.ProductName,
            quantity: item.quantity ?? item.Quantity ?? 0,
            unitCost: item.unitCost ?? item.UnitCost ?? 0,
            lineTotal: item.lineTotal ?? item.LineTotal ?? 0,
        };
    }
}
