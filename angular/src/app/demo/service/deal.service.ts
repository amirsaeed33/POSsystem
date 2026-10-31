import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import {
    CreateDealDto,
    UpdateDealDto,
    DealDto,
    PagedDealResultRequestDto,
} from '../api/deal';
import { environment } from '../../../environments/environment';
import { TtlCache, branchCacheKey } from './ttl-cache';

@Injectable({
    providedIn: 'root',
})
export class DealService {
    private readonly apiUrl = `${environment.apiUrl}/api/services/app/Deal`;
    private readonly byProductCache = new TtlCache<DealDto>(5 * 60 * 1000);

    constructor(private http: HttpClient) {}

    clearCache(): void {
        this.byProductCache.clear();
    }

    async create(input: CreateDealDto): Promise<DealDto> {
        try {
            this.clearCache();
            const res: any = await firstValueFrom(
                this.http.post<any>(`${this.apiUrl}/Create`, input)
            );
            return this.unwrap(res, 'Failed to create deal');
        } catch (error) {
            throw new Error(
                this.extractErrorMessage(error, 'Failed to create deal')
            );
        }
    }

    async update(input: UpdateDealDto): Promise<DealDto> {
        try {
            this.clearCache();
            const res: any = await firstValueFrom(
                this.http.put<any>(`${this.apiUrl}/Update`, input)
            );
            return this.unwrap(res, 'Failed to update deal');
        } catch (error) {
            throw new Error(
                this.extractErrorMessage(error, 'Failed to update deal')
            );
        }
    }

    async get(id: number): Promise<DealDto> {
        try {
            const res: any = await firstValueFrom(
                this.http.get<any>(`${this.apiUrl}/Get`, {
                    params: { Id: id.toString() },
                })
            );
            return this.unwrap(res, 'Failed to get deal');
        } catch (error) {
            throw new Error(
                this.extractErrorMessage(error, 'Failed to get deal')
            );
        }
    }

    async getByProductId(productId: number): Promise<DealDto> {
        try {
            const key = branchCacheKey(this.readBranchId(), `deal:${productId}`);
            const hit = this.byProductCache.get(key);
            if (hit) {
                return hit;
            }

            const res: any = await firstValueFrom(
                this.http.get<any>(`${this.apiUrl}/GetByProductId`, {
                    params: { productId: productId.toString() },
                })
            );
            const deal = this.unwrap(res, 'Failed to get deal details');
            this.byProductCache.set(key, deal);
            return deal;
        } catch (error) {
            throw new Error(
                this.extractErrorMessage(error, 'Failed to get deal details')
            );
        }
    }

    private readBranchId(): number | null {
        const n = parseInt(localStorage.getItem('SmartPos.BranchId') || '', 10);
        return Number.isFinite(n) && n > 0 ? n : null;
    }

    async getAll(
        input?: PagedDealResultRequestDto
    ): Promise<{ items: DealDto[]; totalCount: number }> {
        const params: any = {};
        if (input?.keyword) {
            params.Keyword = input.keyword;
        }
        if (input?.branchId) {
            params.BranchId = input.branchId;
        }
        if (input?.skipCount !== undefined) {
            params.SkipCount = input.skipCount;
        }
        if (input?.maxResultCount !== undefined) {
            params.MaxResultCount = input.maxResultCount;
        }

        try {
            const res: any = await firstValueFrom(
                this.http.get<any>(`${this.apiUrl}/GetAll`, { params })
            );
            const result = this.unwrap(res, 'Failed to load deals');
            const items = result.items || result.Items || [];
            const totalCount =
                result.totalCount ?? result.TotalCount ?? items.length;

            return {
                items,
                totalCount,
            };
        } catch (error) {
            throw new Error(
                this.extractErrorMessage(error, 'Failed to load deals')
            );
        }
    }

    async delete(id: number): Promise<void> {
        try {
            this.clearCache();
            const res: any = await firstValueFrom(
                this.http.delete<any>(`${this.apiUrl}/Delete`, {
                    params: { Id: id.toString() },
                })
            );
            if (res != null) {
                this.unwrap(res, 'Failed to delete deal');
            }
        } catch (error) {
            throw new Error(
                this.extractErrorMessage(error, 'Failed to delete deal')
            );
        }
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
}
