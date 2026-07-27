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
import { environment } from '../../../environments/environment';

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
    constructor(
        private authService: AuthService,
        private tenantContext: TenantContextService,
        private localizationService: LocalizationService,
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

            const tenantId = this.tenantContext.getTenantId();
            if (tenantId != null) {
                headers['Abp.TenantId'] = String(tenantId);
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
