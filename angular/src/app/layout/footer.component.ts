import {
  Component,
  Injector,
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  OnInit
} from '@angular/core';
import { AppComponentBase } from '@shared/app-component-base';
import { CompanyBrandingService } from '@shared/company-branding/company-branding.service';

@Component({
  selector: 'app-footer',
  templateUrl: './footer.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class FooterComponent extends AppComponentBase implements OnInit {
  currentYear: number;
  versionText: string;
  softwareTitle = 'SmartPos';

  constructor(
    injector: Injector,
    private _companyBranding: CompanyBrandingService,
    private cd: ChangeDetectorRef
  ) {
    super(injector);

    this.currentYear = new Date().getFullYear();
    this.versionText =
      this.appSession.application.version +
      ' [' +
      this.appSession.application.releaseDate.format('YYYYDDMM') +
      ']';
  }

  ngOnInit(): void {
    this._companyBranding.ensureLoaded().subscribe(() => {
      this.softwareTitle = this._companyBranding.softwareTitle;
      this.cd.markForCheck();
    });
  }
}
