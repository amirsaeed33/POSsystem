import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import { TenantContextService } from './tenant-context.service';

export interface StorefrontByHostDto {
    branchId: number;
    tenantId?: number | null;
    branchName?: string;
    storefrontTitle?: string;
    customDomain?: string;
    address?: string;
    phone?: string;
    imagePath?: string;
    isRestaurant?: boolean;
}

const PLATFORM_HOSTS = new Set([
    'localhost',
    '127.0.0.1',
    'aljannatapp.com',
    'www.aljannatapp.com',
]);

/**
 * Resolves custom-domain hosts to a single branch storefront.
 * On mapped hosts the app skips the multi-branch picker.
 */
@Injectable({ providedIn: 'root' })
export class StorefrontHostService {
    private readonly apiUrl = `${environment.apiUrl}/api/services/app/CustomerOrder`;
    private resolved: StorefrontByHostDto | null | undefined;
    private resolvePromise: Promise<StorefrontByHostDto | null> | null = null;

    constructor(
        private http: HttpClient,
        private tenantContext: TenantContextService
    ) {}

    /** True when the browser host is a client custom domain (not platform/localhost). */
    isCustomHost(): boolean {
        return !this.isPlatformHost(this.currentHost());
    }

    currentHost(): string {
        if (typeof window === 'undefined' || !window.location?.hostname) {
            return '';
        }
        return window.location.hostname.trim().toLowerCase();
    }

    getCached(): StorefrontByHostDto | null {
        return this.resolved ?? null;
    }

    /** Force-mapped branch for the current custom host, if any. */
    getForcedBranchId(): number | null {
        const id = this.resolved?.branchId;
        return id && id > 0 ? id : null;
    }

    getStorefrontTitle(): string {
        return (
            (this.resolved?.storefrontTitle || this.resolved?.branchName || '').trim()
        );
    }

    /**
     * Call once on app boot. Sets tenant cookie when a mapping is found.
     */
    async ensureResolved(): Promise<StorefrontByHostDto | null> {
        if (this.resolved !== undefined) {
            return this.resolved;
        }
        if (this.resolvePromise) {
            return this.resolvePromise;
        }

        this.resolvePromise = this.resolveInternal().finally(() => {
            this.resolvePromise = null;
        });
        return this.resolvePromise;
    }

    private async resolveInternal(): Promise<StorefrontByHostDto | null> {
        const host = this.currentHost();
        if (!host || this.isPlatformHost(host)) {
            this.resolved = null;
            return null;
        }

        try {
            const res: any = await firstValueFrom(
                this.http.get<any>(`${this.apiUrl}/GetStorefrontByHost`, {
                    params: { host },
                })
            );
            const result = this.unwrap(res);
            if (!result) {
                this.resolved = null;
                return null;
            }

            const dto: StorefrontByHostDto = {
                branchId: Number(result.branchId ?? result.BranchId ?? 0),
                tenantId: result.tenantId ?? result.TenantId ?? null,
                branchName: (result.branchName ?? result.BranchName ?? '').toString(),
                storefrontTitle: (
                    result.storefrontTitle ??
                    result.StorefrontTitle ??
                    result.branchName ??
                    result.BranchName ??
                    ''
                ).toString(),
                customDomain: (
                    result.customDomain ??
                    result.CustomDomain ??
                    ''
                ).toString(),
                address: (result.address ?? result.Address ?? '').toString(),
                phone: (result.phone ?? result.Phone ?? '').toString(),
                imagePath: (result.imagePath ?? result.ImagePath ?? '').toString(),
                isRestaurant: !!(result.isRestaurant ?? result.IsRestaurant),
            };

            if (!dto.branchId || dto.branchId <= 0) {
                this.resolved = null;
                return null;
            }

            if (dto.tenantId != null && Number(dto.tenantId) > 0) {
                this.tenantContext.setTenantId(Number(dto.tenantId));
            }

            this.resolved = dto;
            return dto;
        } catch {
            this.resolved = null;
            return null;
        }
    }

    private isPlatformHost(host: string): boolean {
        const h = (host || '').trim().toLowerCase().replace(/^www\./, '');
        if (!h) {
            return true;
        }
        if (PLATFORM_HOSTS.has(h) || PLATFORM_HOSTS.has(`www.${h}`)) {
            return true;
        }
        return (
            h.endsWith('.siteasp.net') ||
            h.endsWith('.runasp.net') ||
            h.endsWith('.ngrok-free.dev') ||
            h.endsWith('.ngrok-free.app') ||
            h.endsWith('.ngrok.io')
        );
    }

    private unwrap(res: any): any {
        if (!res) {
            return null;
        }
        if (res.success === false) {
            return null;
        }
        return res.result ?? res.Result ?? res;
    }
}
