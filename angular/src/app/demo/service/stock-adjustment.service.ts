import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import {
    CreateStockAdjustmentDto,
    PagedResultDto,
    PagedStockAdjustmentResultRequestDto,
    StockAdjustmentDto,
    StockAdjustmentLineDto,
} from '../api/stock-adjustment';
import { environment } from '../../../environments/environment';

@Injectable({
    providedIn: 'root',
})
export class StockAdjustmentService {
    private readonly apiUrl = `${environment.apiUrl}/api/services/app/StockAdjustment`;

    constructor(private http: HttpClient) {}

    async getAll(
        input?: PagedStockAdjustmentResultRequestDto
    ): Promise<PagedResultDto<StockAdjustmentDto>> {
        const params: any = {};
        if (input?.keyword) {
            params.Keyword = input.keyword;
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
        const result = this.unwrap(res, 'Failed to load stock adjustments');
        const items = result.items || result.Items || [];
        const totalCount = result.totalCount ?? result.TotalCount ?? items.length;

        return {
            items: (Array.isArray(items) ? items : []).map((item: any) =>
                this.mapAdjustment(item)
            ),
            totalCount,
        };
    }

    async get(id: number): Promise<StockAdjustmentDto> {
        const res: any = await firstValueFrom(
            this.http.get<any>(`${this.apiUrl}/Get`, { params: { Id: id } })
        );
        return this.mapAdjustment(
            this.unwrap(res, 'Failed to load stock adjustment')
        );
    }

    async create(input: CreateStockAdjustmentDto): Promise<StockAdjustmentDto> {
        const res: any = await firstValueFrom(
            this.http.post<any>(`${this.apiUrl}/Create`, {
                adjustmentDate: this.toApiDate(input.adjustmentDate),
                reason: input.reason,
                notes: input.notes,
                lines: (input.lines || []).map((line) => ({
                    productId: line.productId,
                    quantityChange: line.quantityChange,
                })),
            })
        );
        return this.mapAdjustment(
            this.unwrap(res, 'Failed to create stock adjustment')
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
        this.unwrap(res, 'Failed to delete stock adjustment');
    }

    /**
     * Backend has no Update endpoint. Edit is implemented as create-then-delete
     * using existing Create/Delete APIs so stock ends up with the new deltas.
     */
    async replace(
        id: number,
        input: CreateStockAdjustmentDto
    ): Promise<StockAdjustmentDto> {
        const created = await this.create(input);
        try {
            await this.delete(id);
        } catch (error) {
            throw new Error(
                (error as any)?.message ||
                    'Updated adjustment was created, but the old one could not be deleted. Please delete it manually.'
            );
        }
        return created;
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

    private mapAdjustment(item: any): StockAdjustmentDto {
        const lines = item.lines || item.Lines || [];
        return {
            id: item.id ?? item.Id,
            adjustmentDate: item.adjustmentDate ?? item.AdjustmentDate,
            reason: item.reason ?? item.Reason ?? 4,
            referenceNo: item.referenceNo ?? item.ReferenceNo,
            notes: item.notes ?? item.Notes,
            lines: (Array.isArray(lines) ? lines : []).map((line: any) =>
                this.mapLine(line)
            ),
        };
    }

    private mapLine(item: any): StockAdjustmentLineDto {
        return {
            id: item.id ?? item.Id,
            stockAdjustmentId:
                item.stockAdjustmentId ?? item.StockAdjustmentId,
            productId: item.productId ?? item.ProductId,
            productName: item.productName ?? item.ProductName,
            quantityChange: item.quantityChange ?? item.QuantityChange ?? 0,
        };
    }
}
