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
import { CompanyBrandingService } from '@shared/company-branding/company-branding.service';
import {
  CreateCompanyProfileDto,
  CompanyProfileServiceProxy
} from '@shared/service-proxies/company-profile-service-proxy';

@Component({
  templateUrl: 'create-company-profile-dialog.component.html'
})
export class CreateCompanyProfileDialogComponent extends AppComponentBase
  implements OnInit {
  saving = false;
  companyProfile: CreateCompanyProfileDto = new CreateCompanyProfileDto();
  imagePreview: string | ArrayBuffer = '';

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
    this.cd.detectChanges();
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

    this._companyProfileService.create(this.companyProfile).subscribe(
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
