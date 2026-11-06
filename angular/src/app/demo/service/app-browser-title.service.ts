import { Injectable } from '@angular/core';
import { Title } from '@angular/platform-browser';

/**
 * Chrome / browser tab title: "Al Jannat" or "Al Jannat - {branch}".
 * Storefront (online-order) branch wins over staff branch context while set.
 */
@Injectable({ providedIn: 'root' })
export class AppBrowserTitleService {
    static readonly APP_NAME = 'Al Jannat';

    private staffBranchName = '';
    private storefrontBranchName = '';

    constructor(private title: Title) {}

    setStaffBranchName(name?: string | null): void {
        this.staffBranchName = (name || '').trim();
        this.apply();
    }

    setStorefrontBranchName(name?: string | null): void {
        this.storefrontBranchName = (name || '').trim();
        this.apply();
    }

    clearStorefrontBranchName(): void {
        this.storefrontBranchName = '';
        this.apply();
    }

    apply(): void {
        const branch =
            this.storefrontBranchName || this.staffBranchName || '';
        this.title.setTitle(
            branch
                ? `${AppBrowserTitleService.APP_NAME} - ${branch}`
                : AppBrowserTitleService.APP_NAME
        );
    }
}
