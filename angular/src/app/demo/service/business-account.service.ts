import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import {
    BusinessAccountDto,
    CreateBusinessAccountDto,
    PagedBusinessAccountResultRequestDto,
    PagedResultDto,
} from '../api/business-account';
import { environment } from '../../../environments/environment';

@Injectable({
    providedIn: 'root',
})
export class BusinessAccountService {
    private readonly apiUrl = `${environment.apiUrl}/api/services/app/BusinessAccount`;

    constructor(private http: HttpClient) {}

    async getAll(
        input?: PagedBusinessAccountResultRequestDto
    ): Promise<PagedResultDto<BusinessAccountDto>> {
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
        const result = this.unwrap(res, 'Failed to load accounts');
        const items = result.items || result.Items || [];
        const totalCount = result.totalCount ?? result.TotalCount ?? items.length;

        return {
            items: (Array.isArray(items) ? items : []).map((item: any) =>
                this.mapAccount(item)
            ),
            totalCount,
        };
    }

    async get(id: number): Promise<BusinessAccountDto> {
        const res: any = await firstValueFrom(
            this.http.get<any>(`${this.apiUrl}/Get`, { params: { Id: id } })
        );
        return this.mapAccount(this.unwrap(res, 'Failed to load account'));
    }

    async create(input: CreateBusinessAccountDto): Promise<BusinessAccountDto> {
        const res: any = await firstValueFrom(
            this.http.post<any>(`${this.apiUrl}/Create`, input)
        );
        return this.mapAccount(this.unwrap(res, 'Failed to create account'));
    }

    async update(input: BusinessAccountDto): Promise<BusinessAccountDto> {
        const res: any = await firstValueFrom(
            this.http.put<any>(`${this.apiUrl}/Update`, {
                id: input.id,
                name: input.name,
                code: input.code,
                accountType: input.accountType,
                openingBalance: input.openingBalance,
                description: input.description,
                isActive: input.isActive,
            })
        );
        return this.mapAccount(this.unwrap(res, 'Failed to update account'));
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
        this.unwrap(res, 'Failed to delete account');
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

    private mapAccount(item: any): BusinessAccountDto {
        return {
            id: item.id ?? item.Id,
            name: item.name ?? item.Name,
            code: item.code ?? item.Code,
            accountType: item.accountType ?? item.AccountType,
            openingBalance: item.openingBalance ?? item.OpeningBalance ?? 0,
            balance: item.balance ?? item.Balance ?? 0,
            description: item.description ?? item.Description,
            isActive: item.isActive ?? item.IsActive ?? true,
        };
    }
}
