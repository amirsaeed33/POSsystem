import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import {
    CreateGroceryPurchaseDto,
    GroceryPurchaseDto,
    PagedGroceryPurchaseResultRequestDto,
    UpdateGroceryPurchaseDto,
} from '../api/grocery-purchase';
import { PagedResultDto } from '../api/branch';
import { environment } from '../../../environments/environment';

@Injectable({
    providedIn: 'root',
})
export class GroceryPurchaseService {
    private readonly apiUrl = `${environment.apiUrl}/api/services/app/GroceryPurchase`;

    constructor(private http: HttpClient) {}

    async getAll(
        input?: PagedGroceryPurchaseResultRequestDto
    ): Promise<PagedResultDto<GroceryPurchaseDto>> {
        const params: any = {};
        if (input?.keyword) {
            params.Keyword = input.keyword;
        }
        if (input?.branchId !== undefined && input?.branchId !== null) {
            params.BranchId = input.branchId;
        }
        if (input?.startDate) {
            params.StartDate = this.toApiDate(input.startDate);
        }
        if (input?.endDate) {
            params.EndDate = this.toApiDate(input.endDate);
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
        const result: any = this.unwrap<any>(res, 'Failed to load grocery purchases');
        const items = result.items || result.Items || [];
        const totalCount = result.totalCount ?? result.TotalCount ?? items.length;

        return {
            items: (Array.isArray(items) ? items : []).map((item: any) =>
                this.mapItem(item)
            ),
            totalCount,
        };
    }

    async get(id: number): Promise<GroceryPurchaseDto> {
        const res: any = await firstValueFrom(
            this.http.get<any>(`${this.apiUrl}/Get`, { params: { Id: id } })
        );
        return this.mapItem(this.unwrap(res, 'Failed to load grocery purchase'));
    }

    async create(input: CreateGroceryPurchaseDto): Promise<GroceryPurchaseDto> {
        const res: any = await firstValueFrom(
            this.http.post<any>(`${this.apiUrl}/Create`, {
                branchId: input.branchId,
                productName: input.productName,
                purchasePrice: input.purchasePrice,
                unit: input.unit,
                unitId: input.unitId,
                purchaseDate: this.toApiDate(input.purchaseDate),
            })
        );
        return this.mapItem(this.unwrap(res, 'Failed to create grocery purchase'));
    }

    async update(input: UpdateGroceryPurchaseDto): Promise<GroceryPurchaseDto> {
        const res: any = await firstValueFrom(
            this.http.put<any>(`${this.apiUrl}/Update`, {
                id: input.id,
                productName: input.productName,
                purchasePrice: input.purchasePrice,
                unit: input.unit,
                unitId: input.unitId,
                purchaseDate: this.toApiDate(input.purchaseDate),
            })
        );
        return this.mapItem(this.unwrap(res, 'Failed to update grocery purchase'));
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
        this.unwrap(res, 'Failed to delete grocery purchase');
    }

    private toApiDate(value: string | Date | undefined): string | undefined {
        if (!value) {
            return undefined;
        }
        if (value instanceof Date) {
            return value.toISOString();
        }
        const d = new Date(value);
        return isNaN(d.getTime()) ? undefined : d.toISOString();
    }

    private unwrap<T>(res: any, fallbackMessage: string): T {
        if (res == null) {
            return res as T;
        }
        if (res.success === false) {
            throw new Error(res.error?.message || fallbackMessage);
        }
        if ('result' in res) {
            return res.result as T;
        }
        return res as T;
    }

    private mapItem(item: any): GroceryPurchaseDto {
        return {
            id: item.id ?? item.Id,
            creationTime: item.creationTime ?? item.CreationTime,
            branchId: item.branchId ?? item.BranchId,
            branchName: item.branchName ?? item.BranchName,
            productName: item.productName ?? item.ProductName,
            purchasePrice: Number(item.purchasePrice ?? item.PurchasePrice ?? 0),
            unit: item.unit ?? item.Unit,
            unitId: item.unitId ?? item.UnitId,
            purchaseDate: item.purchaseDate ?? item.PurchaseDate,
            createdByName: item.createdByName ?? item.CreatedByName,
        };
    }
}
