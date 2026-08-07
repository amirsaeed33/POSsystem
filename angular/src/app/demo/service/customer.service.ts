import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import {
    CreateCustomerDto,
    CustomerDto,
    PagedCustomerResultRequestDto,
    PagedResultDto,
} from '../api/customer';
import { environment } from '../../../environments/environment';

@Injectable({
    providedIn: 'root',
})
export class CustomerService {
    private readonly apiUrl = `${environment.apiUrl}/api/services/app/Customer`;

    constructor(private http: HttpClient) {}

    async getAll(
        input?: PagedCustomerResultRequestDto
    ): Promise<PagedResultDto<CustomerDto>> {
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
        const result = this.unwrap(res, 'Failed to load customers');
        const items = result.items || result.Items || [];
        const totalCount = result.totalCount ?? result.TotalCount ?? items.length;

        return {
            items: (Array.isArray(items) ? items : []).map((item: any) =>
                this.mapCustomer(item)
            ),
            totalCount,
        };
    }

    async get(id: number): Promise<CustomerDto> {
        const res: any = await firstValueFrom(
            this.http.get<any>(`${this.apiUrl}/Get`, { params: { Id: id } })
        );
        return this.mapCustomer(this.unwrap(res, 'Failed to load customer'));
    }

    async create(input: CreateCustomerDto): Promise<CustomerDto> {
        const res: any = await firstValueFrom(
            this.http.post<any>(`${this.apiUrl}/Create`, {
                name: input.name,
                customerType: input.customerType,
                phone: input.phone,
                email: input.email,
                address: input.address,
                description: input.description,
            })
        );
        return this.mapCustomer(this.unwrap(res, 'Failed to create customer'));
    }

    async update(input: CustomerDto): Promise<CustomerDto> {
        const res: any = await firstValueFrom(
            this.http.put<any>(`${this.apiUrl}/Update`, {
                id: input.id,
                name: input.name,
                customerType: input.customerType,
                phone: input.phone,
                email: input.email,
                address: input.address,
                description: input.description,
            })
        );
        return this.mapCustomer(this.unwrap(res, 'Failed to update customer'));
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
        this.unwrap(res, 'Failed to delete customer');
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

    private mapCustomer(item: any): CustomerDto {
        return {
            id: item.id ?? item.Id,
            branchId: item.branchId ?? item.BranchId ?? 0,
            name: item.name ?? item.Name,
            customerType: item.customerType ?? item.CustomerType ?? 0,
            phone: item.phone ?? item.Phone,
            email: item.email ?? item.Email,
            address: item.address ?? item.Address,
            description: item.description ?? item.Description,
            accountId: item.accountId ?? item.AccountId,
            balance: item.balance ?? item.Balance ?? 0,
        };
    }
}
