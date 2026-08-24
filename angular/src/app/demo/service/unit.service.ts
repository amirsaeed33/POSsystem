import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import {
    CreateUnitDto,
    PagedResultDto,
    PagedUnitResultRequestDto,
    UnitDto,
} from '../api/unit';
import { environment } from '../../../environments/environment';

@Injectable({
    providedIn: 'root',
})
export class UnitService {
    private readonly apiUrl = `${environment.apiUrl}/api/services/app/Unit`;

    constructor(private http: HttpClient) {}

        async getAll(
        input?: PagedUnitResultRequestDto
    ): Promise<PagedResultDto<UnitDto>> {
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
        const result = this.unwrap(res, 'Failed to load units');
        const items = result.items || result.Items || [];
        const totalCount = result.totalCount ?? result.TotalCount ?? items.length;

        return {
            items: (Array.isArray(items) ? items : []).map((item: any) =>
                this.mapUnit(item)
            ),
            totalCount,
        };
    }

    async getLookup(): Promise<UnitDto[]> {
        const res: any = await firstValueFrom(
            this.http.get<any>(`${this.apiUrl}/GetLookup`)
        );
        const result = this.unwrap(res, 'Failed to load units');
        const items = result.items || result.Items || result || [];
        return (Array.isArray(items) ? items : []).map((item: any) =>
            this.mapUnit(item)
        );
    }

    async get(id: number): Promise<UnitDto> {
        const res: any = await firstValueFrom(
            this.http.get<any>(`${this.apiUrl}/Get`, { params: { Id: id } })
        );
        return this.mapUnit(this.unwrap(res, 'Failed to load unit'));
    }

    async create(input: CreateUnitDto): Promise<UnitDto> {
        const res: any = await firstValueFrom(
            this.http.post<any>(`${this.apiUrl}/Create`, {
                name: input.name,
                description: input.description,
            })
        );
        return this.mapUnit(this.unwrap(res, 'Failed to create unit'));
    }

    async update(input: UnitDto): Promise<UnitDto> {
        const res: any = await firstValueFrom(
            this.http.put<any>(`${this.apiUrl}/Update`, {
                id: input.id,
                branchId: input.branchId,
                name: input.name,
                symbol: input.symbol,
                description: input.description,
            })
        );
        return this.mapUnit(this.unwrap(res, 'Failed to update unit'));
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
        this.unwrap(res, 'Failed to delete unit');
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

    private mapUnit(item: any): UnitDto {
        return {
            id: item.id ?? item.Id,
            branchId: item.branchId ?? item.BranchId,
            name: item.name ?? item.Name,
            symbol: item.symbol ?? item.Symbol,
            description: item.description ?? item.Description,
        };
    }
}
