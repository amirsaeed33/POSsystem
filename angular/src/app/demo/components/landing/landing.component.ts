import { Component, HostListener, OnDestroy, OnInit } from '@angular/core';
import { DeveloperDto } from 'src/app/demo/api/developer';
import { AppSeoService } from 'src/app/demo/service/app-seo.service';
import { DeveloperService } from 'src/app/demo/service/developer.service';

type LandingRevealKey =
    | 'hero'
    | 'product'
    | 'mobile'
    | 'how'
    | 'pricing'
    | 'developers'
    | 'cta'
    | 'footer';

@Component({
    selector: 'app-landing',
    templateUrl: './landing.component.html',
    styleUrls: ['./landing.component.scss'],
})
export class LandingComponent implements OnInit, OnDestroy {
    readonly currentYear = new Date().getFullYear();

    scrolled = false;
    developers: DeveloperDto[] = [];
    developersLoading = false;

    visible: Record<LandingRevealKey, boolean> = {
        hero: false,
        product: false,
        mobile: false,
        how: false,
        pricing: false,
        developers: false,
        cta: false,
        footer: false,
    };

    readonly plans = [
        {
            name: 'Starter',
            price: '4,999',
            period: '/ month',
            blurb: 'Single shop POS + online menu',
            features: [
                '1 branch / location',
                'POS billing & stock',
                'Online order page',
                'Basic sales reports',
                'Email support',
            ],
            cta: 'Get Starter',
            featured: false,
        },
        {
            name: 'Business',
            price: '9,999',
            period: '/ month',
            blurb: 'Restaurant + delivery operations',
            features: [
                'Up to 3 branches',
                'Kitchen KOT & tables',
                'Riders & delivery zones',
                'Custom domain storefront',
                'Priority WhatsApp support',
            ],
            cta: 'Most popular',
            featured: true,
        },
        {
            name: 'Enterprise',
            price: 'Custom',
            period: '',
            blurb: 'Multi-tenant & brand networks',
            features: [
                'Unlimited branches',
                'Host catalog & approvals',
                'White-label domains',
                'Training & onboarding',
                'Dedicated support',
            ],
            cta: 'Talk to us',
            featured: false,
        },
    ];

    readonly steps = [
        {
            icon: 'pi-shop',
            title: 'Create your business',
            text: 'Sign up, add your shop, and invite cashiers or waiters in minutes.',
        },
        {
            icon: 'pi-list',
            title: 'Build the menu',
            text: 'Products, deals, modifiers, and photos — ready for POS and online.',
        },
        {
            icon: 'pi-shopping-cart',
            title: 'Take orders everywhere',
            text: 'Counter, tables, takeaway, and customer online orders in one flow.',
        },
        {
            icon: 'pi-send',
            title: 'Deliver & grow',
            text: 'Assign riders, track orders, and grow with your own domain & SEO.',
        },
    ];

    readonly modules = [
        {
            icon: 'pi-desktop',
            title: 'Smart POS',
            text: 'Fast billing, discounts, tax, and offline-friendly counter sales.',
        },
        {
            icon: 'pi-table',
            title: 'Restaurant floor',
            text: 'Tables, KOT, kitchen screens, and takeaway payment alarms.',
        },
        {
            icon: 'pi-globe',
            title: 'Online storefront',
            text: 'Branded ordering page — or a custom domain for each client.',
        },
        {
            icon: 'pi-chart-bar',
            title: 'Delivery & reports',
            text: 'Riders, zones, stock, and day-end numbers your owner can trust.',
        },
    ];

    readonly mobileApps = [
        {
            icon: 'pi-shopping-bag',
            title: 'Customer',
            text: 'Browse the menu, place orders, and track delivery from the phone.',
        },
        {
            icon: 'pi-send',
            title: 'Rider',
            text: 'Accept deliveries, navigate to customers, and update order status on the go.',
        },
        {
            icon: 'pi-users',
            title: 'Waiter',
            text: 'Take table orders, send KOT to kitchen, and keep the floor moving.',
        },
        {
            icon: 'pi-cog',
            title: 'Admin',
            text: 'Manage branches, staff, stock, and sales from a mobile admin app.',
        },
    ];

    private observer?: IntersectionObserver;

    constructor(
        private seo: AppSeoService,
        private developerService: DeveloperService
    ) {}

    get primaryDeveloper(): DeveloperDto | null {
        return this.developers.length ? this.developers[0] : null;
    }

    get contactWhatsapp(): string {
        return this.whatsappHref(this.primaryDeveloper?.phoneNumber);
    }

    ngOnInit(): void {
        this.seo.updatePage({
            title: 'Al Jannat | Smart POS, Online Orders & Restaurant Software',
            description:
                'Al Jannat SmartPos — POS, kitchen, online ordering, riders, and custom domains for restaurants and retail. Simple pricing. Local support.',
            path: '/product',
            index: false,
            jsonLd: {
                '@context': 'https://schema.org',
                '@type': 'SoftwareApplication',
                name: 'Al Jannat',
                applicationCategory: 'BusinessApplication',
                operatingSystem: 'Web',
                offers: {
                    '@type': 'Offer',
                    priceCurrency: 'PKR',
                    price: '4999',
                },
            },
        });

        // Hero should animate left → right immediately on first paint.
        this.visible.hero = true;

        void this.loadDevelopers();
        setTimeout(() => this.setupReveal(), 40);
    }

    ngOnDestroy(): void {
        this.observer?.disconnect();
    }

    @HostListener('window:scroll')
    onScroll(): void {
        this.scrolled = (window.scrollY || 0) > 16;
    }

    scrollTo(id: string): void {
        const el = document.getElementById(id);
        if (el) {
            el.scrollIntoView({ behavior: 'smooth', block: 'start' });
        }
    }

    phoneHref(phone?: string | null): string {
        const digits = this.digitsOnly(phone);
        return digits ? `tel:+${this.toIntlPk(digits)}` : '#';
    }

    emailHref(email?: string | null): string {
        const value = (email || '').trim();
        return value ? `mailto:${value}` : '#';
    }

    whatsappHref(phone?: string | null): string {
        const digits = this.digitsOnly(phone);
        return digits ? `https://wa.me/${this.toIntlPk(digits)}` : '#';
    }

    coverUrl(dev: DeveloperDto): string {
        return this.developerService.getImageUrl(dev.backgroundImagePath);
    }

    profileUrl(dev: DeveloperDto): string {
        return this.developerService.getImageUrl(dev.profileImagePath);
    }

    initials(dev: DeveloperDto): string {
        const parts = (dev.name || '')
            .trim()
            .split(/\s+/)
            .filter(Boolean);
        if (!parts.length) {
            return '?';
        }
        if (parts.length === 1) {
            return parts[0].substring(0, 2).toUpperCase();
        }
        return (
            parts[0].charAt(0) + parts[parts.length - 1].charAt(0)
        ).toUpperCase();
    }

    tagList(dev: DeveloperDto): string[] {
        return (dev.tags || '')
            .split(',')
            .map((t) => t.trim())
            .filter(Boolean);
    }

    educationLines(dev: DeveloperDto): string[] {
        return (dev.education || '')
            .split(/\r?\n/)
            .map((l) => l.trim())
            .filter(Boolean);
    }

    planCtaHref(plan: { featured: boolean; price: string }): string {
        if (plan.featured || plan.price === 'Custom') {
            return this.contactWhatsapp !== '#'
                ? this.contactWhatsapp
                : '/auth/signup';
        }
        return '/auth/signup';
    }

    planCtaExternal(plan: { featured: boolean; price: string }): boolean {
        return (
            (plan.featured || plan.price === 'Custom') &&
            this.contactWhatsapp !== '#'
        );
    }

    private setupReveal(): void {
        const keys = Object.keys(this.visible) as LandingRevealKey[];
        if (typeof IntersectionObserver === 'undefined') {
            keys.forEach((k) => {
                this.visible[k] = true;
            });
            return;
        }

        this.observer?.disconnect();
        this.observer = new IntersectionObserver(
            (entries) => {
                for (const e of entries) {
                    if (!e.isIntersecting) {
                        continue;
                    }
                    const key = (e.target as HTMLElement).dataset['reveal'] as
                        | LandingRevealKey
                        | undefined;
                    if (key && key in this.visible) {
                        this.visible[key] = true;
                    }
                }
            },
            { threshold: 0.16, rootMargin: '0px 0px -6% 0px' }
        );

        document
            .querySelectorAll<HTMLElement>('[data-reveal]')
            .forEach((node) => this.observer!.observe(node));
    }

    private async loadDevelopers(): Promise<void> {
        this.developersLoading = true;
        try {
            this.developers = await this.developerService.getPublicDevelopers();
        } catch {
            this.developers = [];
        } finally {
            this.developersLoading = false;
            setTimeout(() => this.setupReveal(), 40);
        }
    }

    private digitsOnly(phone?: string | null): string {
        return (phone || '').replace(/\D/g, '');
    }

    private toIntlPk(digits: string): string {
        if (digits.startsWith('92')) {
            return digits;
        }
        if (digits.startsWith('0')) {
            return `92${digits.substring(1)}`;
        }
        return digits;
    }
}
