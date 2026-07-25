import { Injectable } from '@angular/core';
import {
  HttpEvent,
  HttpHandler,
  HttpInterceptor,
  HttpRequest,
} from '@angular/common/http';
import { Observable } from 'rxjs';

/**
 * Free ngrok endpoints show an interstitial unless this header is present.
 */
@Injectable()
export class NgrokSkipBrowserWarningInterceptor implements HttpInterceptor {
  intercept(
    req: HttpRequest<any>,
    next: HttpHandler
  ): Observable<HttpEvent<any>> {
    if (
      req.url.includes('ngrok') ||
      window.location.hostname.includes('ngrok')
    ) {
      req = req.clone({
        setHeaders: {
          'ngrok-skip-browser-warning': 'true',
        },
      });
    }

    return next.handle(req);
  }
}
