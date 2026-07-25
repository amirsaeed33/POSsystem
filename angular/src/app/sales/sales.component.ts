import { ChangeDetectorRef, Component, Injector } from '@angular/core';
import { finalize } from 'rxjs/operators';
import { BsModalService, BsModalRef } from 'ngx-bootstrap/modal';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import {
  PagedListingComponentBase,
  PagedRequestDto,
} from '@shared/paged-listing-component-base';
import {
  SaleServiceProxy,
  SaleDto,
  SaleDtoPagedResultDto,
} from '@shared/service-proxies/service-proxies';
import { CreateSaleDialogComponent } from './create-sale/create-sale-dialog.component';
import { CreateSaleReturnDialogComponent } from './create-sale-return/create-sale-return-dialog.component';
import { ViewSaleDialogComponent } from './view-sale/view-sale-dialog.component';
import { PrintSaleInvoiceDialogComponent } from './print-sale-invoice/print-sale-invoice-dialog.component';

class PagedSalesRequestDto extends PagedRequestDto {
  keyword: string;
}

@Component({
  templateUrl: './sales.component.html',
  animations: [appModuleAnimation()]
})
export class SalesComponent extends PagedListingComponentBase<SaleDto> {
  sales: SaleDto[] = [];
  keyword = '';

  constructor(
    injector: Injector,
    private _saleService: SaleServiceProxy,
    private _modalService: BsModalService,
    cd: ChangeDetectorRef
  ) {
    super(injector, cd);
  }

  paymentTypeLabel(paymentType: number): string {
    switch (paymentType) {
      case 0: return this.l('PaymentTypeCash');
      case 1: return this.l('PaymentTypeCard');
      case 2: return this.l('PaymentTypeCredit');
      case 3: return this.l('PaymentTypeMixed');
      default: return String(paymentType ?? '');
    }
  }

  list(
    request: PagedSalesRequestDto,
    pageNumber: number,
    finishedCallback: Function
  ): void {
    request.keyword = this.keyword;

    this._saleService
      .getAll(request.keyword, undefined, request.skipCount, request.maxResultCount)
      .pipe(finalize(() => finishedCallback()))
      .subscribe((result: SaleDtoPagedResultDto) => {
        this.sales = result.items;
        this.showPaging(result, pageNumber);
      });
  }

  delete(sale: SaleDto): void {
    abp.message.confirm(
      this.l('SaleDeleteWarningMessage', sale.invoiceNo || ('#' + sale.id)),
      undefined,
      (result: boolean) => {
        if (result) {
          this._saleService
            .delete(sale.id)
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

  createSale(): void {
    const dialog: BsModalRef = this._modalService.show(CreateSaleDialogComponent, {
      class: 'modal-xl',
    });
    dialog.content.onSave.subscribe(() => this.refresh());
  }

  returnSale(sale: SaleDto): void {
    const dialog: BsModalRef = this._modalService.show(CreateSaleReturnDialogComponent, {
      class: 'modal-xl',
      initialState: { saleId: sale.id },
    });
    dialog.content.onSave.subscribe(() => this.refresh());
  }

  viewSale(sale: SaleDto): void {
    this._modalService.show(ViewSaleDialogComponent, {
      class: 'modal-lg',
      initialState: { id: sale.id },
    });
  }

  printInvoice(sale: SaleDto): void {
    this._modalService.show(PrintSaleInvoiceDialogComponent, {
      class: 'modal-lg',
      initialState: { id: sale.id },
    });
  }
}
