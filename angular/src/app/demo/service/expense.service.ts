import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import {
    CreateExpenseDto,
    ExpenseDto,
    PagedExpenseResultRequestDto,
    PagedResultDto,
} from '../api/expense';
import { environment } from '../../../environments/environment';

@Injectable({
    providedIn: 'root',
})
export class ExpenseService {
    private readonly apiUrl = `${environment.apiUrl}/api/services/app/Expense`;

    constructor(private http: HttpClient) {}

    async getAll(
        input?: PagedExpenseResultRequestDto
    ): Promise<PagedResultDto<ExpenseDto>> {
        const params: any = {};
        if (input?.keyword) {
            params.Keyword = input.keyword;
        }
        if (input?.paymentAccountId !== undefined) {
            params.PaymentAccountId = input.paymentAccountId;
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
        const result = this.unwrap(res, 'Failed to load expenses');
        const items = result.items || result.Items || [];
        const totalCount = result.totalCount ?? result.TotalCount ?? items.length;

        return {
            items: (Array.isArray(items) ? items : []).map((item: any) =>
                this.mapExpense(item)
            ),
            totalCount,
        };
    }

    async get(id: number): Promise<ExpenseDto> {
        const res: any = await firstValueFrom(
            this.http.get<any>(`${this.apiUrl}/Get`, { params: { Id: id } })
        );
        return this.mapExpense(this.unwrap(res, 'Failed to load expense'));
    }

    async create(input: CreateExpenseDto): Promise<ExpenseDto> {
        const res: any = await firstValueFrom(
            this.http.post<any>(`${this.apiUrl}/Create`, {
                expenseDate: this.toApiDate(input.expenseDate),
                amount: input.amount,
                referenceNo: input.referenceNo,
                description: input.description,
                paymentAccountId: input.paymentAccountId,
            })
        );
        return this.mapExpense(this.unwrap(res, 'Failed to create expense'));
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
        this.unwrap(res, 'Failed to delete expense');
    }

    /**
     * Backend Update rejects edits. Edit is create-then-delete using existing APIs.
     */
    async replace(id: number, input: CreateExpenseDto): Promise<ExpenseDto> {
        const created = await this.create(input);
        try {
            await this.delete(id);
        } catch (error) {
            throw new Error(
                (error as any)?.message ||
                    'Updated expense was created, but the old one could not be deleted. Please delete it manually.'
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

    private mapExpense(item: any): ExpenseDto {
        return {
            id: item.id ?? item.Id,
            expenseDate: item.expenseDate ?? item.ExpenseDate,
            amount: item.amount ?? item.Amount ?? 0,
            referenceNo: item.referenceNo ?? item.ReferenceNo,
            description: item.description ?? item.Description,
            paymentAccountId: item.paymentAccountId ?? item.PaymentAccountId,
            paymentAccountName:
                item.paymentAccountName ?? item.PaymentAccountName,
            expenseAccountId: item.expenseAccountId ?? item.ExpenseAccountId,
            expenseAccountName:
                item.expenseAccountName ?? item.ExpenseAccountName,
            createdByName: item.createdByName ?? item.CreatedByName,
        };
    }
}
