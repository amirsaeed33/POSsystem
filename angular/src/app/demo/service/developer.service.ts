import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { environment } from 'src/environments/environment';
import {
    CreateDeveloperDto,
    DeveloperDto,
    PagedDeveloperResultRequestDto,
} from '../api/developer';

@Injectable({ providedIn: 'root' })
export class DeveloperService {
    private readonly apiUrl = `${environment.apiUrl}/api/services/app/Developer`;

    constructor(private http: HttpClient) {}

    async getAll(
        input: PagedDeveloperResultRequestDto = {}
    ): Promise<{ totalCount: number; items: DeveloperDto[] }> {
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
        const result = this.unwrap(res, 'Failed to load developers');
        const items = (result?.items || []).map((x: any) => this.mapDto(x));
        return { totalCount: result?.totalCount ?? items.length, items };
    }

    async get(id: number): Promise<DeveloperDto> {
        const res: any = await firstValueFrom(
            this.http.get<any>(`${this.apiUrl}/Get`, { params: { Id: id } })
        );
        return this.mapDto(this.unwrap(res, 'Failed to load developer'));
    }

    async create(input: CreateDeveloperDto): Promise<DeveloperDto> {
        const res: any = await firstValueFrom(
            this.http.post<any>(`${this.apiUrl}/Create`, input)
        );
        return this.mapDto(this.unwrap(res, 'Failed to create developer'));
    }

    async update(input: DeveloperDto): Promise<DeveloperDto> {
        const res: any = await firstValueFrom(
            this.http.put<any>(`${this.apiUrl}/Update`, input)
        );
        return this.mapDto(this.unwrap(res, 'Failed to update developer'));
    }

    async delete(id: number): Promise<void> {
        const res: any = await firstValueFrom(
            this.http.delete<any>(`${this.apiUrl}/Delete`, {
                params: { Id: id },
            })
        );
        if (res == null) return;
        this.unwrap(res, 'Failed to delete developer');
    }

    getImageUrl(imagePath?: string | null): string {
        if (!imagePath) return '';
        if (imagePath.startsWith('http') || imagePath.startsWith('data:')) {
            return imagePath;
        }
        return `${environment.apiUrl}${imagePath.startsWith('/') ? '' : '/'}${imagePath}`;
    }

    private mapDto(raw: any): DeveloperDto {
        if (!raw) {
            return { name: '' };
        }
        return {
            id: raw.id ?? raw.Id,
            name: raw.name ?? raw.Name ?? '',
            email: raw.email ?? raw.Email,
            phoneNumber: raw.phoneNumber ?? raw.PhoneNumber,
            description: raw.description ?? raw.Description,
            profileImagePath: raw.profileImagePath ?? raw.ProfileImagePath,
            backgroundImagePath:
                raw.backgroundImagePath ?? raw.BackgroundImagePath,
            education: raw.education ?? raw.Education,
            tags: raw.tags ?? raw.Tags,
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
