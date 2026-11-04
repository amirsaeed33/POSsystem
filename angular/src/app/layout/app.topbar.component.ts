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
import { BranchService } from 'src/app/demo/service/branch.service';
import { PermissionNames } from 'src/app/demo/api/permission-names';
import { UserLoginInfoDto } from 'src/app/demo/api/session';
import { CustomerChatService } from 'src/app/demo/service/customer-chat.service';
import { CustomerChatMessageDto, CustomerOrderConversationDto } from 'src/app/demo/api/customer-chat';
import { environment } from 'src/environments/environment';
import { Subscription } from 'rxjs';
import { RestaurantTakeawayAlarmService } from 'src/app/demo/service/restaurant-takeaway-alarm.service';
import { AppSoundService } from 'src/app/demo/service/app-sound.service';
import { UnpaidTakeawayOrderDto } from 'src/app/demo/api/restaurant-order';
import { SaleService } from 'src/app/demo/service/sale.service';
import { SaleDto } from 'src/app/demo/api/sale';

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
    branchLogoFailed = false;
    get printerConfigVisible(): boolean {
        return this.layoutService.printerConfigVisible;
    }
    set printerConfigVisible(val: boolean) {
        this.layoutService.printerConfigVisible = val;
    }
    private branchSubscription?: Subscription;

    get branchDisplayName(): string {
        return this.currentBranch?.name || 'SmartPos';
    }

    get branchLogo(): string | null {
        if (this.branchLogoFailed) {
            return null;
        }
        if (this.currentBranch?.imagePath) {
            return this.branchService.getImageUrl(this.currentBranch.imagePath);
        }
        return null;
    }

    get isDeliveryRider(): boolean {
        return this.authService.isDeliveryRider();
    }

    get isWaiter(): boolean {
        return this.authService.isWaiter();
    }

    /** Logo / branch name / Home — role-specific portal home. */
    get homeLink(): string {
        return this.authService.getHomeRoute();
    }

    get homeLinkLabel(): string {
        if (this.isDeliveryRider) {
            return 'Dashboard';
        }
        if (this.isWaiter) {
            return 'POS';
        }
        return 'Home';
    }

    todayPendingOrders: CustomerOrderDto[] = [];
    pendingOrdersCount = 0;
    loadingOrders = false;
    approvingOrderId: number | null = null;
    private orderPollTimer: any;

    // Real-Time Customer Chat State (merged into bell notifications)
    unreadChatCount = 0;
    chatConversations: CustomerOrderConversationDto[] = [];
    loadingChats = false;
    chatDialogVisible = false;
    selectedConversationForChat: CustomerOrderConversationDto | null = null;
    private chatNotificationSub?: Subscription;
    private newOrderSub?: Subscription;
    private orderStatusSub?: Subscription;

    // Restaurant Unpaid Takeaway Alarm State
    unpaidTakeawayOrders: UnpaidTakeawayOrderDto[] = [];
    takeawayAudioBlocked = false;
    takeawayOrdersDialogVisible = false;

    // Unpaid Bills State
    unpaidBills: SaleDto[] = [];
    loadingUnpaidBills = false;
    collectBillDialogVisible = false;
    selectedBillForPayment: SaleDto | null = null;
    collectBillAmount = 0;
    collectBillPaymentMethod = 1;
    collectBillNotes = '';
    collectingBillPayment = false;
    paymentMethodOptions = [
        { label: 'Cash', value: 1 },
        { label: 'Card', value: 2 }
    ];
    private unpaidBillsPollTimer: any;
    private takeawayAlarmSub?: Subscription;
    private takeawayAudioSub?: Subscription;

    readonly BranchStatuses = BranchStatuses;

    constructor(
        public layoutService: LayoutService,
        private authService: AuthService,
        private sessionService: SessionService,
        private tenantContext: TenantContextService,
        private branchContext: BranchContextService,
        private branchService: BranchService,
        private permissionService: PermissionService,
        private customerOrderService: CustomerOrderService,
        private customerChatService: CustomerChatService,
        public takeawayAlarmService: RestaurantTakeawayAlarmService,
        public appSound: AppSoundService,
        private messageService: MessageService,
        private saleService: SaleService,
        private router: Router
    ) { }

    /** Host admin: sees every business location (flag survives location cookie). */
    get isHostAdmin(): boolean {
        return (
            localStorage.getItem(HOST_ADMIN_STORAGE_KEY) === 'true' ||
            this.permissionService.isGranted(PermissionNames.BranchesApprove) ||
            this.permissionService.isGranted(PermissionNames.Tenants)
        );
    }

    /** Tenant admin may switch among own locations; staff are locked. */
    get canViewUnpaidBills(): boolean {
        return !this.isWaiter && !this.isDeliveryRider && (
            this.isHostAdmin ||
            this.permissionService.isGranted(PermissionNames.Sales)
        );
    }

    get canSwitchLocations(): boolean {
        return (
            this.isHostAdmin ||
            this.permissionService.isGranted(PermissionNames.Branches)
        );
    }

    get locationLabel(): string {
        return this.currentBranch?.name || 'Select location';
    }

    /** First few letters of the shop/branch name for compact mobile topbar. */
    get locationLabelShort(): string {
        const name = (this.currentBranch?.name || '').trim();
        if (!name) {
            return 'Shop';
        }
        if (name.length <= 8) {
            return name;
        }
        return name.substring(0, 8).trimEnd() + '…';
    }

    ngOnInit(): void {
        this.currentBranch = this.branchContext.getCurrentBranch();
        this.branchLogoFailed = false;
        this.branchSubscription = this.branchContext.currentBranch$.subscribe((b) => {
            if (b) {
                this.currentBranch = b;
                this.branchLogoFailed = false;
                if (!this.isDeliveryRider && !this.isWaiter) {
                    this.loadTodayOrders();
                    if (b.id) {
                        if (this.canViewUnpaidBills) {
                            this.loadUnpaidBills();
                        }
                        this.customerChatService.joinBranchChat(b.id);
                        this.loadUnreadChatCount();
                    }
                }
            }
        });

        this.tenantContext
            .ensureMultiTenancyLoaded()
            .catch(() => undefined)
            .finally(() => this.loadUserInfo());

        if (!this.isDeliveryRider && !this.isWaiter) {
            this.loadTodayOrders();
            this.orderPollTimer = setInterval(() => this.loadTodayOrders(), 30000);
            this.initChatListener();
        }

        this.takeawayAlarmSub = this.takeawayAlarmService.unpaidOrders$.subscribe((orders) => {
            this.unpaidTakeawayOrders = orders || [];
        });
        if (this.canViewUnpaidBills) {
            this.loadUnpaidBills();
            this.unpaidBillsPollTimer = setInterval(() => this.loadUnpaidBills(), 60000);
        }

        this.takeawayAudioSub = this.takeawayAlarmService.audioBlocked$.subscribe((blocked) => {
            this.takeawayAudioBlocked = blocked;
        });
    }

    ngOnDestroy(): void {
        if (this.unpaidBillsPollTimer) {
            clearInterval(this.unpaidBillsPollTimer);
        }
        if (this.orderPollTimer) {
            clearInterval(this.orderPollTimer);
        }
        if (this.branchSubscription) {
            this.branchSubscription.unsubscribe();
        }
        if (this.chatNotificationSub) {
            this.chatNotificationSub.unsubscribe();
        }
        if (this.newOrderSub) {
            this.newOrderSub.unsubscribe();
        }
        if (this.orderStatusSub) {
            this.orderStatusSub.unsubscribe();
        }
        if (this.takeawayAlarmSub) {
            this.takeawayAlarmSub.unsubscribe();
        }
        if (this.takeawayAudioSub) {
            this.takeawayAudioSub.unsubscribe();
        }
    }

    enableTakeawayAudio(): void {
        this.takeawayAlarmService.enableAudio();
    }

    toggleTakeawayMute(): void {
        // Global mute: takeaway KOT + chat/order chimes + any other AppSound-gated beeps.
        this.takeawayAlarmService.toggleMute();
    }

    openTakeawayInPos(order: UnpaidTakeawayOrderDto): void {
        this.takeawayOrdersDialogVisible = false;
        this.router.navigate(['/pos'], { queryParams: { restaurantOrderId: order.id, _t: Date.now() } });
    }

    private initChatListener(): void {
        if (this.isDeliveryRider) return;
        if (this.currentBranch?.id) {
            this.customerChatService.joinBranchChat(this.currentBranch.id);
            this.loadUnreadChatCount();
        }

        this.chatNotificationSub = this.customerChatService.notificationReceived$.subscribe((msg) => {
            this.unreadChatCount++;
            this.playChatChime();

            const isOrder = Boolean(msg.orderNo);
            const title = isOrder ? `Order #${msg.orderNo}` : 'Storefront Customer Inquiry';
            this.messageService.add({
                severity: 'info',
                summary: `💬 ${msg.senderName || 'Customer'} (${title})`,
                detail: msg.message,
                life: 8000
            });

            this.loadChatConversations(false);
        });

        this.newOrderSub = this.customerChatService.newOrderReceived$.subscribe((order) => {
            if (!this.isHostAdmin && this.currentBranch?.id && order.branchId && order.branchId !== this.currentBranch.id) {
                return;
            }
            this.playOrderChime();
            this.messageService.add({
                severity: 'success',
                summary: `🛒 New Online Order! #${order.orderNo || order.orderId}`,
                detail: `${order.customerName || 'Online Customer'} placed an order of PKR ${order.totalAmount || 0}`,
                life: 9000
            });
            this.loadTodayOrders();
        });

        this.orderStatusSub = this.customerChatService.orderStatusChanged$.subscribe((evt) => {
            if (!this.isHostAdmin && this.currentBranch?.id && evt.branchId && evt.branchId !== this.currentBranch.id) {
                return;
            }
            this.loadTodayOrders();
        });
    }

    get totalNotificationCount(): number {
        return (this.pendingOrdersCount || 0) + (this.unreadChatCount || 0);
    }

    openNotificationsPanel(): void {
        this.loadTodayOrders();
        this.loadChatConversations();
    }

    async loadUnreadChatCount(): Promise<void> {
        if (this.isDeliveryRider || this.isWaiter || !this.currentBranch?.id) return;
        try {
            this.unreadChatCount = await this.customerChatService.getBranchUnreadCount(this.currentBranch.id);
        } catch { }
    }

    async loadChatConversations(showLoading = true): Promise<void> {
        if (!this.currentBranch?.id) return;
        if (showLoading) this.loadingChats = true;
        try {
            this.chatConversations = await this.customerChatService.getBranchConversations(this.currentBranch.id);
            this.unreadChatCount = this.chatConversations.reduce((acc, c) => acc + (c.unreadCount || 0), 0);
        } catch (err) {
            console.warn('Failed to load chat conversations:', err);
        } finally {
            if (showLoading) this.loadingChats = false;
        }
    }

    openChatFromConversation(conv: CustomerOrderConversationDto): void {
        this.selectedConversationForChat = conv;
        this.chatDialogVisible = true;
    }

    private playChatChime(): void {
        if (!this.appSound.canPlay()) {
            return;
        }
        try {
            const AudioContextClass = window.AudioContext || (window as any).webkitAudioContext;
            if (!AudioContextClass) return;
            const ctx = new AudioContextClass();
            const osc = ctx.createOscillator();
            const gain = ctx.createGain();

            osc.type = 'sine';
            osc.frequency.setValueAtTime(659.25, ctx.currentTime); // E5
            osc.frequency.exponentialRampToValueAtTime(987.77, ctx.currentTime + 0.18); // B5

            gain.gain.setValueAtTime(0.18, ctx.currentTime);
            gain.gain.exponentialRampToValueAtTime(0.001, ctx.currentTime + 0.35);

            osc.connect(gain);
            gain.connect(ctx.destination);
            osc.start();
            osc.stop(ctx.currentTime + 0.35);
        } catch { }
    }

    private playOrderChime(): void {
        if (!this.appSound.canPlay()) {
            return;
        }
        try {
            const AudioContextClass = window.AudioContext || (window as any).webkitAudioContext;
            if (!AudioContextClass) return;
            const ctx = new AudioContextClass();
            const now = ctx.currentTime;

            const osc1 = ctx.createOscillator();
            const gain1 = ctx.createGain();
            osc1.type = 'triangle';
            osc1.frequency.setValueAtTime(587.33, now); // D5
            gain1.gain.setValueAtTime(0.25, now);
            gain1.gain.exponentialRampToValueAtTime(0.001, now + 0.22);
            osc1.connect(gain1);
            gain1.connect(ctx.destination);
            osc1.start(now);
            osc1.stop(now + 0.22);

            const osc2 = ctx.createOscillator();
            const gain2 = ctx.createGain();
            osc2.type = 'triangle';
            osc2.frequency.setValueAtTime(880.00, now + 0.16); // A5
            gain2.gain.setValueAtTime(0.28, now + 0.16);
            gain2.gain.exponentialRampToValueAtTime(0.001, now + 0.45);
            osc2.connect(gain2);
            gain2.connect(ctx.destination);
            osc2.start(now + 0.16);
            osc2.stop(now + 0.45);
        } catch { }
    }

    async loadTodayOrders(): Promise<void> {
        if (this.isDeliveryRider) return;
        try {
            this.loadingOrders = true;
            const res = await this.customerOrderService.getAll({ maxResultCount: 100 });
            const allOrders = res.items || [];

            // Filter all pending/active orders that require store attention
            this.todayPendingOrders = allOrders.filter((order) => {
                const isPending = order.status === CustomerOrderStatus.Pending || order.status === 0;
                if (!isPending) return false;

                // Host admin sees all incoming online orders across all branches
                if (this.isHostAdmin) {
                    return true;
                }

                // Branch staff / tenant admin only sees orders for their active branch
                if (this.currentBranch?.id && order.branchId) {
                    return order.branchId === this.currentBranch.id;
                }

                return true;
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

    async approveOrder(order: CustomerOrderDto): Promise<void> {
        if (!order?.id || this.approvingOrderId === order.id) {
            return;
        }

        const label = order.orderNo || `#${order.id}`;
        this.approvingOrderId = order.id;
        try {
            const sale = await this.customerOrderService.approve(order.id);
            const invoice = sale?.invoiceNo ? ` Sale invoice: ${sale.invoiceNo}.` : '';
            this.messageService.add({
                severity: 'success',
                summary: 'Order Approved',
                detail: `Order "${label}" approved.${invoice}`,
                life: 5000,
            });
            await this.loadTodayOrders();
        } catch (error: any) {
            const message =
                error?.error?.error?.message ||
                error?.error?.message ||
                error?.message ||
                'Failed to approve order';
            this.messageService.add({
                severity: 'error',
                summary: 'Approve Failed',
                detail: message,
                life: 6000,
            });
        } finally {
            this.approvingOrderId = null;
        }
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

                    if (this.authService.isDeliveryRider()) {
                        const currentUrl = this.router.url || '';
                        if (!currentUrl.startsWith('/rider') && !currentUrl.startsWith('/auth')) {
                            this.router.navigateByUrl('/rider/dashboard');
                        }
                    } else if (this.authService.isWaiter()) {
                        const currentUrl = this.router.url || '';
                        if (!currentUrl.startsWith('/pos') && !currentUrl.startsWith('/auth')) {
                            this.router.navigateByUrl('/pos');
                        }
                    }
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
        this.branchLogoFailed = false;
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
                this.userImage = `${environment.apiUrl}${pictureUrl.startsWith('/') ? '' : '/'
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
        } else if (user.isDeliveryRider || (user.designation && user.designation.toLowerCase().includes('rider'))) {
            this.userRole = 'Delivery Rider';
        } else if (user.isWaiter || (user.designation && (user.designation.toLowerCase().includes('waiter') || user.designation.toLowerCase().includes('server')))) {
            this.userRole = 'Waiter';
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

    get defaultLogo(): string {
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

    get logo(): string {
        return this.branchLogo || this.defaultLogo;
    }

    onLogoError(): void {
        this.branchLogoFailed = true;
    }

    get tabs(): MenuItem[] {
        return this.layoutService.tabs;
    }

    async loadUnpaidBills(): Promise<void> {
        if (!this.canViewUnpaidBills) {
            this.unpaidBills = [];
            return;
        }
        this.loadingUnpaidBills = true;
        try {
            this.unpaidBills = await this.saleService.getUnpaidSales();
        } catch (err) {
            console.warn('Failed to load unpaid sales in topbar', err);
        } finally {
            this.loadingUnpaidBills = false;
        }
    }

    getBillType(bill: SaleDto | null): string {
        if (!bill) return 'Takeaway';
        if (bill.saleType) return bill.saleType;
        const notes = bill.notes || '';
        if (notes.includes('[Online Order') || notes.includes('ORD-') || (bill.deliveryCharges ?? 0) > 0 || bill.deliveryAreaName) {
            return 'Online Order';
        }
        if (notes.includes('[Dine-In') || notes.includes('Dine-In')) {
            return 'Dine-In';
        }
        return 'Takeaway';
    }

    getBillDueAmount(bill: SaleDto | null): number {
        if (!bill) return 0;
        if (bill.dueAmount !== undefined && bill.dueAmount !== null) {
            return Math.max(0, bill.dueAmount);
        }
        const paid = bill.paidAmount ?? ((bill.cashAmount ?? 0) + (bill.cardAmount ?? 0));
        return Math.max(0, (bill.totalAmount ?? 0) - paid);
    }

    openCollectBillDialog(bill: SaleDto): void {
        this.selectedBillForPayment = bill;
        this.collectBillAmount = this.getBillDueAmount(bill);
        this.collectBillPaymentMethod = 1;
        this.collectBillNotes = '';
        this.collectBillDialogVisible = true;
    }

    cancelCollectBillDialog(): void {
        this.collectBillDialogVisible = false;
        this.selectedBillForPayment = null;
        this.collectBillAmount = 0;
        this.collectBillNotes = '';
    }

    async submitCollectBillPayment(): Promise<void> {
        if (!this.selectedBillForPayment) return;
        const due = this.getBillDueAmount(this.selectedBillForPayment);
        if (this.collectBillAmount <= 0) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Invalid Amount',
                detail: 'Please enter an amount greater than 0.'
            });
            return;
        }
        if (this.collectBillAmount > due + 0.01) {
            this.messageService.add({
                severity: 'error',
                summary: 'Amount Exceeded',
                detail: `Amount cannot exceed remaining due (PKR ${due.toFixed(2)}).`
            });
            return;
        }

        this.collectingBillPayment = true;
        try {
            await this.saleService.collectPayment({
                saleId: this.selectedBillForPayment.id,
                amount: this.collectBillAmount,
                paymentMethod: this.collectBillPaymentMethod,
                notes: this.collectBillNotes?.trim() || undefined
            });

            this.messageService.add({
                severity: 'success',
                summary: 'Payment Collected',
                detail: `Collected PKR ${this.collectBillAmount.toFixed(2)} for invoice #${this.selectedBillForPayment.invoiceNo || this.selectedBillForPayment.id}`
            });
            this.collectBillDialogVisible = false;
            this.selectedBillForPayment = null;
            await this.loadUnpaidBills();
        } catch (err: any) {
            this.messageService.add({
                severity: 'error',
                summary: 'Failed',
                detail: err?.message || 'Failed to collect payment.'
            });
        } finally {
            this.collectingBillPayment = false;
        }
    }

    navigateToSales(): void {
        this.router.navigate(['/sales']);
    }

    onSignOut(): void {
        this.branchContext.clear();
        this.authService.logout();
        this.router.navigate(['/auth/login']);
    }
}
