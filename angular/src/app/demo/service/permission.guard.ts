import { Injectable } from '@angular/core';
import {
    ActivatedRouteSnapshot,
    CanActivate,
    CanActivateChild,
    Router,
    RouterStateSnapshot,
    UrlTree,
} from '@angular/router';
import { AuthService } from './auth.service';
import { PermissionService } from './permission.service';
import { SessionService } from './session.service';

@Injectable({
    providedIn: 'root',
})
export class PermissionGuard implements CanActivate, CanActivateChild {
    constructor(
        private authService: AuthService,
        private sessionService: SessionService,
        private permissionService: PermissionService,
        private router: Router
    ) {}

    canActivate(
        route: ActivatedRouteSnapshot,
        state: RouterStateSnapshot
    ): Promise<boolean | UrlTree> {
        return this.check(route, state);
    }

    canActivateChild(
        childRoute: ActivatedRouteSnapshot,
        state: RouterStateSnapshot
    ): Promise<boolean | UrlTree> {
        return this.check(childRoute, state);
    }

    private async check(
        route: ActivatedRouteSnapshot,
        state?: RouterStateSnapshot
    ): Promise<boolean | UrlTree> {
        if (!this.authService.isAuthenticated()) {
            return true;
        }

        const url = state?.url || '';

        // If user info is not cached yet, load it from session
        if (!this.authService.getUserInfo()) {
            try {
                const sessionInfo = await this.sessionService.getCurrentLoginInformations();
                if (sessionInfo?.user) {
                    this.authService.setUserInfo(sessionInfo.user);
                }
            } catch {
                // Ignore session load error
            }
        }

        // Delivery Rider Portal URL restriction:
        // Riders must see only My Dashboard and Order History.
        // They must not be able to access admin pages or other modules by changing URLs.
        if (this.authService.isDeliveryRider()) {
            const isRiderRoute = url.startsWith('/rider') ||
                url.startsWith('/auth/changepassword') ||
                url.startsWith('/notfound');

            if (!isRiderRoute) {
                return this.router.createUrlTree(['/rider/dashboard']);
            }
            return true;
        }

        // Restaurant Waiter URL restriction:
        // Waiters must see only the POS screen and dining tables.
        // They must not access dashboard, admin pages, sales, or any other modules.
        if (this.authService.isWaiter()) {
            const isWaiterRoute = url.startsWith('/pos') ||
                url.startsWith('/auth/changepassword') ||
                url.startsWith('/notfound');

            if (!isWaiterRoute) {
                return this.router.createUrlTree(['/pos']);
            }
            return true;
        }

        try {
            await this.permissionService.ensureLoaded();
        } catch {
            // If permissions cannot be loaded, deny protected routes.
            const permission = this.resolvePermission(route);
            const anyPermission = this.resolveAnyPermission(route);
            if (permission || anyPermission?.length) {
                return this.router.createUrlTree(['/auth/access']);
            }
            return true;
        }

        const permission = this.resolvePermission(route);
        if (!permission) {
            const anyPermission = this.resolveAnyPermission(route);
            if (!anyPermission?.length) {
                return true;
            }
            if (anyPermission.some((p) => this.permissionService.isGranted(p))) {
                return true;
            }
            return this.router.createUrlTree(['/auth/access']);
        }

        if (this.permissionService.isGranted(permission)) {
            return true;
        }

        return this.router.createUrlTree(['/auth/access']);
    }

    private resolvePermission(route: ActivatedRouteSnapshot): string | null {
        const permission = route.data?.['permission'];
        return typeof permission === 'string' && permission ? permission : null;
    }

    private resolveAnyPermission(route: ActivatedRouteSnapshot): string[] | null {
        const anyPermission = route.data?.['anyPermission'];
        return Array.isArray(anyPermission) && anyPermission.length
            ? anyPermission.filter((p) => typeof p === 'string' && p)
            : null;
    }
}
