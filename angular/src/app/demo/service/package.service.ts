import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { environment } from 'src/environments/environment';
import {
    CreatePackageDto,
    PackageDto,
    PagedPackageResultRequestDto,
} from '../api/package';

@Injectable({ providedIn: 'root' })
export class PackageService {
    private readonly apiUrl = `${environment.apiUrl}/api/services/app/Package`;

    constructor(private http: HttpClient) {}

    /** Public landing page — active packages only (anonymous). */
    async getPublicPackages(): Promise<PackageDto[]> {
        const res: any = await firstValueFrom(
            this.http.get<any>(`${this.apiUrl}/GetPublicPackages`)
        );
        const result = this.unwrap(res, 'Failed to load packages');
        const items = Array.isArray(result) ? result : result?.items || [];
        return (Array.isArray(items) ? items : []).map((x: any) => this.mapDto(x));
    }

    async getAll(
        input: PagedPackageResultRequestDto = {}
    ): Promise<{ totalCount: number; items: PackageDto[] }> {
        const params: any = {
            MaxResultCount: input.maxResultCount ?? 100,
            SkipCount: input.skipCount ?? 0,
        };
        if (input.keyword?.trim()) {
            params.Keyword = input.keyword.trim();
        }
        if (input.isActive != null) {
            params.IsActive = input.isActive;
        }
        if (input.sorting) {
            params.Sorting = input.sorting;
        }

        const res: any = await firstValueFrom(
            this.http.get<any>(`${this.apiUrl}/GetAll`, { params })
        );
        const result = this.unwrap(res, 'Failed to load packages');
        const items = (result?.items || []).map((x: any) => this.mapDto(x));
        return { totalCount: result?.totalCount ?? items.length, items };
    }

    async create(input: CreatePackageDto): Promise<PackageDto> {
        const res: any = await firstValueFrom(
            this.http.post<any>(`${this.apiUrl}/Create`, input)
        );
        return this.mapDto(this.unwrap(res, 'Failed to create package'));
    }

    async update(input: PackageDto): Promise<PackageDto> {
        const res: any = await firstValueFrom(
            this.http.put<any>(`${this.apiUrl}/Update`, input)
        );
        return this.mapDto(this.unwrap(res, 'Failed to update package'));
    }

    async delete(id: number): Promise<void> {
        const res: any = await firstValueFrom(
            this.http.delete<any>(`${this.apiUrl}/Delete`, {
                params: { Id: id },
            })
        );
        if (res == null) return;
        this.unwrap(res, 'Failed to delete package');
    }

    private mapDto(raw: any): PackageDto {
        if (!raw) {
            return { name: '', price: '' };
        }
        return {
            id: raw.id ?? raw.Id,
            name: raw.name ?? raw.Name ?? '',
            price: raw.price ?? raw.Price ?? '',
            period: raw.period ?? raw.Period,
            blurb: raw.blurb ?? raw.Blurb,
            features: raw.features ?? raw.Features,
            ctaLabel: raw.ctaLabel ?? raw.CtaLabel,
            isFeatured: raw.isFeatured ?? raw.IsFeatured ?? false,
            contactViaWhatsApp:
                raw.contactViaWhatsApp ?? raw.ContactViaWhatsApp ?? false,
            isActive: raw.isActive ?? raw.IsActive ?? true,
            sortOrder: raw.sortOrder ?? raw.SortOrder ?? 0,
        };
    }

    private unwrap(res: any, fallback: string): any {
        if (!res) throw new Error('No response from server');
        if (res.success === false || res.error) {
            throw new Error(
                res.error?.message || res.error?.details || fallback
            );
        }
        return res.result ?? res;
    }
}
