import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import {
    CreateLookUpDto,
    LookUpDto,
    PagedLookUpResultRequestDto,
    PagedResultDto,
} from '../api/lookup';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class LookUpService {
    private readonly apiUrl = `${environment.apiUrl}/api/services/app/LookUp`;

    constructor(private http: HttpClient) {}

    async getAll(input?: PagedLookUpResultRequestDto): Promise<PagedResultDto<LookUpDto>> {
        const params: any = {};
        if (input?.keyword) params.Keyword = input.keyword;
        if (input?.type) params.Type = input.type;
        if (input?.isActive !== undefined) params.IsActive = input.isActive;
        if (input?.skipCount !== undefined) params.SkipCount = input.skipCount;
        if (input?.maxResultCount !== undefined) params.MaxResultCount = input.maxResultCount;

        const res: any = await firstValueFrom(this.http.get<any>(`${this.apiUrl}/GetAll`, { params }));
        const result = this.unwrap(res, 'Failed to load lookups');
        const items = result.items || result.Items || [];
        return {
            items: (Array.isArray(items) ? items : []).map((item: any) => this.map(item)),
            totalCount: result.totalCount ?? result.TotalCount ?? items.length,
        };
    }

    async getByType(type: string): Promise<LookUpDto[]> {
        const res: any = await firstValueFrom(
            this.http.get<any>(`${this.apiUrl}/GetByType`, { params: { type } })
        );
        const result = this.unwrap(res, 'Failed to load lookup options');
        const items = result.items || result.Items || result || [];
        return (Array.isArray(items) ? items : []).map((item: any) => this.map(item));
    }

    async get(id: number): Promise<LookUpDto> {
        const res: any = await firstValueFrom(this.http.get<any>(`${this.apiUrl}/Get`, { params: { Id: id } }));
        return this.map(this.unwrap(res, 'Failed to load lookup'));
    }

    async create(input: CreateLookUpDto): Promise<LookUpDto> {
        const res: any = await firstValueFrom(this.http.post<any>(`${this.apiUrl}/Create`, input));
        return this.map(this.unwrap(res, 'Failed to create lookup'));
    }

    async update(input: LookUpDto): Promise<LookUpDto> {
        const res: any = await firstValueFrom(this.http.put<any>(`${this.apiUrl}/Update`, input));
        return this.map(this.unwrap(res, 'Failed to update lookup'));
    }

    async delete(id: number): Promise<void> {
        const res: any = await firstValueFrom(this.http.delete<any>(`${this.apiUrl}/Delete`, { params: { Id: id } }));
        if (res != null) this.unwrap(res, 'Failed to delete lookup');
    }

    private unwrap(res: any, fallbackMessage: string): any {
        if (!res) {
            throw new Error('No response from server');
        }
        if (res.success === false || res.error) {
            throw new Error(res.error?.message || res.error?.details || fallbackMessage);
        }
        return res.result ?? res;
    }

    private map(item: any): LookUpDto {
        return {
            id: item.id ?? item.Id,
            type: item.type ?? item.Type,
            name: item.name ?? item.Name,
            displayName: item.displayName ?? item.DisplayName,
            sortOrder: item.sortOrder ?? item.SortOrder ?? 0,
            isActive: item.isActive ?? item.IsActive ?? true,
        };
    }
}
