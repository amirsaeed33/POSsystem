import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, of } from 'rxjs';
import { catchError, shareReplay, tap } from 'rxjs/operators';
import { AppConsts } from '@shared/AppConsts';
import {
  CompanyProfileDto,
  CompanyProfileServiceProxy
} from '@shared/service-proxies/company-profile-service-proxy';

@Injectable({ providedIn: 'root' })
export class CompanyBrandingService {
  private readonly profileSubject = new BehaviorSubject<CompanyProfileDto | null>(null);
  private load$?: Observable<CompanyProfileDto | null>;

  readonly profile$ = this.profileSubject.asObservable();

  constructor(private _companyProfileService: CompanyProfileServiceProxy) {}

  get profile(): CompanyProfileDto | null {
    return this.profileSubject.value;
  }

  get softwareTitle(): string {
    return this.profile?.name || 'SmartPos';
  }

  getLogoUrl(): string {
    const path = this.profile?.imagePath;
    if (path) {
      return AppConsts.remoteServiceBaseUrl + path;
    }
    return 'assets/img/logo.png';
  }

  ensureLoaded(force = false): Observable<CompanyProfileDto | null> {
    if (!force && this.profileSubject.value) {
      return of(this.profileSubject.value);
    }
    if (!force && this.load$) {
      return this.load$;
    }

    this.load$ = this._companyProfileService.getCurrent().pipe(
      catchError(() => of(null)),
      tap((profile) => {
        this.profileSubject.next(profile);
        if (profile?.name) {
          document.title = profile.name;
        }
      }),
      shareReplay(1)
    );
    return this.load$;
  }

  refresh(): Observable<CompanyProfileDto | null> {
    this.load$ = undefined;
    return this.ensureLoaded(true);
  }
}
