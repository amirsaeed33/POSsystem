import {
  Component,
  ChangeDetectionStrategy,
  Injector,
  OnInit
} from '@angular/core';
import { AppComponentBase } from '@shared/app-component-base';
import { AppConsts } from '@shared/AppConsts';

@Component({
  selector: 'sidebar-user-panel',
  templateUrl: './sidebar-user-panel.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SidebarUserPanelComponent extends AppComponentBase
  implements OnInit {
  shownLoginName = '';

  constructor(injector: Injector) {
    super(injector);
  }

  ngOnInit() {
    this.shownLoginName = this.appSession.getShownLoginName();
  }

  get userImageUrl(): string {
    const path = this.appSession.user?.userImageUrl;
    if (path) {
      return AppConsts.remoteServiceBaseUrl + path;
    }
    return 'assets/img/user.png';
  }
}
