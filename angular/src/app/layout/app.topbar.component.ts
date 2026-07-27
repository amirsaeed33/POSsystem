import { Component, ElementRef, OnInit, ViewChild } from '@angular/core';
import { MenuItem, MessageService } from 'primeng/api';
import { Router } from '@angular/router';
import { LayoutService } from './service/app.layout.service';
import { AuthService } from 'src/app/demo/service/auth.service';
import { SessionService } from 'src/app/demo/service/session.service';
import { TenantContextService } from 'src/app/demo/service/tenant-context.service';
import { LocalizationService } from 'src/app/demo/service/localization.service';
import { LanguageInfo } from 'src/app/demo/api/localization';
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
    userRole = '';
    userImage = 'assets/layout/images/avatar.png';

    languages: LanguageInfo[] = [];
    currentLanguage: LanguageInfo | null = null;
    changingLanguage = false;

    constructor(
        public layoutService: LayoutService,
        private authService: AuthService,
        private sessionService: SessionService,
        private tenantContext: TenantContextService,
        private localizationService: LocalizationService,
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
        });
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
        const baseName =
            `${user.name || ''} ${user.surname || ''}`.trim() ||
            user.userName ||
            'User';

        if (this.tenantContext.isMultiTenancyEnabled()) {
            const tenant = this.tenantContext.getTenantInfo();
            const prefix = tenant?.tenancyName || '.';
            this.userDisplayName = `${prefix}\\${user.userName || baseName}`;
        } else {
            this.userDisplayName = baseName;
        }

        if (user.profilePictureUrl) {
            if (
                user.profilePictureUrl.startsWith('http://') ||
                user.profilePictureUrl.startsWith('https://')
            ) {
                this.userImage = user.profilePictureUrl;
            } else {
                this.userImage = `${environment.apiUrl}${
                    user.profilePictureUrl.startsWith('/') ? '' : '/'
                }${user.profilePictureUrl}`;
            }
        } else {
            this.userImage = 'assets/layout/images/avatar.png';
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

    onImageError(event: Event): void {
        const img = event.target as HTMLImageElement;
        if (img) {
            img.src = 'assets/layout/images/avatar.png';
        }
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
