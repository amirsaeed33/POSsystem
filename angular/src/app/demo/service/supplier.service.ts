import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import {
    CreateSupplierDto,
    PagedResultDto,
    PagedSupplierResultRequestDto,
    SupplierDto,
} from '../api/supplier';
import { environment } from '../../../environments/environment';
import { TtlCache, branchCacheKey } from './ttl-cache';

@Injectable({
    providedIn: 'root',
})
export class SupplierService {
    private readonly apiUrl = `${environment.apiUrl}/api/services/app/Supplier`;
    private readonly listCache = new TtlCache<PagedResultDto<SupplierDto>>(90 * 1000);
    private readonly lookupCache = new TtlCache<SupplierDto[]>(5 * 60 * 1000);

    constructor(private http: HttpClient) {}

    clearCache(): void {
        this.listCache.clear();
        this.lookupCache.clear();
    }

    async getAll(
        input?: PagedSupplierResultRequestDto,
        forceRefresh = false
    ): Promise<PagedResultDto<SupplierDto>> {
        const cacheKey = branchCacheKey(
            this.readBranchId(),
            [
                'list',
                input?.keyword || '',
                input?.skipCount ?? 0,
                input?.maxResultCount ?? 1000,
            ].join('|')
        );

        if (!forceRefresh) {
            const hit = this.listCache.get(cacheKey);
            if (hit) {
                return {
                    items: hit.items.map((s) => ({ ...s })),
                    totalCount: hit.totalCount,
                };
            }
        }

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
        const result = this.unwrap(res, 'Failed to load suppliers');
        const items = result.items || result.Items || [];
        const totalCount = result.totalCount ?? result.TotalCount ?? items.length;
        const mapped: PagedResultDto<SupplierDto> = {
            items: (Array.isArray(items) ? items : []).map((item: any) =>
                this.mapSupplier(item)
            ),
            totalCount,
        };
        this.listCache.set(cacheKey, mapped);
        return mapped;
    }

    async getLookup(forceRefresh = false): Promise<SupplierDto[]> {
        const key = branchCacheKey(this.readBranchId(), 'lookup');
        if (!forceRefresh) {
            const hit = this.lookupCache.get(key);
            if (hit) {
                return hit;
            }
        }

        const res = await this.getAll({ skipCount: 0, maxResultCount: 1000 }, forceRefresh);
        const items = res.items || [];
        this.lookupCache.set(key, items);
        return items;
    }

    private readBranchId(): number | null {
        const n = parseInt(localStorage.getItem('SmartPos.BranchId') || '', 10);
        return Number.isFinite(n) && n > 0 ? n : null;
    }

    async get(id: number): Promise<SupplierDto> {
        const res: any = await firstValueFrom(
            this.http.get<any>(`${this.apiUrl}/Get`, { params: { Id: id } })
        );
        return this.mapSupplier(this.unwrap(res, 'Failed to load supplier'));
    }

    async create(input: CreateSupplierDto): Promise<SupplierDto> {
        this.clearCache();
        const res: any = await firstValueFrom(
            this.http.post<any>(`${this.apiUrl}/Create`, {
                name: input.name,
                phone: input.phone,
                email: input.email,
                address: input.address,
                description: input.description,
            })
        );
        return this.mapSupplier(this.unwrap(res, 'Failed to create supplier'));
    }

    async update(input: SupplierDto): Promise<SupplierDto> {
        this.clearCache();
        const res: any = await firstValueFrom(
            this.http.put<any>(`${this.apiUrl}/Update`, {
                id: input.id,
                name: input.name,
                phone: input.phone,
                email: input.email,
                address: input.address,
                description: input.description,
            })
        );
        return this.mapSupplier(this.unwrap(res, 'Failed to update supplier'));
    }

    async delete(id: number): Promise<void> {
        this.clearCache();
        const res: any = await firstValueFrom(
            this.http.delete<any>(`${this.apiUrl}/Delete`, {
                params: { Id: id },
            })
        );
        if (res == null) {
            return;
        }
        this.unwrap(res, 'Failed to delete supplier');
    }

    async getAssignedProductIds(supplierId: number): Promise<number[]> {
        const res: any = await firstValueFrom(
            this.http.get<any>(`${this.apiUrl}/GetAssignedProductIds`, {
                params: { Id: supplierId },
            })
        );
        const result = this.unwrap(res, 'Failed to load assigned products');
        const items = Array.isArray(result) ? result : result?.items || result?.Items || [];
        return (items as any[])
            .map((x) => Number(x))
            .filter((id) => Number.isFinite(id) && id > 0);
    }

    async setAssignedProducts(
        supplierId: number,
        productIds: number[]
    ): Promise<void> {
        this.clearCache();
        const res: any = await firstValueFrom(
            this.http.post<any>(`${this.apiUrl}/SetAssignedProducts`, {
                supplierId,
                productIds: productIds || [],
            })
        );
        if (res == null) {
            return;
        }
        this.unwrap(res, 'Failed to assign products');
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

    private mapSupplier(item: any): SupplierDto {
        return {
            id: item.id ?? item.Id,
            branchId: item.branchId ?? item.BranchId ?? 0,
            name: item.name ?? item.Name,
            phone: item.phone ?? item.Phone,
            email: item.email ?? item.Email,
            address: item.address ?? item.Address,
            description: item.description ?? item.Description,
            accountId: item.accountId ?? item.AccountId,
            balance: item.balance ?? item.Balance ?? 0,
        };
    }
}
