import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import {
    IsTenantAvailableInput,
    IsTenantAvailableOutput,
    TenantAvailabilityState,
} from '../api/account';
import { TenantLoginInfoDto } from '../api/session';
import { environment } from '../../../environments/environment';

const TENANT_ID_COOKIE = 'Abp.TenantId';
const TENANT_INFO_KEY = 'tenantInfo';
const MULTI_TENANCY_ENABLED_KEY = 'multiTenancyEnabled';

@Injectable({
    providedIn: 'root',
})
export class TenantContextService {
    private readonly isTenantAvailableUrl = `${environment.apiUrl}/api/services/app/Account/IsTenantAvailable`;
    private readonly userConfigurationUrl = `${environment.apiUrl}/AbpUserConfiguration/GetAll`;

    private multiTenancyEnabled: boolean | null = null;
    private configPromise: Promise<void> | null = null;

    constructor(private http: HttpClient) {
        const cached = localStorage.getItem(MULTI_TENANCY_ENABLED_KEY);
        if (cached === 'true' || cached === 'false') {
            this.multiTenancyEnabled = cached === 'true';
        }
    }

    isMultiTenancyEnabled(): boolean {
        return this.multiTenancyEnabled === true;
    }

    getTenantId(): number | null {
        const raw = this.getCookie(TENANT_ID_COOKIE);
        if (!raw) {
            return null;
        }
        const id = parseInt(raw, 10);
        return Number.isFinite(id) ? id : null;
    }

    setTenantId(tenantId: number | null | undefined): void {
        if (tenantId == null) {
            this.deleteCookie(TENANT_ID_COOKIE);
            localStorage.removeItem(TENANT_INFO_KEY);
            return;
        }
        // Match ABP: ~5 year cookie lifetime
        const expire = new Date();
        expire.setFullYear(expire.getFullYear() + 5);
        this.setCookie(TENANT_ID_COOKIE, String(tenantId), expire);
    }

    getTenantInfo(): TenantLoginInfoDto | null {
        const raw = localStorage.getItem(TENANT_INFO_KEY);
        if (!raw) {
            return null;
        }
        try {
            return JSON.parse(raw);
        } catch {
            return null;
        }
    }

    setTenantInfo(tenant: TenantLoginInfoDto | null | undefined): void {
        if (!tenant?.id) {
            localStorage.removeItem(TENANT_INFO_KEY);
            return;
        }
        localStorage.setItem(
            TENANT_INFO_KEY,
            JSON.stringify({
                id: tenant.id,
                tenancyName: tenant.tenancyName,
                name: tenant.name,
            })
        );
    }

    async ensureMultiTenancyLoaded(): Promise<void> {
        if (this.multiTenancyEnabled !== null && !this.configPromise) {
            return;
        }
        if (this.configPromise) {
            return this.configPromise;
        }

        this.configPromise = this.fetchUserConfiguration().finally(() => {
            this.configPromise = null;
        });
        return this.configPromise;
    }

    async isTenantAvailable(
        input: IsTenantAvailableInput
    ): Promise<IsTenantAvailableOutput> {
        const res: any = await firstValueFrom(
            this.http.post<any>(this.isTenantAvailableUrl, {
                tenancyName: input.tenancyName,
            })
        );

        if (!res) {
            throw new Error('No response from server');
        }
        if (res.success === false || res.error) {
            throw new Error(
                res.error?.message ||
                    res.error?.details ||
                    'Failed to resolve tenant'
            );
        }

        const result = res.result ?? res;
        const state = result.state ?? result.State;
        const tenantId = result.tenantId ?? result.TenantId;

        return {
            state: Number(state) as TenantAvailabilityState,
            tenantId: tenantId != null ? Number(tenantId) : undefined,
        };
    }

    /**
     * Resolve tenancy name like angular-old (Available → set cookie).
     * Returns true if the tenant cookie changed.
     */
    async resolveTenancyName(tenancyName: string): Promise<{
        changed: boolean;
        state: TenantAvailabilityState;
    }> {
        const trimmed = (tenancyName || '').trim();
        if (!trimmed) {
            const hadTenant = this.getTenantId() != null;
            this.setTenantId(undefined);
            return {
                changed: hadTenant,
                state: TenantAvailabilityState.Available,
            };
        }

        const result = await this.isTenantAvailable({ tenancyName: trimmed });
        if (result.state === TenantAvailabilityState.Available) {
            const previous = this.getTenantId();
            const next = result.tenantId ?? null;
            this.setTenantId(next);
            return {
                changed: previous !== next,
                state: result.state,
            };
        }

        return { changed: false, state: result.state };
    }

    private async fetchUserConfiguration(): Promise<void> {
        const res: any = await firstValueFrom(
            this.http.get<any>(this.userConfigurationUrl)
        );
        if (!res) {
            throw new Error('No response from user configuration');
        }
        if (res.success === false || res.error) {
            throw new Error(
                res.error?.message ||
                    res.error?.details ||
                    'Failed to load multi-tenancy configuration'
            );
        }

        const result = res.result ?? res;
        const multiTenancy = result.multiTenancy ?? result.MultiTenancy ?? {};
        const enabled =
            multiTenancy.isEnabled ?? multiTenancy.IsEnabled ?? false;
        this.multiTenancyEnabled = !!enabled;
        localStorage.setItem(
            MULTI_TENANCY_ENABLED_KEY,
            String(this.multiTenancyEnabled)
        );

        const session = result.session ?? result.Session ?? {};
        const tenantId = session.tenantId ?? session.TenantId;
        if (tenantId != null && this.getTenantId() == null) {
            this.setTenantId(Number(tenantId));
        }
    }

    private getCookie(name: string): string | null {
        const match = document.cookie.match(
            new RegExp(
                '(?:^|; )' +
                    name.replace(/([.$?*|{}()[\]\\/+^])/g, '\\$1') +
                    '=([^;]*)'
            )
        );
        return match ? decodeURIComponent(match[1]) : null;
    }

    private setCookie(name: string, value: string, expireDate: Date): void {
        document.cookie = `${name}=${encodeURIComponent(
            value
        )};expires=${expireDate.toUTCString()};path=/`;
    }

    private deleteCookie(name: string): void {
        document.cookie = `${name}=;expires=Thu, 01 Jan 1970 00:00:00 GMT;path=/`;
    }
}
