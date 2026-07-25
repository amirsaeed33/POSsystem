import { ChangeDetectorRef, Component, Injector } from '@angular/core';
import { finalize } from 'rxjs/operators';
import { BsModalService, BsModalRef } from 'ngx-bootstrap/modal';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import {
  PagedListingComponentBase,
  PagedRequestDto,
} from '@shared/paged-listing-component-base';
import {
  StockAdjustmentDto,
  StockAdjustmentDtoPagedResultDto,
  StockAdjustmentServiceProxy,
} from '@shared/service-proxies/stock-adjustment-service-proxy';
import { CreateStockAdjustmentDialogComponent } from './create-stock-adjustment/create-stock-adjustment-dialog.component';
import { ViewStockAdjustmentDialogComponent } from './view-stock-adjustment/view-stock-adjustment-dialog.component';

class PagedStockAdjustmentsRequestDto extends PagedRequestDto {
  keyword: string;
}

@Component({
  templateUrl: './stock-adjustments.component.html',
  animations: [appModuleAnimation()]
})
export class StockAdjustmentsComponent extends PagedListingComponentBase<StockAdjustmentDto> {
  adjustments: StockAdjustmentDto[] = [];
  keyword = '';

  constructor(
    injector: Injector,
    private _adjustmentService: StockAdjustmentServiceProxy,
    private _modalService: BsModalService,
    cd: ChangeDetectorRef
  ) {
    super(injector, cd);
  }

  list(
    request: PagedStockAdjustmentsRequestDto,
    pageNumber: number,
    finishedCallback: Function
  ): void {
    request.keyword = this.keyword;

    this._adjustmentService
      .getAll(request.keyword, request.skipCount, request.maxResultCount)
      .pipe(finalize(() => finishedCallback()))
      .subscribe((result: StockAdjustmentDtoPagedResultDto) => {
        this.adjustments = result.items;
        this.showPaging(result, pageNumber);
      });
  }

  reasonLabel(reason: number): string {
    switch (reason) {
      case 0: return this.l('AdjustmentReasonOpening');
      case 1: return this.l('AdjustmentReasonDamage');
      case 2: return this.l('AdjustmentReasonLoss');
      case 3: return this.l('AdjustmentReasonRecount');
      default: return this.l('AdjustmentReasonOther');
    }
  }

  delete(item: StockAdjustmentDto): void {
    abp.message.confirm(
      this.l('StockAdjustmentDeleteWarningMessage', item.referenceNo || ('#' + item.id)),
      undefined,
      (result: boolean) => {
        if (result) {
          this._adjustmentService
            .delete(item.id)
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

  createAdjustment(): void {
    const dialog: BsModalRef = this._modalService.show(CreateStockAdjustmentDialogComponent, {
      class: 'modal-lg',
    });
    dialog.content.onSave.subscribe(() => this.refresh());
  }

  viewAdjustment(item: StockAdjustmentDto): void {
    this._modalService.show(ViewStockAdjustmentDialogComponent, {
      class: 'modal-lg',
      initialState: { id: item.id },
    });
  }
}
