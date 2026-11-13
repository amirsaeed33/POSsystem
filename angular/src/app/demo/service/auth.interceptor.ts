import { Injectable } from '@angular/core';
import {
    HttpErrorResponse,
    HttpEvent,
    HttpHandler,
    HttpInterceptor,
    HttpRequest,
} from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { AuthService } from './auth.service';
import { TenantContextService } from './tenant-context.service';
import { LocalizationService } from './localization.service';
import { BranchContextService } from './branch-context.service';
import { environment } from '../../../environments/environment';

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
    constructor(
        private authService: AuthService,
        private tenantContext: TenantContextService,
        private localizationService: LocalizationService,
        private branchContext: BranchContextService,
        private router: Router
    ) {}

    intercept(
        req: HttpRequest<any>,
        next: HttpHandler
    ): Observable<HttpEvent<any>> {
        let request = req;
        const isApiRequest = req.url.startsWith(environment.apiUrl);

        if (isApiRequest) {
            const headers: Record<string, string> = {
                '.AspNetCore.Culture':
                    this.localizationService.getCultureHeaderValue(),
                'ngrok-skip-browser-warning': 'true',
            };
            const anonymousApi = this.isAnonymousApi(req.url);
            const token = this.authService.getAccessToken();
            if (token && !anonymousApi) {
                headers['Authorization'] = `Bearer ${token}`;
            }

            // Sign-up / tenancy lookup and the public product page must run on host
            // (no Abp.TenantId). Password / OTP login need the selected tenant cookie.
            const skipTenantHeader =
                anonymousApi ||
                /\/api\/services\/app\/Account\/(SignUpTenant|IsTenantAvailable)/i.test(
                    req.url
                ) ||
                /\/api\/services\/app\/Branch\/ActivateBranch/i.test(req.url);
            const tenantId = this.tenantContext.getTenantId();
            if (!skipTenantHeader && tenantId != null) {
                headers['Abp.TenantId'] = String(tenantId);
            }

            const branchId = this.branchContext.getBranchId();
            if (branchId != null) {
                headers['SmartPos.BranchId'] = String(branchId);
            }

            request = req.clone({ setHeaders: headers });
        }

        return next.handle(request).pipe(
            catchError((error: HttpErrorResponse) => {
                const path = (this.router.url || '').split('?')[0];
                const isPublicPage =
                    path === '/product' ||
                    path.startsWith('/product/') ||
                    path.startsWith('/auth/') ||
                    path.startsWith('/online-order');
                if (
                    error.status === 401 &&
                    isApiRequest &&
                    !isPublicPage &&
                    !this.isAnonymousApi(req.url)
                ) {
                    this.authService.logout();
                    this.router.navigate(['/auth/login']);
                }

                // Extract ABP backend error message if present
                let abpMessage: string | null = null;
                if (error.error) {
                    if (typeof error.error === 'object') {
                        abpMessage = error.error.error?.message || error.error.message || error.error.details;
                    } else if (typeof error.error === 'string') {
                        try {
                            const parsed = JSON.parse(error.error);
                            abpMessage = parsed.error?.message || parsed.message || parsed.details;
                        } catch {}
                    }
                }

                if (abpMessage) {
                    try {
                        Object.defineProperty(error, 'message', {
                            value: abpMessage,
                            writable: true,
                            configurable: true,
                            enumerable: true,
                        });
                    } catch {
                        (error as any).message = abpMessage;
                    }
                }

                return throwError(() => error);
            })
        );
    }

    /** Landing and storefront calls that must succeed with no login. */
    private isAnonymousApi(url: string): boolean {
        return /\/api\/services\/app\/(Package\/GetPublicPackages|Developer\/GetPublicDevelopers|Seller\/(GetPublicSellers|Register)|Branch\/GetPublicFeedbacks|CustomerOrder\/GetStorefrontByHost)/i.test(
            url
        );
    }
}
