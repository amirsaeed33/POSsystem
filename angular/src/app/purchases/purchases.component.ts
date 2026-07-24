import { ChangeDetectorRef, Component, Injector } from '@angular/core';
import { finalize } from 'rxjs/operators';
import { BsModalService, BsModalRef } from 'ngx-bootstrap/modal';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import {
  PagedListingComponentBase,
  PagedRequestDto,
} from '@shared/paged-listing-component-base';
import {
  PurchaseServiceProxy,
  PurchaseDto,
  PurchaseDtoPagedResultDto,
} from '@shared/service-proxies/service-proxies';
import { CreatePurchaseDialogComponent } from './create-purchase/create-purchase-dialog.component';
import { CreatePurchaseReturnDialogComponent } from './create-purchase-return/create-purchase-return-dialog.component';
import { ViewPurchaseDialogComponent } from './view-purchase/view-purchase-dialog.component';
import { PrintPurchaseInvoiceDialogComponent } from './print-purchase-invoice/print-purchase-invoice-dialog.component';

class PagedPurchasesRequestDto extends PagedRequestDto {
  keyword: string;
}

@Component({
  templateUrl: './purchases.component.html',
  animations: [appModuleAnimation()]
})
export class PurchasesComponent extends PagedListingComponentBase<PurchaseDto> {
  purchases: PurchaseDto[] = [];
  keyword = '';

  constructor(
    injector: Injector,
    private _purchaseService: PurchaseServiceProxy,
    private _modalService: BsModalService,
    cd: ChangeDetectorRef
  ) {
    super(injector, cd);
  }

  list(
    request: PagedPurchasesRequestDto,
    pageNumber: number,
    finishedCallback: Function
  ): void {
    request.keyword = this.keyword;

    this._purchaseService
      .getAll(request.keyword, undefined, request.skipCount, request.maxResultCount)
      .pipe(finalize(() => finishedCallback()))
      .subscribe((result: PurchaseDtoPagedResultDto) => {
        this.purchases = result.items;
        this.showPaging(result, pageNumber);
      });
  }

  delete(purchase: PurchaseDto): void {
    abp.message.confirm(
      this.l('PurchaseDeleteWarningMessage', purchase.invoiceNo || ('#' + purchase.id)),
      undefined,
      (result: boolean) => {
        if (result) {
          this._purchaseService
            .delete(purchase.id)
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

  createPurchase(): void {
    const dialog: BsModalRef = this._modalService.show(CreatePurchaseDialogComponent, {
      class: 'modal-xl',
    });
    dialog.content.onSave.subscribe(() => this.refresh());
  }

  returnPurchase(purchase: PurchaseDto): void {
    const dialog: BsModalRef = this._modalService.show(CreatePurchaseReturnDialogComponent, {
      class: 'modal-xl',
      initialState: { purchaseId: purchase.id },
    });
    dialog.content.onSave.subscribe(() => this.refresh());
  }

  viewPurchase(purchase: PurchaseDto): void {
    this._modalService.show(ViewPurchaseDialogComponent, {
      class: 'modal-lg',
      initialState: { id: purchase.id },
    });
  }

  printInvoice(purchase: PurchaseDto): void {
    this._modalService.show(PrintPurchaseInvoiceDialogComponent, {
      class: 'modal-lg',
      initialState: { id: purchase.id },
    });
  }
}
