import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import {
    CompanyProfileDto,
    CreateCompanyProfileDto,
    PagedCompanyProfileResultRequestDto,
    PagedResultDto,
} from '../api/company-profile';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class CompanyProfileService {
    private readonly apiUrl = `${environment.apiUrl}/api/services/app/CompanyProfile`;

    constructor(private http: HttpClient) {}

    async getAll(
        input?: PagedCompanyProfileResultRequestDto
    ): Promise<PagedResultDto<CompanyProfileDto>> {
        const params: any = {};
        if (input?.keyword) params.Keyword = input.keyword;
        if (input?.skipCount !== undefined) params.SkipCount = input.skipCount;
        if (input?.maxResultCount !== undefined)
            params.MaxResultCount = input.maxResultCount;

        const res: any = await firstValueFrom(
            this.http.get<any>(`${this.apiUrl}/GetAll`, { params })
        );
        const result = this.unwrap(res, 'Failed to load company profiles');
        const items = result.items || result.Items || [];
        return {
            items: (Array.isArray(items) ? items : []).map((i) => this.map(i)),
            totalCount: result.totalCount ?? result.TotalCount ?? items.length,
        };
    }

    async get(id: number): Promise<CompanyProfileDto> {
        const res: any = await firstValueFrom(
            this.http.get<any>(`${this.apiUrl}/Get`, { params: { Id: id } })
        );
        return this.map(this.unwrap(res, 'Failed to load company profile'));
    }

    async getCurrent(): Promise<CompanyProfileDto | null> {
        try {
            const res: any = await firstValueFrom(
                this.http.get<any>(`${this.apiUrl}/GetCurrent`)
            );
            const result = this.unwrap(res, 'Failed to load company profile');
            if (!result) {
                return null;
            }
            return this.map(result);
        } catch {
            return null;
        }
    }

    async create(input: CreateCompanyProfileDto): Promise<CompanyProfileDto> {
        const res: any = await firstValueFrom(
            this.http.post<any>(`${this.apiUrl}/Create`, input)
        );
        return this.map(this.unwrap(res, 'Failed to create company profile'));
    }

    async update(input: CompanyProfileDto): Promise<CompanyProfileDto> {
        const res: any = await firstValueFrom(
            this.http.put<any>(`${this.apiUrl}/Update`, {
                id: input.id,
                name: input.name,
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
        return this.map(this.unwrap(res, 'Failed to update company profile'));
    }

    async delete(id: number): Promise<void> {
        const res: any = await firstValueFrom(
            this.http.delete<any>(`${this.apiUrl}/Delete`, {
                params: { Id: id },
            })
        );
        if (res == null) return;
        this.unwrap(res, 'Failed to delete company profile');
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

    private map(item: any): CompanyProfileDto {
        return {
            id: item.id ?? item.Id,
            name: item.name ?? item.Name,
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
