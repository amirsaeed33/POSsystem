import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import {
    CategoryDto,
    CreateCategoryDto,
    PagedCategoryResultRequestDto,
    PagedResultDto,
} from '../api/category';
import { environment } from '../../../environments/environment';
import { TtlCache, branchCacheKey } from './ttl-cache';

@Injectable({
    providedIn: 'root',
})
export class CategoryService {
    private readonly apiUrl = `${environment.apiUrl}/api/services/app/Category`;
    private readonly lookupCache = new TtlCache<any[]>(5 * 60 * 1000);

    constructor(private http: HttpClient) {}

        async getAll(
        input?: PagedCategoryResultRequestDto
    ): Promise<PagedResultDto<CategoryDto>> {
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
        const result = this.unwrap(res, 'Failed to load categories');
        const items = result.items || result.Items || [];
        const totalCount = result.totalCount ?? result.TotalCount ?? items.length;

        return {
            items: (Array.isArray(items) ? items : []).map((item: any) =>
                this.mapCategory(item)
            ),
            totalCount,
        };
    }

    clearCache(): void {
        this.lookupCache.clear();
    }

    async getLookup(forceRefresh = false): Promise<CategoryDto[]> {
        const key = branchCacheKey(this.readBranchId(), 'lookup');
        if (!forceRefresh) {
            const hit = this.lookupCache.get(key);
            if (hit) {
                return hit;
            }
        }

        const res: any = await firstValueFrom(
            this.http.get<any>(`${this.apiUrl}/GetLookup`)
        );
        const result = this.unwrap(res, 'Failed to load categories');
        const items = result.items || result.Items || result || [];
        const mapped = (Array.isArray(items) ? items : []).map((item: any) =>
            this.mapCategory(item)
        );
        this.lookupCache.set(key, mapped);
        return mapped;
    }

    private readBranchId(): number | null {
        const n = parseInt(localStorage.getItem('SmartPos.BranchId') || '', 10);
        return Number.isFinite(n) && n > 0 ? n : null;
    }

    async get(id: number): Promise<CategoryDto> {
        const res: any = await firstValueFrom(
            this.http.get<any>(`${this.apiUrl}/Get`, { params: { Id: id } })
        );
        return this.mapCategory(this.unwrap(res, 'Failed to load category'));
    }

    async create(input: CreateCategoryDto): Promise<CategoryDto> {
        this.clearCache();
        const res: any = await firstValueFrom(
            this.http.post<any>(`${this.apiUrl}/Create`, {
                name: input.name,
                description: input.description,
                defaultUnitId: input.defaultUnitId,
            })
        );
        return this.mapCategory(this.unwrap(res, 'Failed to create category'));
    }

    async update(input: CategoryDto): Promise<CategoryDto> {
        this.clearCache();
        const res: any = await firstValueFrom(
            this.http.put<any>(`${this.apiUrl}/Update`, {
                id: input.id,
                branchId: input.branchId,
                name: input.name,
                description: input.description,
                defaultUnitId: input.defaultUnitId,
            })
        );
        return this.mapCategory(this.unwrap(res, 'Failed to update category'));
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
        this.unwrap(res, 'Failed to delete category');
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

    private mapCategory(item: any): CategoryDto {
        return {
            id: item.id ?? item.Id,
            branchId: item.branchId ?? item.BranchId,
            name: item.name ?? item.Name,
            description: item.description ?? item.Description,
            defaultUnitId: item.defaultUnitId ?? item.DefaultUnitId,
            defaultUnitName: item.defaultUnitName ?? item.DefaultUnitName,
        };
    }
}
