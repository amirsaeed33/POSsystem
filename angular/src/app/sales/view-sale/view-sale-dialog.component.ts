import {
  Component,
  Injector,
  OnInit,
  ChangeDetectorRef,
  inject
} from '@angular/core';
import { finalize } from 'rxjs/operators';
import { BsModalRef, BsModalService } from 'ngx-bootstrap/modal';
import { AppComponentBase } from '@shared/app-component-base';
import {
  SaleServiceProxy,
  SaleDto,
  SaleReturnServiceProxy,
  SaleReturnDto
} from '@shared/service-proxies/service-proxies';
import { PrintSaleInvoiceDialogComponent } from '../print-sale-invoice/print-sale-invoice-dialog.component';
import { CreateSaleReturnDialogComponent } from '../create-sale-return/create-sale-return-dialog.component';

@Component({
  templateUrl: 'view-sale-dialog.component.html'
})
export class ViewSaleDialogComponent extends AppComponentBase
  implements OnInit {
  id: number;
  sale: SaleDto = new SaleDto();
  returns: SaleReturnDto[] = [];

  public _saleService = inject(SaleServiceProxy);
  private _saleReturnService = inject(SaleReturnServiceProxy);
  public bsModalRef = inject(BsModalRef);
  private _modalService = inject(BsModalService);
  private cd = inject(ChangeDetectorRef);

  constructor(injector: Injector) {
    super(injector);
  }

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this._saleService.get(this.id).subscribe((result) => {
      this.sale = result;
      this.cd.detectChanges();
    });

    this._saleReturnService.getAll(undefined, this.id, 0, 100).subscribe((result) => {
      this.returns = result.items || [];
      this.cd.detectChanges();
    });
  }

  returnProducts(): void {
    this.bsModalRef.hide();
    const dialog = this._modalService.show(CreateSaleReturnDialogComponent, {
      class: 'modal-xl',
      initialState: { saleId: this.id },
    });
    dialog.content.onSave.subscribe(() => {});
  }

  deleteReturn(saleReturn: SaleReturnDto): void {
    abp.message.confirm(
      this.l('SaleReturnDeleteWarningMessage', '#' + saleReturn.id),
      undefined,
      (result: boolean) => {
        if (result) {
          this._saleReturnService
            .delete(saleReturn.id)
            .pipe(
              finalize(() => {
                abp.notify.success(this.l('SuccessfullyDeleted'));
                this.load();
              })
            )
            .subscribe(() => {});
        }
      }
    );
  }

  printInvoice(): void {
    this.bsModalRef.hide();
    this._modalService.show(PrintSaleInvoiceDialogComponent, {
      class: 'modal-lg',
      initialState: { id: this.id },
    });
  }
}
