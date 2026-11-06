import { Injectable } from '@angular/core';
import { Title } from '@angular/platform-browser';

/**
 * Chrome tab title + favicon (mobile + desktop).
 * Title: branch name when selected, otherwise "Al Jannat".
 * Favicon: branch logo when set, otherwise AJ fallback.
 * Storefront branch wins over staff branch while set.
 */
@Injectable({ providedIn: 'root' })
export class AppBrowserTitleService {
    static readonly APP_NAME = 'Al Jannat';
    /** PNG is more reliable than SVG for mobile Chrome / iOS home-screen icons. */
    static readonly AJ_FAVICON_PNG = 'assets/layout/images/favicon.png';
    static readonly AJ_FAVICON = AppBrowserTitleService.AJ_FAVICON_PNG;

    private staffBranchName = '';
    private storefrontBranchName = '';
    private staffBranchLogoUrl = '';
    private storefrontBranchLogoUrl = '';

    constructor(private title: Title) {}

    setStaffBranchName(name?: string | null): void {
        this.staffBranchName = (name || '').trim();
        this.applyTitle();
    }

    setStaffBranchLogoUrl(url?: string | null): void {
        this.staffBranchLogoUrl = (url || '').trim();
        this.applyFavicon();
    }

    setStorefrontBranchName(name?: string | null): void {
        this.storefrontBranchName = (name || '').trim();
        this.applyTitle();
    }

    setStorefrontBranchLogoUrl(url?: string | null): void {
        this.storefrontBranchLogoUrl = (url || '').trim();
        this.applyFavicon();
    }

    clearStorefrontBranchName(): void {
        this.storefrontBranchName = '';
        this.storefrontBranchLogoUrl = '';
        this.applyTitle();
        this.applyFavicon();
    }

    apply(): void {
        this.applyTitle();
        this.applyFavicon();
    }

    private applyTitle(): void {
        const branch =
            this.storefrontBranchName || this.staffBranchName || '';
        this.title.setTitle(branch || AppBrowserTitleService.APP_NAME);
    }

    private applyFavicon(): void {
        if (typeof document === 'undefined') {
            return;
        }

        const logo =
            this.storefrontBranchLogoUrl || this.staffBranchLogoUrl || '';
        const href = this.withCacheBust(logo || AppBrowserTitleService.AJ_FAVICON_PNG);
        const type = logo ? this.guessImageType(logo) : 'image/png';

        // Mobile browsers often keep a stale Verona favicon unless every icon link is replaced.
        document
            .querySelectorAll(
                "link[rel='icon'], link[rel='shortcut icon'], link[rel='apple-touch-icon'], link[rel='apple-touch-icon-precomposed']"
            )
            .forEach((el) => el.parentElement?.removeChild(el));

        this.appendIconLink('icon', href, type);
        this.appendIconLink('shortcut icon', href, type);
        this.appendIconLink('apple-touch-icon', href, type);
    }

    private appendIconLink(rel: string, href: string, type: string): void {
        const link = document.createElement('link');
        link.setAttribute('rel', rel);
        link.type = type;
        link.href = href;
        document.head.appendChild(link);
    }

    private withCacheBust(href: string): string {
        if (!href || href.startsWith('data:')) {
            return href;
        }
        if (/[?&]v=/.test(href)) {
            return href;
        }
        const sep = href.includes('?') ? '&' : '?';
        return `${href}${sep}v=aj2`;
    }

    private guessImageType(url: string): string {
        const lower = url.toLowerCase();
        if (lower.includes('.svg')) {
            return 'image/svg+xml';
        }
        if (lower.includes('.jpg') || lower.includes('.jpeg')) {
            return 'image/jpeg';
        }
        if (lower.includes('.webp')) {
            return 'image/webp';
        }
        if (lower.startsWith('data:image/')) {
            const end = lower.indexOf(';');
            return end > 0 ? lower.slice(0, end) : 'image/png';
        }
        return 'image/png';
    }
}
