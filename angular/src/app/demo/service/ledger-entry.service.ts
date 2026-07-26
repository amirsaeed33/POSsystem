import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import {
    CreateLedgerEntryDto,
    LedgerEntryDto,
    PagedLedgerEntryResultRequestDto,
    PagedResultDto,
} from '../api/ledger-entry';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class LedgerEntryService {
    private readonly apiUrl = `${environment.apiUrl}/api/services/app/LedgerEntry`;

    constructor(private http: HttpClient) {}

    async getAll(
        input?: PagedLedgerEntryResultRequestDto
    ): Promise<PagedResultDto<LedgerEntryDto>> {
        const params: any = {};
        if (input?.keyword) params.Keyword = input.keyword;
        if (input?.accountId !== undefined) params.AccountId = input.accountId;
        if (input?.voucherType) params.VoucherType = input.voucherType;
        if (input?.fromDate) params.FromDate = this.toApiDate(input.fromDate);
        if (input?.toDate) params.ToDate = this.toApiDate(input.toDate);
        if (input?.skipCount !== undefined) params.SkipCount = input.skipCount;
        if (input?.maxResultCount !== undefined)
            params.MaxResultCount = input.maxResultCount;

        const res: any = await firstValueFrom(
            this.http.get<any>(`${this.apiUrl}/GetAll`, { params })
        );
        const result = this.unwrap(res, 'Failed to load ledger entries');
        const items = result.items || result.Items || [];
        return {
            items: (Array.isArray(items) ? items : []).map((i) => this.map(i)),
            totalCount: result.totalCount ?? result.TotalCount ?? items.length,
        };
    }

    async get(id: number): Promise<LedgerEntryDto> {
        const res: any = await firstValueFrom(
            this.http.get<any>(`${this.apiUrl}/Get`, { params: { Id: id } })
        );
        return this.map(this.unwrap(res, 'Failed to load ledger entry'));
    }

    async create(input: CreateLedgerEntryDto): Promise<LedgerEntryDto> {
        const res: any = await firstValueFrom(
            this.http.post<any>(`${this.apiUrl}/Create`, {
                ...input,
                transactionDate: this.toApiDate(input.transactionDate),
            })
        );
        return this.map(this.unwrap(res, 'Failed to create ledger entry'));
    }

    async update(input: LedgerEntryDto): Promise<LedgerEntryDto> {
        const res: any = await firstValueFrom(
            this.http.put<any>(`${this.apiUrl}/Update`, {
                id: input.id,
                accountId: input.accountId,
                transactionDate: this.toApiDate(input.transactionDate),
                voucherType: input.voucherType,
                voucherId: input.voucherId,
                debit: input.debit,
                credit: input.credit,
                description: input.description,
            })
        );
        return this.map(this.unwrap(res, 'Failed to update ledger entry'));
    }

    async delete(id: number): Promise<void> {
        const res: any = await firstValueFrom(
            this.http.delete<any>(`${this.apiUrl}/Delete`, {
                params: { Id: id },
            })
        );
        if (res == null) return;
        this.unwrap(res, 'Failed to delete ledger entry');
    }

    private toApiDate(value?: string | Date): string | undefined {
        if (!value) return undefined;
        if (value instanceof Date) return value.toISOString();
        const parsed = new Date(
            value.includes('T') ? value : `${value}T00:00:00`
        );
        return isNaN(parsed.getTime()) ? value : parsed.toISOString();
    }

    private unwrap(res: any, fallback: string): any {
        if (!res) throw new Error('No response from server');
        if (res.success === false || res.error) {
            throw new Error(res.error?.message || res.error?.details || fallback);
        }
        return res.result ?? res;
    }

    private map(item: any): LedgerEntryDto {
        return {
            id: item.id ?? item.Id,
            accountId: item.accountId ?? item.AccountId,
            accountName: item.accountName ?? item.AccountName,
            transactionDate: item.transactionDate ?? item.TransactionDate,
            voucherType: item.voucherType ?? item.VoucherType,
            voucherId: item.voucherId ?? item.VoucherId,
            debit: item.debit ?? item.Debit ?? 0,
            credit: item.credit ?? item.Credit ?? 0,
            description: item.description ?? item.Description,
        };
    }
}
