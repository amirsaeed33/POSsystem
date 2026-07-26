import {
  Component,
  Injector,
  OnInit,
  Output,
  EventEmitter,
  ChangeDetectorRef
} from '@angular/core';
import { BsModalRef } from 'ngx-bootstrap/modal';
import { AppComponentBase } from '@shared/app-component-base';
import { AppConsts } from '@shared/AppConsts';
import { CompanyBrandingService } from '@shared/company-branding/company-branding.service';
import {
  CompanyProfileServiceProxy,
  CompanyProfileDto
} from '@shared/service-proxies/company-profile-service-proxy';

@Component({
  templateUrl: 'edit-company-profile-dialog.component.html'
})
export class EditCompanyProfileDialogComponent extends AppComponentBase
  implements OnInit {
  saving = false;
  companyProfile: CompanyProfileDto = new CompanyProfileDto();
  imagePreview: string | ArrayBuffer = '';
  id: number;

  @Output() onSave = new EventEmitter<any>();

  constructor(
    injector: Injector,
    public _companyProfileService: CompanyProfileServiceProxy,
    private _companyBranding: CompanyBrandingService,
    public bsModalRef: BsModalRef,
    private cd: ChangeDetectorRef
  ) {
    super(injector);
  }

  ngOnInit(): void {
    this._companyProfileService.get(this.id).subscribe((result: CompanyProfileDto) => {
      this.companyProfile = result;
      if (result.imagePath) {
        this.imagePreview = AppConsts.remoteServiceBaseUrl + result.imagePath;
      }
      this.cd.detectChanges();
    });
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!input.files || input.files.length === 0) {
      return;
    }

    const file = input.files[0];
    const reader = new FileReader();
    reader.onload = () => {
      this.imagePreview = reader.result;
      this.companyProfile.imageBase64 = reader.result as string;
      this.cd.detectChanges();
    };
    reader.readAsDataURL(file);
  }

  save(): void {
    this.saving = true;

    if (
      this.companyProfile.imageBase64 &&
      !this.companyProfile.imageBase64.startsWith('data:image')
    ) {
      this.companyProfile.imageBase64 = undefined;
    }

    this._companyProfileService.update(this.companyProfile).subscribe(
      () => {
        this.notify.info(this.l('SavedSuccessfully'));
        this._companyBranding.refresh().subscribe();
        this.bsModalRef.hide();
        this.onSave.emit();
      },
      () => {
        this.saving = false;
      }
    );
  }
}
