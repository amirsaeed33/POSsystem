import { Component, ElementRef, OnInit, ViewChild } from '@angular/core';
import { MenuItem, MessageService } from 'primeng/api';
import { Router } from '@angular/router';
import { LayoutService } from './service/app.layout.service';
import { AuthService } from 'src/app/demo/service/auth.service';
import { SessionService } from 'src/app/demo/service/session.service';
import { TenantContextService } from 'src/app/demo/service/tenant-context.service';
import { LocalizationService } from 'src/app/demo/service/localization.service';
import { BranchContextService } from 'src/app/demo/service/branch-context.service';
import { LanguageInfo } from 'src/app/demo/api/localization';
import { BranchDto } from 'src/app/demo/api/branch';
import { UserLoginInfoDto } from 'src/app/demo/api/session';
import { environment } from 'src/environments/environment';

@Component({
    selector: 'app-topbar',
    templateUrl: './app.topbar.component.html',
    providers: [MessageService],
})
export class AppTopBarComponent implements OnInit {
    @ViewChild('menubutton') menuButton!: ElementRef;
    @ViewChild('searchinput') searchInput!: ElementRef;

    searchActive = false;

    userInfo: UserLoginInfoDto | null = null;
    userDisplayName = 'User';
    userInitials = 'U';
    userRole = '';
    userImage: string | null = null;
    hasUserImage = false;

    languages: LanguageInfo[] = [];
    currentLanguage: LanguageInfo | null = null;
    changingLanguage = false;

    branches: BranchDto[] = [];
    currentBranch: BranchDto | null = null;

    constructor(
        public layoutService: LayoutService,
        private authService: AuthService,
        private sessionService: SessionService,
        private tenantContext: TenantContextService,
        private localizationService: LocalizationService,
        private branchContext: BranchContextService,
        private messageService: MessageService,
        private router: Router
    ) {}

    ngOnInit(): void {
        Promise.all([
            this.tenantContext.ensureMultiTenancyLoaded().catch(() => undefined),
            this.localizationService.ensureLoaded().catch(() => undefined),
        ]).finally(() => {
            this.languages = this.localizationService.getLanguages();
            this.currentLanguage = this.localizationService.getCurrentLanguage();
            this.loadUserInfo();
            this.loadBranches();
        });
    }

    onSelectBranch(branch: BranchDto): void {
        if (!branch || branch.id === this.currentBranch?.id) {
            return;
        }
        this.branchContext.setBranch(branch);
        this.currentBranch = branch;
    }

    private loadBranches(): void {
        if (!this.authService.isAuthenticated()) {
            return;
        }

        this.branchContext.branches$.subscribe((branches) => {
            this.branches = branches;
        });
        this.branchContext.currentBranch$.subscribe((branch) => {
            this.currentBranch = branch;
        });

        this.branchContext.ensureLoaded().catch(() => undefined);
    }

    onChangeLanguage(language: LanguageInfo): void {
        if (
            !language?.name ||
            language.name === this.currentLanguage?.name ||
            this.changingLanguage
        ) {
            return;
        }

        this.changingLanguage = true;
        this.localizationService.changeLanguage(language.name).catch((error) => {
            this.changingLanguage = false;
            this.messageService.add({
                severity: 'error',
                summary: 'Error',
                detail: error?.message || 'Failed to change language',
            });
        });
    }

    loadUserInfo(): void {
        const cachedUserInfo = this.authService.getUserInfo();
        if (cachedUserInfo) {
            this.setUserInfo(cachedUserInfo);
        }

        if (!this.authService.isAuthenticated()) {
            return;
        }

        this.sessionService
            .getCurrentLoginInformations()
            .then((sessionInfo) => {
                if (sessionInfo?.user) {
                    this.authService.setUserInfo(sessionInfo.user);
                    this.setUserInfo(sessionInfo.user);
                }
                if (sessionInfo?.tenant) {
                    this.tenantContext.setTenantInfo(sessionInfo.tenant);
                    if (sessionInfo.tenant.id) {
                        this.tenantContext.setTenantId(sessionInfo.tenant.id);
                    }
                    if (sessionInfo.user) {
                        this.setUserInfo(sessionInfo.user);
                    }
                } else {
                    this.tenantContext.setTenantInfo(null);
                    if (sessionInfo?.user) {
                        this.setUserInfo(sessionInfo.user);
                    }
                }
            })
            .catch(() => {
                if (!this.userInfo && cachedUserInfo) {
                    this.setUserInfo(cachedUserInfo);
                }
            });
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

    activateSearch(): void {
        this.searchActive = true;
        setTimeout(() => {
            this.searchInput?.nativeElement?.focus();
        }, 100);
    }

    deactivateSearch(): void {
        this.searchActive = false;
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
        this.authService.logout();
        this.router.navigate(['/auth/login']);
    }
}
