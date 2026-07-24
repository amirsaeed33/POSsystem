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
  PurchaseServiceProxy,
  PurchaseDto,
  PurchaseReturnServiceProxy,
  PurchaseReturnDto
} from '@shared/service-proxies/service-proxies';
import { PrintPurchaseInvoiceDialogComponent } from '../print-purchase-invoice/print-purchase-invoice-dialog.component';
import { CreatePurchaseReturnDialogComponent } from '../create-purchase-return/create-purchase-return-dialog.component';

@Component({
  templateUrl: 'view-purchase-dialog.component.html'
})
export class ViewPurchaseDialogComponent extends AppComponentBase
  implements OnInit {
  id: number;
  purchase: PurchaseDto = new PurchaseDto();
  returns: PurchaseReturnDto[] = [];

  public _purchaseService = inject(PurchaseServiceProxy);
  private _purchaseReturnService = inject(PurchaseReturnServiceProxy);
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
    this._purchaseService.get(this.id).subscribe((result) => {
      this.purchase = result;
      this.cd.detectChanges();
    });

    this._purchaseReturnService.getAll(undefined, this.id, 0, 100).subscribe((result) => {
      this.returns = result.items || [];
      this.cd.detectChanges();
    });
  }

  returnProducts(): void {
    this.bsModalRef.hide();
    this._modalService.show(CreatePurchaseReturnDialogComponent, {
      class: 'modal-xl',
      initialState: { purchaseId: this.id },
    });
  }

  deleteReturn(purchaseReturn: PurchaseReturnDto): void {
    abp.message.confirm(
      this.l('PurchaseReturnDeleteWarningMessage', '#' + purchaseReturn.id),
      undefined,
      (result: boolean) => {
        if (result) {
          this._purchaseReturnService
            .delete(purchaseReturn.id)
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
    this._modalService.show(PrintPurchaseInvoiceDialogComponent, {
      class: 'modal-lg',
      initialState: { id: this.id },
    });
  }
}
