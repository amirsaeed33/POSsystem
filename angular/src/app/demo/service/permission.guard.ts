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

@Injectable({
    providedIn: 'root',
})
export class PermissionGuard implements CanActivate, CanActivateChild {
    constructor(
        private authService: AuthService,
        private permissionService: PermissionService,
        private router: Router
    ) {}

    canActivate(
        route: ActivatedRouteSnapshot,
        _state: RouterStateSnapshot
    ): Promise<boolean | UrlTree> {
        return this.check(route);
    }

    canActivateChild(
        childRoute: ActivatedRouteSnapshot,
        _state: RouterStateSnapshot
    ): Promise<boolean | UrlTree> {
        return this.check(childRoute);
    }

    private async check(
        route: ActivatedRouteSnapshot
    ): Promise<boolean | UrlTree> {
        if (!this.authService.isAuthenticated()) {
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
