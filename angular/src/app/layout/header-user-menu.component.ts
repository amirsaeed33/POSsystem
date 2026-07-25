import { Component, ChangeDetectionStrategy, Injector } from '@angular/core';
import { AppComponentBase } from '@shared/app-component-base';
import { AppAuthService } from '@shared/auth/app-auth.service';
import { AppConsts } from '@shared/AppConsts';

@Component({
  selector: 'header-user-menu',
  templateUrl: './header-user-menu.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class HeaderUserMenuComponent extends AppComponentBase {
  constructor(injector: Injector, private _authService: AppAuthService) {
    super(injector);
  }

  get userImageUrl(): string {
    const path = this.appSession.user?.userImageUrl;
    if (path) {
      return AppConsts.remoteServiceBaseUrl + path;
    }
    return 'assets/img/user.png';
  }

  logout(): void {
    this._authService.logout();
  }
}
