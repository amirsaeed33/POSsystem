import { Component, OnDestroy, OnInit, Renderer2, ViewChild } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { filter, Subscription } from 'rxjs';
import { BranchDto, BranchStatuses } from 'src/app/demo/api/branch';
import { BranchContextService } from 'src/app/demo/service/branch-context.service';
import { TenantContextService } from 'src/app/demo/service/tenant-context.service';
import { AuthService } from 'src/app/demo/service/auth.service';
import { SubscriptionService } from 'src/app/demo/service/subscription.service';
import { UserPresenceService } from 'src/app/demo/service/user-presence.service';
import { TabCloseEvent } from './api/tabcloseevent';
import { MenuService } from './app.menu.service';
import { AppSidebarComponent } from './app.sidebar.component';
import { AppTopBarComponent } from './app.topbar.component';
import { LayoutService } from './service/app.layout.service';

@Component({
    selector: 'app-layout',
    templateUrl: './app.layout.component.html'
})
export class AppLayoutComponent implements OnInit, OnDestroy {

    overlayMenuOpenSubscription: Subscription;

    tabOpenSubscription: Subscription;

    tabCloseSubscription: Subscription;

    branchSubscription: Subscription | null = null;

    menuOutsideClickListener: any;

    menuScrollListener: any;

    /** Tenant sessions with a non-Approved branch are locked from ops UI. */
    branchOpsLocked = false;
    /** True when locked and not on an allowed page (e.g. Branches). */
    showBranchLockOverlay = false;
    /** Paid-until date has passed. The business can only open Billing. */
    subscriptionExpired = false;
    /** Plan is still active but due within 7 days. Banner only — not a lock. */
    subscriptionExpiringSoon = false;
    subscriptionPending = false;
    subscriptionPaidUntil: string | null = null;
    subscriptionDaysLeft = 0;
    showSubscriptionLock = false;
    private readonly billingRoute = '/billing';
    lockedBranch: BranchDto | null = null;
    readonly BranchStatuses = BranchStatuses;
    private readonly unlockedRoutes = ['/branches', '/rider'];

    @ViewChild(AppSidebarComponent) appSidebar!: AppSidebarComponent;

    @ViewChild(AppTopBarComponent) appTopbar!: AppTopBarComponent;

    constructor(
        private menuService: MenuService,
        public layoutService: LayoutService,
        public renderer: Renderer2,
        public router: Router,
        private branchContext: BranchContextService,
        private tenantContext: TenantContextService,
        private authService: AuthService,
        private presence: UserPresenceService,
        private subscriptionsApi: SubscriptionService
    ) {
        this.overlayMenuOpenSubscription = this.layoutService.overlayOpen$.subscribe(() => {
            if (!this.menuOutsideClickListener) {
                this.menuOutsideClickListener = this.renderer.listen('document', 'click', event => {
                    const isOutsideClicked = !(this.appSidebar.el.nativeElement.isSameNode(event.target) || this.appSidebar.el.nativeElement.contains(event.target)
                    || this.appTopbar.menuButton.nativeElement.isSameNode(event.target) || this.appTopbar.menuButton.nativeElement.contains(event.target));
                    if (isOutsideClicked) {
                        this.hideMenu();
                    }
                });
            }

            if ((this.layoutService.isSlim() || this.layoutService.isSlimPlus()) && !this.menuScrollListener) {
                this.menuScrollListener = this.renderer.listen(this.appSidebar.menuContainer.nativeElement, 'scroll', event => {
                    if (this.layoutService.isDesktop()) {
                        this.hideMenu();
                    }
                });
            }

            if (this.layoutService.state.staticMenuMobileActive) {
                this.blockBodyScroll();
            }
        });

        this.router.events.pipe(filter(event => event instanceof NavigationEnd))
            .subscribe(() => {
                this.hideMenu();
                this.updateLockOverlay();
                void this.refreshSubscriptionLock();
            });

        this.tabOpenSubscription = this.layoutService.tabOpen$.subscribe(tab => {
            this.router.navigate(tab.routerLink);
            this.layoutService.openTab(tab);
        });

        this.tabCloseSubscription = this.layoutService.tabClose$.subscribe((event: TabCloseEvent) => {
            if (this.router.isActive(event.tab.routerLink[0], { paths: 'subset', queryParams: 'subset', fragment: 'ignored', matrixParams: 'ignored'})) {
                const tabs = this.layoutService.tabs;

                if (tabs.length > 1) { 
                    if (event.index === (tabs.length - 1))
                        this.router.navigate(tabs[tabs.length - 2].routerLink);
                    else
                        this.router.navigate(tabs[event.index + 1].routerLink);
                }
                else {
                    this.router.navigate(['/']);
                }
            }

            this.layoutService.closeTab(event.index);
        });
    }

    ngOnInit(): void {
        this.presence.startHeartbeat(() => this.authService.isAuthenticated());
        void this.refreshSubscriptionLock();
        this.refreshBranchLock(this.branchContext.getCurrentBranch());
        this.branchSubscription = this.branchContext.currentBranch$.subscribe((branch) => {
            this.refreshBranchLock(branch);
        });
    }

    private refreshBranchLock(branch: BranchDto | null): void {
        if (this.authService.isDeliveryRider()) {
            this.branchOpsLocked = false;
            this.showBranchLockOverlay = false;
            return;
        }
        const isTenant = this.tenantContext.getTenantId() != null;
        this.lockedBranch = branch;
        this.branchOpsLocked =
            isTenant &&
            !!branch &&
            (branch.status || BranchStatuses.Pending).toLowerCase() !==
                BranchStatuses.Approved.toLowerCase();
        this.updateLockOverlay();
    }

    private async refreshSubscriptionLock(): Promise<void> {
        if (!this.authService.isAuthenticated() || BranchContextService.isHostAdminSession()) {
            this.subscriptionExpired = false;
            this.subscriptionExpiringSoon = false;
            this.subscriptionPending = false;
            this.subscriptionDaysLeft = 0;
            this.showSubscriptionLock = false;
            return;
        }

        try {
            const status = await this.subscriptionsApi.getAccessStatus(true);
            this.subscriptionExpired = status.expired;
            this.subscriptionPending = status.hasPendingPayment;
            this.subscriptionPaidUntil = status.paidUntil || null;
            this.subscriptionDaysLeft = status.daysLeft ?? 0;
            this.subscriptionExpiringSoon =
                !status.expired &&
                !!status.paidUntil &&
                this.subscriptionDaysLeft <= 7;
        } catch {
            this.subscriptionExpired = false;
            this.subscriptionExpiringSoon = false;
            this.showSubscriptionLock = false;
            return;
        }

        this.updateSubscriptionLock();
    }

    private updateSubscriptionLock(): void {
        if (!this.subscriptionExpired) {
            this.showSubscriptionLock = false;
            return;
        }
        const url = (this.router.url || '').split('?')[0];
        const onBilling = url === this.billingRoute || url.startsWith(this.billingRoute + '/');
        this.showSubscriptionLock = !onBilling;
    }

    private updateLockOverlay(): void {
        this.updateSubscriptionLock();
        if (!this.branchOpsLocked) {
            this.showBranchLockOverlay = false;
            return;
        }
        const url = (this.router.url || '').split('?')[0];
        const onAllowed = this.unlockedRoutes.some(
            (route) => url === route || url.startsWith(route + '/')
        );
        this.showBranchLockOverlay = !onAllowed;
    }

    get lockedStatusLabel(): string {
        const status = this.lockedBranch?.status || BranchStatuses.Pending;
        return status;
    }

    blockBodyScroll(): void {
        if (document.body.classList) {
            document.body.classList.add('blocked-scroll');
        }
        else {
            document.body.className += ' blocked-scroll';
        }
    }

    unblockBodyScroll(): void {
        if (document.body.classList) {
            document.body.classList.remove('blocked-scroll');
        }
        else {
            document.body.className = document.body.className.replace(new RegExp('(^|\\b)' +
                'blocked-scroll'.split(' ').join('|') + '(\\b|$)', 'gi'), ' ');
        }
    }

    hideMenu() {
        this.layoutService.state.overlayMenuActive = false;
        this.layoutService.state.staticMenuMobileActive = false;
        this.layoutService.state.menuHoverActive = false;
        this.menuService.reset();
        if(this.menuOutsideClickListener) {
            this.menuOutsideClickListener();
            this.menuOutsideClickListener = null;
        }
        
        if (this.menuScrollListener) {
            this.menuScrollListener();
            this.menuScrollListener = null;
        }

        this.unblockBodyScroll();
    }

    get containerClass() {
        return {
            'layout-slim': this.layoutService.config().menuMode === 'slim',
            'layout-slim-plus': this.layoutService.config().menuMode === 'slim-plus',
            'layout-static': this.layoutService.config().menuMode === 'static',
            'layout-overlay': this.layoutService.config().menuMode === 'overlay',
            'layout-overlay-active': this.layoutService.state.overlayMenuActive,
            'layout-mobile-active': this.layoutService.state.staticMenuMobileActive,
            'layout-static-inactive': this.layoutService.state.staticMenuDesktopInactive && this.layoutService.config().menuMode === 'static',
            'p-input-filled': this.layoutService.config().inputStyle === 'filled',
            'p-ripple-disabled': !this.layoutService.config().ripple,
            'layout-light': this.layoutService.config().layoutTheme === 'colorScheme' && this.layoutService.config().colorScheme === 'light',
            'layout-dark': this.layoutService.config().layoutTheme === 'colorScheme' && this.layoutService.config().colorScheme === 'dark',
            'layout-primary': this.layoutService.config().colorScheme !== 'dark' && this.layoutService.config().layoutTheme === 'primaryColor'
        }
    }

    ngOnDestroy() {
        this.presence.stopHeartbeat();
        if (this.overlayMenuOpenSubscription) {
            this.overlayMenuOpenSubscription.unsubscribe();
        }

        if (this.menuOutsideClickListener) {
            this.menuOutsideClickListener();
        }

        if (this.tabOpenSubscription) {
            this.tabOpenSubscription.unsubscribe();
        }

        if (this.tabCloseSubscription) {
            this.tabCloseSubscription.unsubscribe();
        }

        if (this.branchSubscription) {
            this.branchSubscription.unsubscribe();
        }
    }

}

