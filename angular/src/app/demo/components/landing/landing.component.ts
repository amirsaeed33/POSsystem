import { Component, HostListener, OnDestroy, OnInit } from '@angular/core';
import { MessageService } from 'primeng/api';
import { DeveloperDto } from 'src/app/demo/api/developer';
import { PackageDto } from 'src/app/demo/api/package';
import { PublicSellerDto, RegisterSellerDto } from 'src/app/demo/api/seller';
import { AppSeoService } from 'src/app/demo/service/app-seo.service';
import { BranchService } from 'src/app/demo/service/branch.service';
import { DeveloperService } from 'src/app/demo/service/developer.service';
import { PackageService } from 'src/app/demo/service/package.service';
import { SellerService } from 'src/app/demo/service/seller.service';

type LandingRevealKey =
    | 'hero'
    | 'product'
    | 'mobile'
    | 'how'
    | 'pricing'
    | 'sellers'
    | 'feedbacks'
    | 'developers'
    | 'cta'
    | 'footer';

@Component({
    selector: 'app-landing',
    templateUrl: './landing.component.html',
    styleUrls: ['./landing.component.scss'],
    providers: [MessageService],
})
export class LandingComponent implements OnInit, OnDestroy {
    readonly currentYear = new Date().getFullYear();
    readonly sellerCommissionPkr = 3000;

    scrolled = false;
    developers: DeveloperDto[] = [];
    developersLoading = false;
    packages: PackageDto[] = [];
    packagesLoading = false;
    feedbacksLoading = false;
    sellers: PublicSellerDto[] = [];
    sellersLoading = false;

    sellerDialogVisible = false;
    sellerSubmitting = false;
    sellerForm: RegisterSellerDto = this.emptySellerForm();

    visible: Record<LandingRevealKey, boolean> = {
        hero: false,
        product: false,
        mobile: false,
        how: false,
        pricing: false,
        sellers: false,
        feedbacks: false,
        developers: false,
        cta: false,
        footer: false,
    };

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

    feedbacks: Array<{
        name: string;
        role?: string;
        business?: string;
        city?: string;
        quote: string;
        rating: number;
    }> = [];

    private observer?: IntersectionObserver;

    constructor(
        private seo: AppSeoService,
        private developerService: DeveloperService,
        private packageService: PackageService,
        private branchService: BranchService,
        private sellerService: SellerService,
        private messageService: MessageService
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
            index: true,
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
        void this.loadPackages();
        void this.loadFeedbacks();
        void this.loadSellers();
        setTimeout(() => this.setupReveal(), 40);
    }

    openSellerDialog(): void {
        this.sellerForm = this.emptySellerForm();
        this.sellerDialogVisible = true;
    }

    async submitSellerApplication(): Promise<void> {
        const name = (this.sellerForm.fullName || '').trim();
        const phone = (this.sellerForm.phoneNumber || '').trim();
        if (!name || !phone) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Required',
                detail: 'Full name and phone number are required.',
            });
            return;
        }

        this.sellerSubmitting = true;
        try {
            await this.sellerService.register({
                ...this.sellerForm,
                fullName: name,
                phoneNumber: phone,
                email: (this.sellerForm.email || '').trim() || undefined,
                city: (this.sellerForm.city || '').trim() || undefined,
                companyName: (this.sellerForm.companyName || '').trim() || undefined,
                experienceNotes: (this.sellerForm.experienceNotes || '').trim() || undefined,
                message: (this.sellerForm.message || '').trim() || undefined,
            });
            this.sellerDialogVisible = false;
            this.messageService.add({
                severity: 'success',
                summary: 'Application received',
                detail: 'We’ll review your data and contact you after approval.',
            });
        } catch (err: any) {
            this.messageService.add({
                severity: 'error',
                summary: 'Could not submit',
                detail: err?.message || 'Failed to submit seller application',
            });
        } finally {
            this.sellerSubmitting = false;
        }
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

    packageFeatures(pkg: PackageDto): string[] {
        return (pkg.features || '')
            .split(/\r?\n/)
            .map((l) => l.trim())
            .filter(Boolean);
    }

    feedbackInitials(name: string): string {
        const parts = (name || '')
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

    feedbackStars(rating: number): number[] {
        const n = Math.max(0, Math.min(5, Math.round(rating || 0)));
        return Array.from({ length: n }, (_, i) => i + 1);
    }

    planCtaHref(pkg: PackageDto): string {
        if (pkg.contactViaWhatsApp || pkg.isFeatured || pkg.price === 'Custom') {
            return this.contactWhatsapp !== '#'
                ? this.contactWhatsapp
                : '/auth/signup';
        }
        return '/auth/signup';
    }

    planCtaExternal(pkg: PackageDto): boolean {
        return (
            !!(pkg.contactViaWhatsApp || pkg.isFeatured || pkg.price === 'Custom') &&
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

    private async loadPackages(): Promise<void> {
        this.packagesLoading = true;
        try {
            this.packages = await this.packageService.getPublicPackages();
        } catch {
            this.packages = [];
        } finally {
            this.packagesLoading = false;
            setTimeout(() => this.setupReveal(), 40);
        }
    }

    private async loadFeedbacks(): Promise<void> {
        this.feedbacksLoading = true;
        try {
            const rows = await this.branchService.getPublicFeedbacks();
            this.feedbacks = (rows || []).map((f) => ({
                name: f.name,
                role: f.role,
                business: f.business || f.branchName,
                city: f.city,
                quote: f.quote,
                rating: f.rating || 5,
            }));
        } catch {
            this.feedbacks = [];
        } finally {
            this.feedbacksLoading = false;
            setTimeout(() => this.setupReveal(), 40);
        }
    }

    private async loadSellers(): Promise<void> {
        this.sellersLoading = true;
        try {
            this.sellers = await this.sellerService.getPublicSellers();
        } catch {
            this.sellers = [];
        } finally {
            this.sellersLoading = false;
            setTimeout(() => this.setupReveal(), 40);
        }
    }

    private emptySellerForm(): RegisterSellerDto {
        return {
            fullName: '',
            email: '',
            phoneNumber: '',
            city: '',
            companyName: '',
            experienceNotes: '',
            message: '',
        };
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
