import { ChangeDetectorRef, Component, Injector } from '@angular/core';
import { finalize } from 'rxjs/operators';
import { BsModalService, BsModalRef } from 'ngx-bootstrap/modal';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import {
  PagedListingComponentBase,
  PagedRequestDto,
} from '@shared/paged-listing-component-base';
import {
  BrandServiceProxy,
  BrandDto,
  BrandDtoPagedResultDto,
} from '@shared/service-proxies/service-proxies';
import { CreateBrandDialogComponent } from './create-brand/create-brand-dialog.component';
import { EditBrandDialogComponent } from './edit-brand/edit-brand-dialog.component';

class PagedBrandsRequestDto extends PagedRequestDto {
  keyword: string;
}

@Component({
  templateUrl: './brands.component.html',
  animations: [appModuleAnimation()]
})
export class BrandsComponent extends PagedListingComponentBase<BrandDto> {
  brands: BrandDto[] = [];
  keyword = '';

  constructor(
    injector: Injector,
    private _brandService: BrandServiceProxy,
    private _modalService: BsModalService,
    cd: ChangeDetectorRef
  ) {
    super(injector, cd);
  }

  list(
    request: PagedBrandsRequestDto,
    pageNumber: number,
    finishedCallback: Function
  ): void {
    request.keyword = this.keyword;

    this._brandService
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
      .subscribe((result: BrandDtoPagedResultDto) => {
        this.brands = result.items;
        this.showPaging(result, pageNumber);
      });
  }

  delete(brand: BrandDto): void {
    abp.message.confirm(
      this.l('BrandDeleteWarningMessage', brand.name),
      undefined,
      (result: boolean) => {
        if (result) {
          this._brandService
            .delete(brand.id)
            .pipe(
              finalize(() => {
                abp.notify.success(this.l('SuccessfullyDeleted'));
                this.refresh();
              })
            )
            .subscribe(() => {});
        }
      }
    );
  }

  createBrand(): void {
    this.showCreateOrEditBrandDialog();
  }

  editBrand(brand: BrandDto): void {
    this.showCreateOrEditBrandDialog(brand.id);
  }

  showCreateOrEditBrandDialog(id?: number): void {
    let createOrEditBrandDialog: BsModalRef;
    if (!id) {
      createOrEditBrandDialog = this._modalService.show(
        CreateBrandDialogComponent,
        {
          class: 'modal-lg',
        }
      );
    } else {
      createOrEditBrandDialog = this._modalService.show(
        EditBrandDialogComponent,
        {
          class: 'modal-lg',
          initialState: {
            id: id,
          },
        }
      );
    }

    createOrEditBrandDialog.content.onSave.subscribe(() => {
      this.refresh();
    });
  }
}
