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
            };
            const token = this.authService.getAccessToken();
            if (token) {
                headers['Authorization'] = `Bearer ${token}`;
            }

            // Sign-up / tenancy lookup must run on host (no Abp.TenantId).
            // Password / OTP login need the selected tenant cookie when present.
            const skipTenantHeader =
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
                const isOnlineOrder = req.url.includes('CustomerOrder') || this.router.url.includes('online-order');
                if (error.status === 401 && isApiRequest && !isOnlineOrder) {
                    this.authService.logout();
                    this.router.navigate(['/auth/login']);
                }
                return throwError(() => error);
            })
        );
    }
}
