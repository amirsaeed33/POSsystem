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

            // Do not send a pre-selected tenant on login endpoints — backend resolves it from the user.
            const isLoginRequest = /\/api\/TokenAuth\/(Authenticate|AuthenticateWithEmailCode|SendEmailLoginCode|ExternalAuthenticate)/i.test(
                req.url
            );
            const tenantId = this.tenantContext.getTenantId();
            if (!isLoginRequest && tenantId != null) {
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
                if (error.status === 401 && isApiRequest) {
                    this.authService.logout();
                    this.router.navigate(['/auth/login']);
                }
                return throwError(() => error);
            })
        );
    }
}
