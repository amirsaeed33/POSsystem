import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import {
    CreateOrUpdateOnlineOrderHeroSlideDto,
    OnlineOrderHeroSlideDto,
} from '../api/online-order-hero-slide';
import { environment } from '../../../environments/environment';

@Injectable({
    providedIn: 'root',
})
export class OnlineOrderHeroSlideService {
    private readonly apiUrl = `${environment.apiUrl}/api/services/app/OnlineOrderHeroSlide`;

    constructor(private http: HttpClient) {}

    async getByBranch(branchId: number): Promise<OnlineOrderHeroSlideDto[]> {
        const res: any = await firstValueFrom(
            this.http.get<any>(`${this.apiUrl}/GetByBranch`, {
                params: { branchId: String(branchId) },
            })
        );
        const result = this.unwrap(res, 'Failed to load hero slides');
        const items = result.items || result.Items || result || [];
        return (Array.isArray(items) ? items : []).map((x: any) => this.map(x));
    }

    async getActiveByBranch(branchId: number): Promise<OnlineOrderHeroSlideDto[]> {
        const res: any = await firstValueFrom(
            this.http.get<any>(`${this.apiUrl}/GetActiveByBranch`, {
                params: { branchId: String(branchId) },
            })
        );
        const result = this.unwrap(res, 'Failed to load active hero slides');
        const items = result.items || result.Items || result || [];
        return (Array.isArray(items) ? items : []).map((x: any) => this.map(x));
    }

    async createOrUpdate(
        input: CreateOrUpdateOnlineOrderHeroSlideDto
    ): Promise<OnlineOrderHeroSlideDto> {
        const res: any = await firstValueFrom(
            this.http.post<any>(`${this.apiUrl}/CreateOrUpdate`, input)
        );
        return this.map(this.unwrap(res, 'Failed to save hero slide'));
    }

    async delete(id: number): Promise<void> {
        const res: any = await firstValueFrom(
            this.http.delete<any>(`${this.apiUrl}/Delete`, {
                params: { Id: String(id) },
            })
        );
        if (res == null) {
            return;
        }
        this.unwrap(res, 'Failed to delete hero slide');
    }

    getImageUrl(path?: string | null): string {
        if (!path) {
            return '';
        }
        if (
            path.startsWith('http://') ||
            path.startsWith('https://') ||
            path.startsWith('data:')
        ) {
            return path;
        }

        const relative = path.startsWith('/') ? path : `/${path}`;
        const isHero = /\/?OnlineOrderHeroImages\//i.test(relative);

        // Production / explicit API host: load static files from the API origin.
        if (environment.apiUrl) {
            return `${environment.apiUrl}${relative}`;
        }

        // Local Angular (empty apiUrl): the /OnlineOrderHeroImages proxy is only
        // loaded when ng serve starts, so hit the API host directly for hero files.
        if (
            isHero &&
            typeof window !== 'undefined' &&
            /^(localhost|127\.0\.0\.1)$/i.test(window.location.hostname)
        ) {
            return `https://localhost:44311${relative}`;
        }

        // ngrok / same-origin: use /api controller (proxied via existing /api rule).
        if (isHero) {
            return relative.replace(
                /^\/?OnlineOrderHeroImages\//i,
                '/api/OnlineOrderHeroImages/'
            );
        }

        return relative;
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
        return res.result !== undefined ? res.result : res;
    }

    private map(item: any): OnlineOrderHeroSlideDto {
        return {
            id: item.id ?? item.Id ?? 0,
            branchId: item.branchId ?? item.BranchId ?? 0,
            sortOrder: item.sortOrder ?? item.SortOrder ?? 0,
            isActive: item.isActive ?? item.IsActive ?? true,
            badge: item.badge ?? item.Badge,
            badgeIcon: item.badgeIcon ?? item.BadgeIcon,
            titleMain: item.titleMain ?? item.TitleMain ?? '',
            titleHighlight: item.titleHighlight ?? item.TitleHighlight,
            titleEnd: item.titleEnd ?? item.TitleEnd,
            description: item.description ?? item.Description,
            offerPill: item.offerPill ?? item.OfferPill,
            rating: item.rating ?? item.Rating,
            primaryBtnText: item.primaryBtnText ?? item.PrimaryBtnText,
            primaryTarget: item.primaryTarget ?? item.PrimaryTarget ?? 'menu',
            secondaryBtnText: item.secondaryBtnText ?? item.SecondaryBtnText,
            secondaryTarget: item.secondaryTarget ?? item.SecondaryTarget ?? 'menu',
            imagePath: item.imagePath ?? item.ImagePath,
            bgImagePath: item.bgImagePath ?? item.BgImagePath,
        };
    }
}
