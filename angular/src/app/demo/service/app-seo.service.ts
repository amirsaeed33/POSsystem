import { Injectable, Inject } from '@angular/core';
import { DOCUMENT } from '@angular/common';
import { Meta, Title } from '@angular/platform-browser';
import { environment } from 'src/environments/environment';

export interface SeoPageOptions {
    title?: string;
    description?: string;
    path?: string;
    image?: string;
    /** When false, search engines should not index this page. */
    index?: boolean;
    jsonLd?: Record<string, unknown> | Record<string, unknown>[] | null;
}

/**
 * Page SEO: title, description, canonical, Open Graph, robots, JSON-LD.
 * Public storefront pages should call updatePage; private app areas use noindex.
 */
@Injectable({ providedIn: 'root' })
export class AppSeoService {
    static readonly SITE_NAME = 'Al Jannat';
    static readonly DEFAULT_TITLE = 'Al Jannat | Online Food Order & Delivery';
    static readonly DEFAULT_DESCRIPTION =
        'Order from Al Jannat online — browse the menu, place delivery or pickup orders, and track your food in real time.';
    static readonly DEFAULT_IMAGE = '/assets/layout/images/logo-light.png';

    private readonly jsonLdId = 'app-seo-jsonld';
    /** When set (custom-domain storefront), SEO titles use the client brand. */
    private brandNameOverride: string | null = null;

    constructor(
        private title: Title,
        private meta: Meta,
        @Inject(DOCUMENT) private document: Document
    ) {}

    get siteName(): string {
        return (this.brandNameOverride || AppSeoService.SITE_NAME).trim();
    }

    /** Apply client brand for custom-domain storefronts (clears with null). */
    setBrandOverride(name: string | null | undefined): void {
        const trimmed = (name || '').trim();
        this.brandNameOverride = trimmed || null;
    }

    get siteOrigin(): string {
        // Always prefer the browser origin so custom domains get correct canonicals.
        if (typeof window !== 'undefined' && window.location?.origin) {
            return window.location.origin;
        }
        const api = (environment.apiUrl || '').replace(/\/$/, '');
        if (api.startsWith('http')) {
            return api;
        }
        return 'https://aljannatapp.com';
    }

    /** Default public landing SEO (also matches index.html crawler snapshot). */
    applyDefaults(): void {
        this.updatePage({
            title: AppSeoService.DEFAULT_TITLE,
            description: AppSeoService.DEFAULT_DESCRIPTION,
            path: '/',
            index: true,
            jsonLd: this.restaurantJsonLd(),
        });
    }

    updatePage(options: SeoPageOptions): void {
        const pageTitle = (options.title || AppSeoService.DEFAULT_TITLE).trim();
        const description = (
            options.description || AppSeoService.DEFAULT_DESCRIPTION
        ).trim();
        const path = this.normalizePath(options.path);
        const canonical = `${this.siteOrigin}${path === '/' ? '/' : path}`;
        const image = this.toAbsoluteUrl(
            options.image || AppSeoService.DEFAULT_IMAGE
        );
        const index = options.index !== false;

        this.title.setTitle(pageTitle);

        this.meta.updateTag({ name: 'description', content: description });
        this.meta.updateTag({
            name: 'robots',
            content: index ? 'index, follow' : 'noindex, nofollow',
        });
        this.meta.updateTag({
            name: 'keywords',
            content:
                'Al Jannat, online food order, food delivery, restaurant menu, order online, takeaway',
        });

        this.setLinkCanonical(canonical);

        this.meta.updateTag({ property: 'og:type', content: 'website' });
        this.meta.updateTag({
            property: 'og:site_name',
            content: this.siteName,
        });
        this.meta.updateTag({ property: 'og:title', content: pageTitle });
        this.meta.updateTag({
            property: 'og:description',
            content: description,
        });
        this.meta.updateTag({ property: 'og:url', content: canonical });
        this.meta.updateTag({ property: 'og:image', content: image });

        this.meta.updateTag({
            name: 'twitter:card',
            content: 'summary_large_image',
        });
        this.meta.updateTag({ name: 'twitter:title', content: pageTitle });
        this.meta.updateTag({
            name: 'twitter:description',
            content: description,
        });
        this.meta.updateTag({ name: 'twitter:image', content: image });

        if (options.jsonLd === null) {
            this.removeJsonLd();
        } else if (options.jsonLd) {
            this.setJsonLd(options.jsonLd);
        }
    }

    /** Storefront menu for a selected branch. */
    setStorefrontBranch(opts: {
        branchName?: string | null;
        branchAddress?: string | null;
        branchPhone?: string | null;
        path?: string;
        image?: string | null;
        brandTitle?: string | null;
    }): void {
        const brand = (opts.brandTitle || this.siteName || '').trim();
        const name = (opts.branchName || brand || '').trim();
        const title = name
            ? `${name} | Order Online`
            : AppSeoService.DEFAULT_TITLE;
        const bits = [
            name ? `Order online from ${name}` : 'Order food online',
            opts.branchAddress ? `at ${opts.branchAddress.trim()}` : '',
            '— delivery or pickup, live order tracking.',
        ].filter(Boolean);
        this.updatePage({
            title,
            description: bits.join(' '),
            path: opts.path || '/online-order',
            image: opts.image || undefined,
            index: true,
            jsonLd: this.restaurantJsonLd({
                name: name || brand || AppSeoService.SITE_NAME,
                address: opts.branchAddress,
                telephone: opts.branchPhone,
                image: opts.image,
            }),
        });
    }

    setProduct(opts: {
        productName: string;
        description?: string | null;
        branchName?: string | null;
        path?: string;
        image?: string | null;
        price?: number | null;
        currency?: string | null;
    }): void {
        const product = (opts.productName || 'Menu item').trim();
        const branch = (opts.branchName || '').trim();
        const brand = this.siteName;
        const title = branch
            ? `${product} | ${branch}`
            : `${product} | ${brand}`;
        const description = (
            opts.description ||
            `Order ${product}${branch ? ` from ${branch}` : ''} online.`
        ).trim();

        const jsonLd: Record<string, unknown> = {
            '@context': 'https://schema.org',
            '@type': 'Product',
            name: product,
            description,
            brand: { '@type': 'Brand', name: brand },
        };
        if (opts.image) {
            jsonLd['image'] = this.toAbsoluteUrl(opts.image);
        }
        if (opts.price != null && !isNaN(Number(opts.price))) {
            jsonLd['offers'] = {
                '@type': 'Offer',
                priceCurrency: (opts.currency || 'PKR').trim() || 'PKR',
                price: Number(opts.price),
                availability: 'https://schema.org/InStock',
                url: `${this.siteOrigin}${this.normalizePath(opts.path || '/online-order')}`,
            };
        }

        this.updatePage({
            title,
            description,
            path: opts.path || '/online-order',
            image: opts.image || undefined,
            index: true,
            jsonLd,
        });
    }

    /** Private staff / account / order-history pages. */
    setNoIndex(title?: string): void {
        this.updatePage({
            title: title || AppSeoService.SITE_NAME,
            description: AppSeoService.DEFAULT_DESCRIPTION,
            path:
                typeof window !== 'undefined' ? window.location.pathname : '/',
            index: false,
            jsonLd: null,
        });
    }

    restaurantJsonLd(extra?: {
        name?: string;
        address?: string | null;
        telephone?: string | null;
        image?: string | null;
    }): Record<string, unknown> {
        const name = (extra?.name || this.siteName).trim();
        const data: Record<string, unknown> = {
            '@context': 'https://schema.org',
            '@type': 'Restaurant',
            name,
            url: this.siteOrigin + '/',
            image: this.toAbsoluteUrl(
                extra?.image || AppSeoService.DEFAULT_IMAGE
            ),
            servesCuisine: ['Pakistani', 'Fast Food', 'Pizza', 'BBQ'],
            priceRange: '$$',
            potentialAction: {
                '@type': 'OrderAction',
                target: `${this.siteOrigin}/online-order`,
            },
        };
        const phone = (extra?.telephone || '').trim();
        if (phone) {
            data['telephone'] = phone;
        }
        const address = (extra?.address || '').trim();
        if (address) {
            data['address'] = {
                '@type': 'PostalAddress',
                streetAddress: address,
            };
        }
        return data;
    }

    private normalizePath(path?: string): string {
        if (!path || path === '/') {
            return '/';
        }
        const clean = path.split('?')[0].split('#')[0];
        return clean.startsWith('/') ? clean : `/${clean}`;
    }

    private toAbsoluteUrl(url: string): string {
        const value = (url || '').trim();
        if (!value) {
            return `${this.siteOrigin}${AppSeoService.DEFAULT_IMAGE}`;
        }
        if (/^https?:\/\//i.test(value) || value.startsWith('data:')) {
            return value;
        }
        if (value.startsWith('//')) {
            return `https:${value}`;
        }
        return `${this.siteOrigin}${value.startsWith('/') ? value : '/' + value}`;
    }

    private setLinkCanonical(href: string): void {
        let link = this.document.querySelector(
            "link[rel='canonical']"
        ) as HTMLLinkElement | null;
        if (!link) {
            link = this.document.createElement('link');
            link.setAttribute('rel', 'canonical');
            this.document.head.appendChild(link);
        }
        link.href = href;
    }

    private setJsonLd(
        data: Record<string, unknown> | Record<string, unknown>[]
    ): void {
        this.removeJsonLd();
        const script = this.document.createElement('script');
        script.type = 'application/ld+json';
        script.id = this.jsonLdId;
        script.text = JSON.stringify(data);
        this.document.head.appendChild(script);
    }

    private removeJsonLd(): void {
        this.document.getElementById(this.jsonLdId)?.remove();
    }
}
