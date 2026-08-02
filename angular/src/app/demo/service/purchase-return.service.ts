import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import {
    CreatePurchaseReturnDto,
    PagedPurchaseReturnResultRequestDto,
    PagedResultDto,
    PurchaseReturnDto,
    PurchaseReturnLineDto,
    PurchaseReturnableDto,
    PurchaseReturnableLineDto,
} from '../api/purchase-return';
import { environment } from '../../../environments/environment';

@Injectable({
    providedIn: 'root',
})
export class PurchaseReturnService {
    private readonly apiUrl = `${environment.apiUrl}/api/services/app/PurchaseReturn`;

    constructor(private http: HttpClient) {}

    async getAll(
        input?: PagedPurchaseReturnResultRequestDto
    ): Promise<PagedResultDto<PurchaseReturnDto>> {
        const params: any = {};
        if (input?.keyword) {
            params.Keyword = input.keyword;
        }
        if (input?.purchaseId !== undefined && input?.purchaseId !== null) {
            params.PurchaseId = input.purchaseId;
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
        const result = this.unwrap(res, 'Failed to load purchase returns');
        const items = result.items || result.Items || [];
        return {
            items: (Array.isArray(items) ? items : []).map((item) =>
                this.mapReturn(item)
            ),
            totalCount: result.totalCount ?? result.TotalCount ?? items.length,
        };
    }

    async get(id: number): Promise<PurchaseReturnDto> {
        const res: any = await firstValueFrom(
            this.http.get<any>(`${this.apiUrl}/Get`, { params: { Id: id } })
        );
        return this.mapReturn(
            this.unwrap(res, 'Failed to load purchase return')
        );
    }

    async getReturnablePurchase(
        purchaseId: number
    ): Promise<PurchaseReturnableDto> {
        const res: any = await firstValueFrom(
            this.http.get<any>(`${this.apiUrl}/GetReturnablePurchase`, {
                params: { Id: purchaseId },
            })
        );
        return this.mapReturnable(
            this.unwrap(res, 'Failed to load returnable purchase')
        );
    }

    async create(input: CreatePurchaseReturnDto): Promise<PurchaseReturnDto> {
        const res: any = await firstValueFrom(
            this.http.post<any>(`${this.apiUrl}/Create`, {
                purchaseId: input.purchaseId,
                branchId: input.branchId,
                returnDate: this.toApiDate(input.returnDate),
                notes: input.notes,
                lines: (input.lines || []).map((line) => ({
                    purchaseLineId: line.purchaseLineId,
                    quantity: line.quantity,
                })),
            })
        );
        return this.mapReturn(
            this.unwrap(res, 'Failed to create purchase return')
        );
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
        this.unwrap(res, 'Failed to delete purchase return');
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

    private mapReturn(item: any): PurchaseReturnDto {
        const lines = item.lines || item.Lines || [];
        return {
            id: item.id ?? item.Id,
            purchaseId: item.purchaseId ?? item.PurchaseId,
            purchaseInvoiceNo:
                item.purchaseInvoiceNo ?? item.PurchaseInvoiceNo,
            supplierName: item.supplierName ?? item.SupplierName,
            branchId: item.branchId ?? item.BranchId,
            branchName: item.branchName ?? item.BranchName,
            returnDate: item.returnDate ?? item.ReturnDate,
            totalAmount: item.totalAmount ?? item.TotalAmount ?? 0,
            notes: item.notes ?? item.Notes,
            lines: (Array.isArray(lines) ? lines : []).map((line: any) =>
                this.mapLine(line)
            ),
        };
    }

    private mapLine(item: any): PurchaseReturnLineDto {
        return {
            id: item.id ?? item.Id,
            purchaseReturnId: item.purchaseReturnId ?? item.PurchaseReturnId,
            purchaseLineId: item.purchaseLineId ?? item.PurchaseLineId,
            productId: item.productId ?? item.ProductId,
            productName: item.productName ?? item.ProductName,
            quantity: item.quantity ?? item.Quantity ?? 0,
            unitCost: item.unitCost ?? item.UnitCost ?? 0,
            lineTotal: item.lineTotal ?? item.LineTotal ?? 0,
        };
    }

    private mapReturnable(item: any): PurchaseReturnableDto {
        const lines = item.lines || item.Lines || [];
        return {
            id: item.id ?? item.Id,
            supplierId: item.supplierId ?? item.SupplierId,
            supplierName: item.supplierName ?? item.SupplierName,
            invoiceNo: item.invoiceNo ?? item.InvoiceNo,
            purchaseDate: item.purchaseDate ?? item.PurchaseDate,
            lines: (Array.isArray(lines) ? lines : []).map(
                (line: any): PurchaseReturnableLineDto => ({
                    id: line.id ?? line.Id,
                    purchaseLineId: line.purchaseLineId ?? line.PurchaseLineId,
                    productId: line.productId ?? line.ProductId,
                    productName: line.productName ?? line.ProductName,
                    purchasedQuantity:
                        line.purchasedQuantity ?? line.PurchasedQuantity ?? 0,
                    returnedQuantity:
                        line.returnedQuantity ?? line.ReturnedQuantity ?? 0,
                    returnableQuantity:
                        line.returnableQuantity ?? line.ReturnableQuantity ?? 0,
                    unitCost: line.unitCost ?? line.UnitCost ?? 0,
                })
            ),
        };
    }
}
