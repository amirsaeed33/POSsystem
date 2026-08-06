import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import {
    BranchDto,
    CreateBranchDto,
    PagedBranchResultRequestDto,
    PagedResultDto,
} from '../api/branch';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class BranchService {
    private readonly apiUrl = `${environment.apiUrl}/api/services/app/Branch`;

    constructor(private http: HttpClient) {}

    async getAll(
        input?: PagedBranchResultRequestDto
    ): Promise<PagedResultDto<BranchDto>> {
        const params: any = {};
        if (input?.keyword) params.Keyword = input.keyword;
        if (input?.statusId != null) params.StatusId = input.statusId;
        if (input?.skipCount !== undefined) params.SkipCount = input.skipCount;
        if (input?.maxResultCount !== undefined)
            params.MaxResultCount = input.maxResultCount;

        const res: any = await firstValueFrom(
            this.http.get<any>(`${this.apiUrl}/GetAll`, { params })
        );
        const result = this.unwrap(res, 'Failed to load branches');
        const items = result.items || result.Items || [];
        return {
            items: (Array.isArray(items) ? items : []).map((i) => this.map(i)),
            totalCount: result.totalCount ?? result.TotalCount ?? items.length,
        };
    }

    async get(id: number): Promise<BranchDto> {
        const res: any = await firstValueFrom(
            this.http.get<any>(`${this.apiUrl}/Get`, { params: { Id: id } })
        );
        return this.map(this.unwrap(res, 'Failed to load branch'));
    }

    async getLookup(): Promise<BranchDto[]> {
        const res: any = await firstValueFrom(
            this.http.get<any>(`${this.apiUrl}/GetLookup`)
        );
        const result = this.unwrap(res, 'Failed to load branches');
        const items = result.items || result.Items || result || [];
        return (Array.isArray(items) ? items : []).map((i) => this.map(i));
    }

    async getInvoiceInfo(): Promise<BranchDto | null> {
        try {
            const res: any = await firstValueFrom(
                this.http.get<any>(`${this.apiUrl}/GetInvoiceInfo`)
            );
            const result = this.unwrap(res, 'Failed to load branch');
            if (!result) {
                return null;
            }
            return this.map(result);
        } catch {
            return null;
        }
    }

    async create(input: CreateBranchDto): Promise<BranchDto> {
        const res: any = await firstValueFrom(
            this.http.post<any>(`${this.apiUrl}/Create`, input)
        );
        return this.map(this.unwrap(res, 'Failed to create branch'));
    }

    async update(
        input: CreateBranchDto & {
            id: number;
            imagePath?: string;
            statusId?: number;
        }
    ): Promise<BranchDto> {
        const res: any = await firstValueFrom(
            this.http.put<any>(`${this.apiUrl}/Update`, {
                id: input.id,
                name: input.name,
                code: input.code,
                statusId: input.statusId,
                isActive: input.isActive,
                isDefault: input.isDefault,
                imagePath: input.imagePath,
                imageBase64: input.imageBase64,
                invoiceAddress: input.invoiceAddress,
                invoiceContactEmail: input.invoiceContactEmail,
                invoiceContactPhone: input.invoiceContactPhone,
                taxNumber: input.taxNumber,
                website: input.website,
                invoiceFooter: input.invoiceFooter,
            })
        );
        return this.map(this.unwrap(res, 'Failed to update branch'));
    }

    async getPendingApprovals(): Promise<BranchDto[]> {
        const res: any = await firstValueFrom(
            this.http.get<any>(`${this.apiUrl}/GetPendingApprovals`)
        );
        const result = this.unwrap(res, 'Failed to load pending branches');
        const items = result.items || result.Items || [];
        return (Array.isArray(items) ? items : []).map((i) => this.map(i));
    }

    async changeStatus(id: number, statusId: number): Promise<BranchDto> {
        const res: any = await firstValueFrom(
            this.http.post<any>(`${this.apiUrl}/ChangeStatus`, {
                id,
                statusId,
            })
        );
        return this.map(this.unwrap(res, 'Failed to update branch status'));
    }

    async delete(id: number): Promise<void> {
        const res: any = await firstValueFrom(
            this.http.delete<any>(`${this.apiUrl}/Delete`, {
                params: { Id: id },
            })
        );
        if (res == null) return;
        this.unwrap(res, 'Failed to delete branch');
    }

    getImageUrl(imagePath?: string): string {
        if (!imagePath) return '';
        if (
            imagePath.startsWith('http') ||
            imagePath.startsWith('data:')
        ) {
            return imagePath;
        }
        return `${environment.apiUrl}${imagePath.startsWith('/') ? '' : '/'}${imagePath}`;
    }

    private unwrap(res: any, fallback: string): any {
        if (!res) throw new Error('No response from server');
        if (res.success === false || res.error) {
            throw new Error(res.error?.message || res.error?.details || fallback);
        }
        return res.result ?? res;
    }

    private map(item: any): BranchDto {
        return {
            id: item.id ?? item.Id,
            name: item.name ?? item.Name,
            code: item.code ?? item.Code,
            statusId: item.statusId ?? item.StatusId ?? 0,
            status: item.status ?? item.Status ?? 'Pending',
            statusDisplayName:
                item.statusDisplayName ?? item.StatusDisplayName,
            creationTime: item.creationTime ?? item.CreationTime,
            tenantId: item.tenantId ?? item.TenantId ?? null,
            tenancyName: item.tenancyName ?? item.TenancyName,
            isActive: item.isActive ?? item.IsActive ?? true,
            isDefault: item.isDefault ?? item.IsDefault ?? false,
            imagePath: item.imagePath ?? item.ImagePath,
            imageBase64: item.imageBase64 ?? item.ImageBase64,
            invoiceAddress: item.invoiceAddress ?? item.InvoiceAddress,
            invoiceContactEmail:
                item.invoiceContactEmail ?? item.InvoiceContactEmail,
            invoiceContactPhone:
                item.invoiceContactPhone ?? item.InvoiceContactPhone,
            taxNumber: item.taxNumber ?? item.TaxNumber,
            website: item.website ?? item.Website,
            invoiceFooter: item.invoiceFooter ?? item.InvoiceFooter,
        };
    }
}
