import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  Injector,
  OnInit
} from '@angular/core';
import { AppComponentBase } from '@shared/app-component-base';
import { CompanyBrandingService } from '@shared/company-branding/company-branding.service';

@Component({
  selector: 'account-header',
  templateUrl: './account-header.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AccountHeaderComponent extends AppComponentBase implements OnInit {
  softwareTitle = 'SmartPos';
  logoUrl = '';

  constructor(
    injector: Injector,
    private _companyBranding: CompanyBrandingService,
    private cd: ChangeDetectorRef
  ) {
    super(injector);
  }

  ngOnInit(): void {
    this._companyBranding.ensureLoaded().subscribe(() => {
      this.softwareTitle = this._companyBranding.softwareTitle;
      this.logoUrl = this._companyBranding.profile?.imagePath
        ? this._companyBranding.getLogoUrl()
        : '';
      this.cd.markForCheck();
    });
  }
}
