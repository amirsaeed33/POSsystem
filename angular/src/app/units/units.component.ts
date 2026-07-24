import { ChangeDetectorRef, Component, Injector } from '@angular/core';
import { finalize } from 'rxjs/operators';
import { BsModalService, BsModalRef } from 'ngx-bootstrap/modal';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import {
  PagedListingComponentBase,
  PagedRequestDto,
} from '@shared/paged-listing-component-base';
import {
  UnitServiceProxy,
  UnitDto,
  UnitDtoPagedResultDto,
} from '@shared/service-proxies/service-proxies';
import { CreateUnitDialogComponent } from './create-unit/create-unit-dialog.component';
import { EditUnitDialogComponent } from './edit-unit/edit-unit-dialog.component';

class PagedUnitsRequestDto extends PagedRequestDto {
  keyword: string;
}

@Component({
  templateUrl: './units.component.html',
  animations: [appModuleAnimation()]
})
export class UnitsComponent extends PagedListingComponentBase<UnitDto> {
  units: UnitDto[] = [];
  keyword = '';

  constructor(
    injector: Injector,
    private _unitService: UnitServiceProxy,
    private _modalService: BsModalService,
    cd: ChangeDetectorRef
  ) {
    super(injector, cd);
  }

  list(
    request: PagedUnitsRequestDto,
    pageNumber: number,
    finishedCallback: Function
  ): void {
    request.keyword = this.keyword;

    this._unitService
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
      .subscribe((result: UnitDtoPagedResultDto) => {
        this.units = result.items;
        this.showPaging(result, pageNumber);
      });
  }

  delete(unit: UnitDto): void {
    abp.message.confirm(
      this.l('UnitDeleteWarningMessage', unit.name),
      undefined,
      (result: boolean) => {
        if (result) {
          this._unitService
            .delete(unit.id)
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

  createUnit(): void {
    this.showCreateOrEditUnitDialog();
  }

  editUnit(unit: UnitDto): void {
    this.showCreateOrEditUnitDialog(unit.id);
  }

  showCreateOrEditUnitDialog(id?: number): void {
    let createOrEditUnitDialog: BsModalRef;
    if (!id) {
      createOrEditUnitDialog = this._modalService.show(
        CreateUnitDialogComponent,
        {
          class: 'modal-lg',
        }
      );
    } else {
      createOrEditUnitDialog = this._modalService.show(
        EditUnitDialogComponent,
        {
          class: 'modal-lg',
          initialState: {
            id: id,
          },
        }
      );
    }

    createOrEditUnitDialog.content.onSave.subscribe(() => {
      this.refresh();
    });
  }
}
