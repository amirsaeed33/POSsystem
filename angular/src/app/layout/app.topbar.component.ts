import { Component, ElementRef, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { MenuItem, MessageService } from 'primeng/api';
import { Router } from '@angular/router';
import { LayoutService } from './service/app.layout.service';
import { AuthService } from 'src/app/demo/service/auth.service';
import { SessionService } from 'src/app/demo/service/session.service';
import { TenantContextService } from 'src/app/demo/service/tenant-context.service';
import { BranchContextService, HOST_ADMIN_STORAGE_KEY } from 'src/app/demo/service/branch-context.service';
import { PermissionService } from 'src/app/demo/service/permission.service';
import { CustomerOrderService } from 'src/app/demo/service/customer-order.service';
import { CustomerOrderDto, CustomerOrderStatus } from 'src/app/demo/api/customer-order';
import { BranchDto, BranchStatuses } from 'src/app/demo/api/branch';
import { PermissionNames } from 'src/app/demo/api/permission-names';
import { UserLoginInfoDto } from 'src/app/demo/api/session';
import { environment } from 'src/environments/environment';

@Component({
    selector: 'app-topbar',
    templateUrl: './app.topbar.component.html',
    providers: [MessageService],
})
export class AppTopBarComponent implements OnInit, OnDestroy {
    @ViewChild('menubutton') menuButton!: ElementRef;

    userInfo: UserLoginInfoDto | null = null;
    userDisplayName = 'User';
    userInitials = 'U';
    userRole = '';
    userImage: string | null = null;
    hasUserImage = false;

    branches: BranchDto[] = [];
    currentBranch: BranchDto | null = null;
    changingBranch = false;

    todayPendingOrders: CustomerOrderDto[] = [];
    pendingOrdersCount = 0;
    loadingOrders = false;
    private orderPollTimer: any;

    readonly BranchStatuses = BranchStatuses;

    constructor(
        public layoutService: LayoutService,
        private authService: AuthService,
        private sessionService: SessionService,
        private tenantContext: TenantContextService,
        private branchContext: BranchContextService,
        private permissionService: PermissionService,
        private customerOrderService: CustomerOrderService,
        private messageService: MessageService,
        private router: Router
    ) {}

    /** Host admin: sees every business location (flag survives location cookie). */
    get isHostAdmin(): boolean {
        return (
            localStorage.getItem(HOST_ADMIN_STORAGE_KEY) === 'true' ||
            this.permissionService.isGranted(PermissionNames.BranchesApprove) ||
            this.permissionService.isGranted(PermissionNames.Tenants)
        );
    }

    /** Tenant admin may switch among own locations; staff are locked. */
    get canSwitchLocations(): boolean {
        return (
            this.isHostAdmin ||
            this.permissionService.isGranted(PermissionNames.Branches)
        );
    }

    get locationLabel(): string {
        return this.currentBranch?.name || 'Select location';
    }

    ngOnInit(): void {
        this.tenantContext
            .ensureMultiTenancyLoaded()
            .catch(() => undefined)
            .finally(() => this.loadUserInfo());

        this.loadTodayOrders();
        this.orderPollTimer = setInterval(() => this.loadTodayOrders(), 30000);
    }

    ngOnDestroy(): void {
        if (this.orderPollTimer) {
            clearInterval(this.orderPollTimer);
        }
    }

    async loadTodayOrders(): Promise<void> {
        try {
            this.loadingOrders = true;
            const res = await this.customerOrderService.getAll({ maxResultCount: 100 });
            const allOrders = res.items || [];

            const today = new Date();
            today.setHours(0, 0, 0, 0);

            // Filter today's pending/active orders (remove completed or rejected)
            this.todayPendingOrders = allOrders.filter((order) => {
                const isPending = order.status === CustomerOrderStatus.Pending || order.status === 0;
                if (!isPending) {
                    return false;
                }
                if (!order.orderDate) {
                    return true;
                }
                const orderDate = new Date(order.orderDate);
                orderDate.setHours(0, 0, 0, 0);
                return orderDate.getTime() === today.getTime();
            });

            this.pendingOrdersCount = this.todayPendingOrders.length;
        } catch (error) {
            console.error('Failed to load online orders for topbar:', error);
        } finally {
            this.loadingOrders = false;
        }
    }

    formatOrderTime(dateInput: any): string {
        if (!dateInput) return '';
        const d = new Date(dateInput);
        return d.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
    }

    viewOrder(order: CustomerOrderDto): void {
        this.router.navigate(['/customer-orders'], { queryParams: { id: order.id } });
    }

    loadUserInfo(): void {
        const cachedUserInfo = this.authService.getUserInfo();
        if (cachedUserInfo) {
            this.setUserInfo(cachedUserInfo);
        }

        if (!this.authService.isAuthenticated()) {
            return;
        }

        this.permissionService
            .ensureLoaded()
            .catch(() => undefined)
            .then(() => this.sessionService.getCurrentLoginInformations())
            .then((sessionInfo) => {
                if (sessionInfo?.user) {
                    this.authService.setUserInfo(sessionInfo.user);
                    this.setUserInfo(sessionInfo.user);
                }

                if (this.isHostAdmin) {
                    return this.branchContext.ensureLoaded(null);
                }

                if (sessionInfo?.tenant) {
                    this.tenantContext.setTenantInfo(sessionInfo.tenant);
                    if (sessionInfo.tenant.id) {
                        this.tenantContext.setTenantId(sessionInfo.tenant.id);
                    }
                } else {
                    this.tenantContext.setTenantInfo(null);
                }

                return this.branchContext.ensureLoaded(
                    sessionInfo?.user?.branchId ?? null
                );
            })
            .then((branches) => {
                if (branches == null) {
                    return;
                }
                this.branches = branches || this.branchContext.getAllowedBranches();
                this.currentBranch = this.branchContext.getCurrentBranch();

                if (
                    !this.isHostAdmin &&
                    this.tenantContext.getTenantId() != null &&
                    !(this.branches?.length) &&
                    !(this.router.url || '').startsWith('/branches/create')
                ) {
                    this.router.navigateByUrl('/branches/create');
                    return;
                }

                if (this.isHostAdmin && this.currentBranch) {
                    if (this.syncLocationBusinessContext(this.currentBranch)) {
                        location.reload();
                    }
                }
            })
            .catch(() => {
                if (!this.userInfo && cachedUserInfo) {
                    this.setUserInfo(cachedUserInfo);
                }
            });
    }

    onChangeBranch(branch: BranchDto): void {
        if (
            !branch?.id ||
            branch.id === this.currentBranch?.id ||
            this.changingBranch ||
            !this.canSwitchLocations
        ) {
            return;
        }

        this.changingBranch = true;
        this.branchContext.setCurrentBranch(branch);
        this.currentBranch = branch;

        if (this.isHostAdmin && this.syncLocationBusinessContext(branch)) {
            location.reload();
            return;
        }

        this.changingBranch = false;
        this.remountCurrentRoute();
    }

    /**
     * Align Abp.TenantId with the selected location (internal plumbing for host).
     * Returns true when the cookie changed (caller should reload).
     */
    private syncLocationBusinessContext(branch: BranchDto): boolean {
        const nextTenantId = branch.tenantId ?? null;
        const currentTenantId = this.tenantContext.getTenantId();
        if (nextTenantId === currentTenantId) {
            return false;
        }

        if (nextTenantId != null) {
            this.tenantContext.setTenantId(nextTenantId);
            this.tenantContext.setTenantInfo({
                id: nextTenantId,
                tenancyName: branch.tenancyName || '',
                name: branch.tenancyName || '',
            });
        } else {
            this.tenantContext.setTenantId(undefined);
            this.tenantContext.setTenantInfo(null);
        }
        return true;
    }

    private remountCurrentRoute(): void {
        const url = this.router.url;
        this.router
            .navigateByUrl('/notfound', { skipLocationChange: true })
            .then(() => this.router.navigateByUrl(url));
    }

    setUserInfo(user: UserLoginInfoDto): void {
        this.userInfo = user;
        const firstName = (user.name || '').trim();
        const lastName = (user.surname || '').trim();
        this.userDisplayName =
            `${firstName} ${lastName}`.trim() || user.userName || 'User';
        this.userInitials = this.getInitials(firstName, lastName, user.userName);

        const pictureUrl =
            user.profilePictureUrl ||
            (user as any).userImageUrl ||
            (user as any).UserImageUrl;

        if (pictureUrl) {
            if (
                pictureUrl.startsWith('http://') ||
                pictureUrl.startsWith('https://')
            ) {
                this.userImage = pictureUrl;
            } else {
                this.userImage = `${environment.apiUrl}${
                    pictureUrl.startsWith('/') ? '' : '/'
                }${pictureUrl}`;
            }
            this.hasUserImage = true;
        } else {
            this.userImage = null;
            this.hasUserImage = false;
        }

        if (user.roleNames && Array.isArray(user.roleNames) && user.roleNames.length > 0) {
            const roles = user.roleNames;
            const adminRole = roles.find(
                (r) => r && r.toLowerCase().includes('admin')
            );
            if (adminRole) {
                this.userRole = 'Admin';
            } else {
                const firstRole = roles[0];
                this.userRole = firstRole
                    ? firstRole.charAt(0).toUpperCase() +
                      firstRole.slice(1).toLowerCase()
                    : 'User';
            }
        } else {
            this.userRole = 'User';
        }
    }

    getInitials(firstName: string, lastName: string, userName?: string): string {
        const first = firstName?.charAt(0) || '';
        const last = lastName?.charAt(0) || '';
        const initials = `${first}${last}`.toUpperCase();
        if (initials) {
            return initials;
        }
        const fromUser = (userName || 'U').trim().charAt(0).toUpperCase();
        return fromUser || 'U';
    }

    onImageError(): void {
        this.hasUserImage = false;
        this.userImage = null;
    }

    onMenuButtonClick(): void {
        this.layoutService.onMenuToggle();
    }

    removeTab(event: MouseEvent, item: MenuItem, index: number): void {
        this.layoutService.onTabClose(item, index);
        event.preventDefault();
    }

    get layoutTheme(): string {
        return this.layoutService.config().layoutTheme;
    }

    get colorScheme(): string {
        return this.layoutService.config().colorScheme;
    }

    get logo(): string {
        const path = 'assets/layout/images/logo-';
        const logo =
            this.layoutTheme === 'primaryColor' &&
            this.layoutService.config().theme !== 'yellow'
                ? 'light.png'
                : this.colorScheme === 'light'
                  ? 'dark.png'
                  : 'light.png';
        return path + logo;
    }

    get tabs(): MenuItem[] {
        return this.layoutService.tabs;
    }

    onSignOut(): void {
        this.branchContext.clear();
        this.authService.logout();
        this.router.navigate(['/auth/login']);
    }
}
