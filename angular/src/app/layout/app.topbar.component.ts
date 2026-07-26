import { Component, ElementRef, OnInit, ViewChild } from '@angular/core';
import { MenuItem } from 'primeng/api';
import { Router } from '@angular/router';
import { LayoutService } from "./service/app.layout.service";
import { AuthService } from 'src/app/demo/service/auth.service';
import { SessionService } from 'src/app/demo/service/session.service';
import { UserLoginInfoDto } from 'src/app/demo/api/session';
import { environment } from 'src/environments/environment';

@Component({
    selector: 'app-topbar',
    templateUrl: './app.topbar.component.html'
})
export class AppTopBarComponent implements OnInit {

    menu: MenuItem[] = [];

    @ViewChild('searchinput') searchInput!: ElementRef;

    @ViewChild('menubutton') menuButton!: ElementRef;

    searchActive: boolean = false;

    userInfo: UserLoginInfoDto | null = null;
    userDisplayName: string = 'User';
    userRole: string = '';
    userImage: string = 'assets/layout/images/avatar.png';

    constructor(
        public layoutService: LayoutService,
        private authService: AuthService,
        private sessionService: SessionService,
        private router: Router
    ) {}

    ngOnInit(): void {
        this.loadUserInfo();
    }

    loadUserInfo(): void {
        // First try to get from localStorage
        const cachedUserInfo = this.authService.getUserInfo();
        if (cachedUserInfo) {
            this.setUserInfo(cachedUserInfo);
        }

        // Then fetch from API to get latest info
        if (this.authService.isAuthenticated()) {
            this.sessionService.getCurrentLoginInformations()
                .then((sessionInfo) => {
                    if (sessionInfo?.user) {
                        this.authService.setUserInfo(sessionInfo.user);
                        this.setUserInfo(sessionInfo.user);
                    }
                })
                .catch(() => {
                    // If API call fails, use cached info or defaults
                    if (!this.userInfo && cachedUserInfo) {
                        this.setUserInfo(cachedUserInfo);
                    }
                });
        }
    }

    setUserInfo(user: UserLoginInfoDto): void {
        this.userInfo = user;
        this.userDisplayName = `${user.name || ''} ${user.surname || ''}`.trim() || user.userName || 'User';
        
        // Set user image if available, otherwise use default
        if (user.profilePictureUrl) {
            // Construct full URL from relative path
            if (user.profilePictureUrl.startsWith('http://') || user.profilePictureUrl.startsWith('https://')) {
                this.userImage = user.profilePictureUrl;
            } else {
                // It's a relative path, construct full URL
                this.userImage = `${environment.apiUrl}${user.profilePictureUrl.startsWith('/') ? '' : '/'}${user.profilePictureUrl}`;
            }
        } else {
            // You can add logic here to generate avatar from initials or use a default
            this.userImage = 'assets/layout/images/avatar.png';
        }
        
        // Get role from roleNames array
        if (user.roleNames && Array.isArray(user.roleNames) && user.roleNames.length > 0) {
            // Get the first role, or 'Admin' if it contains Admin
            const roles = user.roleNames;
            const adminRole = roles.find(r => r && r.toLowerCase().includes('admin'));
            if (adminRole) {
                this.userRole = 'Admin';
            } else {
                // Capitalize first letter of first role
                const firstRole = roles[0];
                if (firstRole) {
                    this.userRole = firstRole.charAt(0).toUpperCase() + firstRole.slice(1).toLowerCase();
                } else {
                    this.userRole = 'User';
                }
            }
        } else {
            // Default role if no roles found
            this.userRole = 'User';
        }
    }

    onImageError(event: Event): void {
        const img = event.target as HTMLImageElement;
        if (img) {
            img.src = 'assets/layout/images/avatar.png';
        }
    }

    onMenuButtonClick() {
        this.layoutService.onMenuToggle();
    }

    activateSearch() {
        this.searchActive = true;
        setTimeout(() => {
            this.searchInput.nativeElement.focus();
        }, 100);
    }

    deactivateSearch() {
        this.searchActive = false;
    }

    removeTab(event: MouseEvent, item: MenuItem, index: number) {
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
        const logo = (this.layoutTheme === 'primaryColor'  && !(this.layoutService.config().theme  == "yellow")) ? 'light.png' : (this.colorScheme === 'light' ? 'dark.png' : 'light.png');
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
