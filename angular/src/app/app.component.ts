import { Component, OnDestroy, OnInit } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { PrimeNGConfig } from 'primeng/api';
import { filter, Subscription } from 'rxjs';
import { AppBrowserTitleService } from './demo/service/app-browser-title.service';
import { AppSeoService } from './demo/service/app-seo.service';
import { BranchContextService } from './demo/service/branch-context.service';
import { BranchService } from './demo/service/branch.service';
import { StorefrontHostService } from './demo/service/storefront-host.service';

@Component({
    selector: 'app-root',
    templateUrl: './app.component.html',
    styleUrls: ['./app.component.scss']
})
export class AppComponent implements OnInit, OnDestroy {
    private branchSub?: Subscription;
    private routerSub?: Subscription;

    constructor(
        private primengConfig: PrimeNGConfig,
        private branchContext: BranchContextService,
        private branchService: BranchService,
        private browserTitle: AppBrowserTitleService,
        private seo: AppSeoService,
        private router: Router,
        private storefrontHost: StorefrontHostService
    ) { }

    ngOnInit(): void {
        this.primengConfig.ripple = true;
        this.primengConfig.zIndex = {
            modal: 1200,
            overlay: 1000,
            menu: 1000,
            tooltip: 1100
        };

        this.seo.applyDefaults();
        this.browserTitle.apply();
        void this.bootstrapCustomDomainStorefront();

        this.branchSub = this.branchContext.currentBranch$.subscribe((branch) => {
            this.browserTitle.setStaffBranchName(branch?.name);
            const logoUrl = branch?.imagePath
                ? this.branchService.getImageUrl(branch.imagePath)
                : '';
            this.browserTitle.setStaffBranchLogoUrl(logoUrl || null);
        });

        this.routerSub = this.router.events
            .pipe(filter((e): e is NavigationEnd => e instanceof NavigationEnd))
            .subscribe((e) => this.applyRouteSeo(e.urlAfterRedirects || e.url));
    }

    ngOnDestroy(): void {
        this.branchSub?.unsubscribe();
        this.routerSub?.unsubscribe();
    }

    private async bootstrapCustomDomainStorefront(): Promise<void> {
        const storefront = await this.storefrontHost.ensureResolved();
        if (!storefront?.branchId) {
            return;
        }

        const brand = (
            storefront.storefrontTitle ||
            storefront.branchName ||
            ''
        ).trim();
        if (brand) {
            this.seo.setBrandOverride(brand);
            this.browserTitle.setStorefrontBranchName(brand);
            this.seo.setStorefrontBranch({
                branchName: brand,
                brandTitle: brand,
                branchAddress: storefront.address,
                branchPhone: storefront.phone,
                path: '/online-order',
                image: storefront.imagePath || null,
            });
        }

        // Custom client domains only: product landing stays on aljannatapp.com `/`.
        const path = (this.router.url || '').split('?')[0];
        const shouldForceStorefront =
            path === '/' ||
            path === '/home' ||
            path === '/online-order' ||
            path.startsWith('/online-order/product');
        if (!shouldForceStorefront) {
            return;
        }

        await this.router.navigate(['/online-order'], {
            queryParams: { branchId: storefront.branchId },
            queryParamsHandling: 'merge',
            replaceUrl: true,
        });
    }

    private applyRouteSeo(url: string): void {
        const path = (url || '').split('?')[0];
        const brand = this.storefrontHost.getStorefrontTitle();

        // Product landing — LandingComponent owns SEO on platform host.
        if (path === '/product' || path.startsWith('/product/')) {
            return;
        }

        if (path === '/' || path === '/home') {
            if (brand) {
                // Custom domain mapped to a branch: storefront SEO, not product marketing.
                this.seo.setStorefrontBranch({
                    branchName: brand,
                    brandTitle: brand,
                    path: '/online-order',
                });
            }
            return;
        }

        // Public storefront — components set richer branch/product SEO.
        if (path === '/online-order' || path.startsWith('/online-order/product')) {
            if (path === '/online-order') {
                if (brand) {
                    this.seo.setStorefrontBranch({
                        branchName: brand,
                        brandTitle: brand,
                        path: '/online-order',
                    });
                } else {
                    this.seo.updatePage({
                        title: AppSeoService.DEFAULT_TITLE,
                        description: AppSeoService.DEFAULT_DESCRIPTION,
                        path: '/online-order',
                        index: true,
                        jsonLd: this.seo.restaurantJsonLd(),
                    });
                }
            }
            return;
        }

        // Personal order history should not be indexed.
        if (path.startsWith('/online-order/history') || path.startsWith('/online-order/track')) {
            this.seo.setNoIndex(
                brand ? `My Orders | ${brand}` : 'My Orders | Al Jannat'
            );
            return;
        }

        // Staff / auth / admin app — keep out of Google.
        this.seo.setNoIndex(brand || 'Al Jannat');
    }
}
