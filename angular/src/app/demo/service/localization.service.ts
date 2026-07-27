import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import {
    DEFAULT_LANGUAGE_NAME,
    DEFAULT_LANGUAGES,
    LanguageInfo,
    LOCALIZATION_CULTURE_COOKIE,
} from '../api/localization';
import { environment } from '../../../environments/environment';
import { AuthService } from './auth.service';

@Injectable({
    providedIn: 'root',
})
export class LocalizationService {
    private readonly userConfigurationUrl = `${environment.apiUrl}/AbpUserConfiguration/GetAll`;
    private readonly changeLanguageUrl = `${environment.apiUrl}/api/services/app/User/ChangeLanguage`;

    private languages: LanguageInfo[] = [...DEFAULT_LANGUAGES];
    private currentLanguage: LanguageInfo =
        DEFAULT_LANGUAGES.find((l) => l.name === DEFAULT_LANGUAGE_NAME) ||
        DEFAULT_LANGUAGES[0];
    private localizationValues: Record<string, Record<string, string>> = {};
    private loaded = false;
    private loadPromise: Promise<void> | null = null;

    constructor(
        private http: HttpClient,
        private authService: AuthService
    ) {}

    getLanguages(): LanguageInfo[] {
        return this.languages.filter((l) => !l.isDisabled);
    }

    getCurrentLanguage(): LanguageInfo {
        return this.currentLanguage;
    }

    /** Culture for `.AspNetCore.Culture` — defaults to English. */
    getCultureName(): string {
        return this.getCultureCookie() || DEFAULT_LANGUAGE_NAME;
    }

    getCultureHeaderValue(): string {
        const culture = this.getCultureName();
        return `c=${culture}|uic=${culture}`;
    }

    localize(key: string, sourceName = 'SmartPos'): string {
        const source = this.localizationValues[sourceName];
        if (source && source[key]) {
            return source[key];
        }
        return key;
    }

    async ensureLoaded(): Promise<void> {
        if (this.loaded) {
            return;
        }
        if (this.loadPromise) {
            return this.loadPromise;
        }
        this.loadPromise = this.fetchConfiguration().finally(() => {
            this.loadPromise = null;
        });
        return this.loadPromise;
    }

    /**
     * Match angular-old: persist via API when logged in, set culture cookie, reload.
     */
    async changeLanguage(languageName: string): Promise<void> {
        const name = (languageName || '').trim();
        if (!name || name === this.getCultureName()) {
            return;
        }

        if (this.authService.isAuthenticated()) {
            await this.persistUserLanguage(name);
        }

        this.setCultureCookie(name);
        window.location.reload();
    }

    private async persistUserLanguage(languageName: string): Promise<void> {
        const res: any = await firstValueFrom(
            this.http.post<any>(this.changeLanguageUrl, { languageName })
        );
        if (!res) {
            throw new Error('No response from server');
        }
        if (res.success === false || res.error) {
            throw new Error(
                res.error?.message ||
                    res.error?.details ||
                    'Failed to change language'
            );
        }
    }

    private async fetchConfiguration(): Promise<void> {
        try {
            const res: any = await firstValueFrom(
                this.http.get<any>(this.userConfigurationUrl)
            );
            if (!res || res.success === false || res.error) {
                this.applyFallbackLanguages();
                this.loaded = true;
                return;
            }

            const result = res.result ?? res;
            const localization =
                result.localization ?? result.Localization ?? {};
            const languagesRaw =
                localization.languages ?? localization.Languages ?? [];
            const current =
                localization.currentLanguage ?? localization.CurrentLanguage;
            const values =
                localization.values ?? localization.Values ?? {};

            const mapped = this.mapLanguages(languagesRaw);
            this.languages = mapped.length ? mapped : [...DEFAULT_LANGUAGES];

            const cookieCulture = this.getCultureCookie();
            const currentName =
                cookieCulture ||
                current?.name ||
                current?.Name ||
                DEFAULT_LANGUAGE_NAME;

            this.currentLanguage =
                this.languages.find((l) => l.name === currentName) ||
                this.languages.find((l) => l.name === DEFAULT_LANGUAGE_NAME) ||
                this.languages[0];

            this.localizationValues = this.mapLocalizationValues(values);
            this.loaded = true;
        } catch {
            this.applyFallbackLanguages();
            this.loaded = true;
        }
    }

    private applyFallbackLanguages(): void {
        this.languages = [...DEFAULT_LANGUAGES];
        const culture = this.getCultureName();
        this.currentLanguage =
            this.languages.find((l) => l.name === culture) ||
            this.languages[0];
    }

    private mapLanguages(raw: any): LanguageInfo[] {
        if (!Array.isArray(raw)) {
            return [];
        }
        return raw.map((item: any) => ({
            name: item.name ?? item.Name,
            displayName: item.displayName ?? item.DisplayName,
            icon: item.icon ?? item.Icon,
            isDisabled: !!(item.isDisabled ?? item.IsDisabled),
            isDefault: !!(item.isDefault ?? item.IsDefault),
        }));
    }

    private mapLocalizationValues(
        values: any
    ): Record<string, Record<string, string>> {
        if (!values || typeof values !== 'object') {
            return {};
        }
        const mapped: Record<string, Record<string, string>> = {};
        Object.keys(values).forEach((source) => {
            const dict = values[source];
            if (dict && typeof dict === 'object') {
                mapped[source] = { ...dict };
            }
        });
        return mapped;
    }

    private getCultureCookie(): string | null {
        const match = document.cookie.match(
            new RegExp(
                '(?:^|; )' +
                    LOCALIZATION_CULTURE_COOKIE.replace(
                        /([.$?*|{}()[\]\\/+^])/g,
                        '\\$1'
                    ) +
                    '=([^;]*)'
            )
        );
        return match ? decodeURIComponent(match[1]) : null;
    }

    private setCultureCookie(languageName: string): void {
        const expire = new Date();
        expire.setFullYear(expire.getFullYear() + 5);
        document.cookie = `${LOCALIZATION_CULTURE_COOKIE}=${encodeURIComponent(
            languageName
        )};expires=${expire.toUTCString()};path=/`;
    }
}
