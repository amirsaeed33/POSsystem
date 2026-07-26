import { ChangeDetectorRef, Component, Injector } from '@angular/core';
import { finalize } from 'rxjs/operators';
import { BsModalService, BsModalRef } from 'ngx-bootstrap/modal';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import {
  PagedListingComponentBase,
  PagedRequestDto,
} from '@shared/paged-listing-component-base';
import { AppConsts } from '@shared/AppConsts';
import { CompanyBrandingService } from '@shared/company-branding/company-branding.service';
import {
  CompanyProfileServiceProxy,
  CompanyProfileDto,
  CompanyProfileDtoPagedResultDto,
} from '@shared/service-proxies/company-profile-service-proxy';
import { CreateCompanyProfileDialogComponent } from './create-company-profile/create-company-profile-dialog.component';
import { EditCompanyProfileDialogComponent } from './edit-company-profile/edit-company-profile-dialog.component';

class PagedCompanyProfilesRequestDto extends PagedRequestDto {
  keyword: string;
}

@Component({
  templateUrl: './company-profiles.component.html',
  animations: [appModuleAnimation()]
})
export class CompanyProfilesComponent extends PagedListingComponentBase<CompanyProfileDto> {
  companyProfiles: CompanyProfileDto[] = [];
  keyword = '';

  constructor(
    injector: Injector,
    private _companyProfileService: CompanyProfileServiceProxy,
    private _companyBranding: CompanyBrandingService,
    private _modalService: BsModalService,
    cd: ChangeDetectorRef
  ) {
    super(injector, cd);
  }

  getImageUrl(companyProfile: CompanyProfileDto): string {
    if (!companyProfile?.imagePath) {
      return '';
    }
    return AppConsts.remoteServiceBaseUrl + companyProfile.imagePath;
  }

  list(
    request: PagedCompanyProfilesRequestDto,
    pageNumber: number,
    finishedCallback: Function
  ): void {
    request.keyword = this.keyword;

    this._companyProfileService
      .getAll(
        request.keyword,
        request.skipCount,
        request.maxResultCount
      )
      .pipe(
        finalize(() => {
          finishedCallback();
        })
      )
      .subscribe((result: CompanyProfileDtoPagedResultDto) => {
        this.companyProfiles = result.items;
        this.showPaging(result, pageNumber);
      });
  }

  delete(companyProfile: CompanyProfileDto): void {
    abp.message.confirm(
      this.l('CompanyProfileDeleteWarningMessage', companyProfile.name),
      undefined,
      (result: boolean) => {
        if (result) {
          this._companyProfileService
            .delete(companyProfile.id)
            .pipe(
              finalize(() => {
                abp.notify.success(this.l('SuccessfullyDeleted'));
                this._companyBranding.refresh().subscribe();
                this.refresh();
              })
            )
            .subscribe(() => {});
        }
      }
    );
  }

  createCompanyProfile(): void {
    this.showCreateOrEditCompanyProfileDialog();
  }

  editCompanyProfile(companyProfile: CompanyProfileDto): void {
    this.showCreateOrEditCompanyProfileDialog(companyProfile.id);
  }

  showCreateOrEditCompanyProfileDialog(id?: number): void {
    let createOrEditDialog: BsModalRef;
    if (!id) {
      createOrEditDialog = this._modalService.show(
        CreateCompanyProfileDialogComponent,
        {
          class: 'modal-lg',
        }
      );
    } else {
      createOrEditDialog = this._modalService.show(
        EditCompanyProfileDialogComponent,
        {
          class: 'modal-lg',
          initialState: {
            id: id,
          },
        }
      );
    }

    createOrEditDialog.content.onSave.subscribe(() => {
      this.refresh();
    });
  }
}
