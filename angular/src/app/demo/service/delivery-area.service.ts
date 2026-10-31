import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PagedResultDto } from '../api/customer-order';
import { TtlCache, branchCacheKey } from './ttl-cache';

export interface DeliveryAreaDto {
    id: number;
    branchId: number;
    branchName?: string;
    name: string;
    deliveryFee: number;
    minimumOrderAmount: number;
    estimatedDeliveryTime?: string;
    coverageDetails?: string;
    isActive: boolean;
}

export interface CreateDeliveryAreaDto {
    branchId?: number;
    name: string;
    deliveryFee: number;
    minimumOrderAmount: number;
    estimatedDeliveryTime?: string;
    coverageDetails?: string;
    isActive: boolean;
}

export interface PagedDeliveryAreaResultRequestDto {
    keyword?: string;
    branchId?: number;
    isActive?: boolean;
    skipCount?: number;
    maxResultCount?: number;
}

@Injectable({ providedIn: 'root' })
export class DeliveryAreaService {
    private readonly apiUrl = `${environment.apiUrl}/api/services/app/DeliveryArea`;
    private readonly activeAreasCache = new TtlCache<DeliveryAreaDto[]>(
        5 * 60 * 1000
    );

    constructor(private http: HttpClient) {}

    clearCache(): void {
        this.activeAreasCache.clear();
    }

    async getAll(
        input?: PagedDeliveryAreaResultRequestDto
    ): Promise<PagedResultDto<DeliveryAreaDto>> {
        const params: any = {};
        if (input?.keyword) params.Keyword = input.keyword;
        if (input?.branchId !== undefined && input?.branchId !== null)
            params.BranchId = input.branchId;
        if (input?.isActive !== undefined && input?.isActive !== null)
            params.IsActive = input.isActive;
        if (input?.skipCount !== undefined) params.SkipCount = input.skipCount;
        if (input?.maxResultCount !== undefined)
            params.MaxResultCount = input.maxResultCount;

        const res: any = await firstValueFrom(
            this.http.get<any>(`${this.apiUrl}/GetAll`, { params })
        );
        const result = this.unwrap(res, 'Failed to load delivery areas');
        const items = result.items || result.Items || [];
        return {
            items: (Array.isArray(items) ? items : []).map((i) => this.map(i)),
            totalCount: result.totalCount ?? result.TotalCount ?? items.length,
        };
    }

    async get(id: number): Promise<DeliveryAreaDto> {
        const res: any = await firstValueFrom(
            this.http.get<any>(`${this.apiUrl}/Get`, { params: { Id: id } })
        );
        return this.map(this.unwrap(res, 'Failed to load delivery area'));
    }

    async create(input: CreateDeliveryAreaDto): Promise<DeliveryAreaDto> {
        this.clearCache();
        const res: any = await firstValueFrom(
            this.http.post<any>(`${this.apiUrl}/Create`, {
                branchId: input.branchId,
                name: input.name,
                deliveryFee: input.deliveryFee,
                minimumOrderAmount: input.minimumOrderAmount,
                estimatedDeliveryTime: input.estimatedDeliveryTime,
                coverageDetails: input.coverageDetails,
                isActive: input.isActive,
            })
        );
        return this.map(this.unwrap(res, 'Failed to create delivery area'));
    }

    async update(input: DeliveryAreaDto): Promise<DeliveryAreaDto> {
        this.clearCache();
        const res: any = await firstValueFrom(
            this.http.put<any>(`${this.apiUrl}/Update`, {
                id: input.id,
                branchId: input.branchId,
                name: input.name,
                deliveryFee: input.deliveryFee,
                minimumOrderAmount: input.minimumOrderAmount,
                estimatedDeliveryTime: input.estimatedDeliveryTime,
                coverageDetails: input.coverageDetails,
                isActive: input.isActive,
            })
        );
        return this.map(this.unwrap(res, 'Failed to update delivery area'));
    }

    async delete(id: number): Promise<void> {
        this.clearCache();
        const res: any = await firstValueFrom(
            this.http.delete<any>(`${this.apiUrl}/Delete`, {
                params: { Id: id },
            })
        );
        if (res == null) return;
        this.unwrap(res, 'Failed to delete delivery area');
    }

    async toggleActive(id: number): Promise<DeliveryAreaDto> {
        this.clearCache();
        const res: any = await firstValueFrom(
            this.http.post<any>(`${this.apiUrl}/ToggleActive`, { id })
        );
        return this.map(this.unwrap(res, 'Failed to toggle status'));
    }

    async getActiveAreasForBranch(branchId: number): Promise<DeliveryAreaDto[]> {
        const key = branchCacheKey(branchId, 'activeAreas');
        const hit = this.activeAreasCache.get(key);
        if (hit) {
            return hit;
        }

        const res: any = await firstValueFrom(
            this.http.get<any>(`${this.apiUrl}/GetActiveAreasForBranch`, {
                params: { branchId },
            })
        );
        const result = this.unwrap(res, 'Failed to load active delivery areas');
        const items = Array.isArray(result) ? result : result?.items || [];
        const mapped = items.map((i: any) => this.map(i));
        this.activeAreasCache.set(key, mapped);
        return mapped;
    }

    private unwrap(res: any, fallback: string): any {
        if (!res) throw new Error('No response from server');
        if (res.success === false || res.error) {
            throw new Error(res.error?.message || res.error?.details || fallback);
        }
        return res.result ?? res;
    }

    private map(item: any): DeliveryAreaDto {
        return {
            id: item.id ?? item.Id,
            branchId: item.branchId ?? item.BranchId,
            branchName: item.branchName ?? item.BranchName,
            name: item.name ?? item.Name,
            deliveryFee: item.deliveryFee ?? item.DeliveryFee ?? 0,
            minimumOrderAmount: item.minimumOrderAmount ?? item.MinimumOrderAmount ?? 0,
            estimatedDeliveryTime: item.estimatedDeliveryTime ?? item.EstimatedDeliveryTime,
            coverageDetails: item.coverageDetails ?? item.CoverageDetails,
            isActive: item.isActive ?? item.IsActive ?? true,
        };
    }
}
