import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';

const GRANTED_PERMISSIONS_KEY = 'grantedPermissions';

@Injectable({
    providedIn: 'root',
})
export class PermissionService {
    private readonly userConfigurationUrl = `${environment.apiUrl}/AbpUserConfiguration/GetAll`;
    private grantedPermissions = new Set<string>();
    private fetchedThisSession = false;
    private loadPromise: Promise<void> | null = null;

    constructor(private http: HttpClient) {
        this.hydrateFromStorage();
    }

    isGranted(permissionName?: string | null): boolean {
        if (!permissionName) {
            return true;
        }
        return this.grantedPermissions.has(permissionName);
    }

    clear(): void {
        this.grantedPermissions.clear();
        this.fetchedThisSession = false;
        this.loadPromise = null;
        localStorage.removeItem(GRANTED_PERMISSIONS_KEY);
    }

    /** Load from API once per session (or use cache until fetch succeeds). */
    async ensureLoaded(): Promise<void> {
        if (this.fetchedThisSession) {
            return;
        }
        if (!this.hasToken()) {
            this.clear();
            this.fetchedThisSession = true;
            return;
        }
        try {
            await this.load();
        } catch {
            if (!this.grantedPermissions.size) {
                throw new Error('Failed to load permissions');
            }
            this.fetchedThisSession = true;
        }
    }

    async load(): Promise<void> {
        if (!this.hasToken()) {
            this.clear();
            this.fetchedThisSession = true;
            return;
        }

        if (this.loadPromise) {
            return this.loadPromise;
        }

        this.loadPromise = this.fetchAndApply().finally(() => {
            this.loadPromise = null;
        });

        return this.loadPromise;
    }

    private async fetchAndApply(): Promise<void> {
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
                    'Failed to load permissions'
            );
        }

        const result = res.result ?? res;
        const auth = result.auth ?? result.Auth ?? {};
        const granted =
            auth.grantedPermissions ?? auth.GrantedPermissions ?? {};

        const names = this.normalizeGrantedPermissions(granted);
        this.grantedPermissions = new Set(names);
        this.persist(names);
        this.fetchedThisSession = true;
    }

    private normalizeGrantedPermissions(granted: any): string[] {
        if (!granted) {
            return [];
        }
        if (Array.isArray(granted)) {
            return granted.filter((name) => typeof name === 'string' && !!name);
        }
        if (typeof granted === 'object') {
            return Object.keys(granted).filter((name) => {
                const value = granted[name];
                return value !== false && value !== 'false' && value != null;
            });
        }
        return [];
    }

    private persist(names: string[]): void {
        localStorage.setItem(GRANTED_PERMISSIONS_KEY, JSON.stringify(names));
    }

    private hydrateFromStorage(): void {
        const raw = localStorage.getItem(GRANTED_PERMISSIONS_KEY);
        if (!raw) {
            return;
        }
        try {
            const names = JSON.parse(raw);
            if (Array.isArray(names)) {
                this.grantedPermissions = new Set(
                    names.filter((name) => typeof name === 'string')
                );
            }
        } catch {
            localStorage.removeItem(GRANTED_PERMISSIONS_KEY);
        }
    }

    private hasToken(): boolean {
        return !!localStorage.getItem('accessToken');
    }
}
