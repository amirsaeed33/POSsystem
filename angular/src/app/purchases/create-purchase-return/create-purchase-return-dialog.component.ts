import {
  Component,
  Injector,
  OnInit,
  Output,
  EventEmitter,
  ChangeDetectorRef,
  inject
} from '@angular/core';
import { BsModalRef } from 'ngx-bootstrap/modal';
import { AppComponentBase } from '@shared/app-component-base';
import {
  PurchaseReturnServiceProxy,
  CreatePurchaseReturnDto,
  CreatePurchaseReturnLineDto,
  PurchaseReturnableDto,
  PurchaseReturnableLineDto
} from '@shared/service-proxies/service-proxies';

class ReturnLineRow {
  purchaseLineId: number;
  productName: string;
  purchasedQuantity: number;
  returnedQuantity: number;
  returnableQuantity: number;
  unitCost: number;
  returnQuantity: number;
}

@Component({
  templateUrl: 'create-purchase-return-dialog.component.html'
})
export class CreatePurchaseReturnDialogComponent extends AppComponentBase
  implements OnInit {
  saving = false;
  loading = true;
  purchaseId: number;
  purchaseInfo: PurchaseReturnableDto = new PurchaseReturnableDto();
  lines: ReturnLineRow[] = [];
  returnDate: string;
  notes: string;

  @Output() onSave = new EventEmitter<any>();

  public _purchaseReturnService = inject(PurchaseReturnServiceProxy);
  public bsModalRef = inject(BsModalRef);
  private cd = inject(ChangeDetectorRef);

  constructor(injector: Injector) {
    super(injector);
  }

  ngOnInit(): void {
    this.returnDate = this.toDateInputValue();
    this._purchaseReturnService.getReturnablePurchase(this.purchaseId).subscribe(
      (result) => {
        this.purchaseInfo = result;
        this.lines = (result.lines || []).map((line: PurchaseReturnableLineDto) => ({
          purchaseLineId: line.purchaseLineId,
          productName: line.productName,
          purchasedQuantity: line.purchasedQuantity,
          returnedQuantity: line.returnedQuantity,
          returnableQuantity: line.returnableQuantity,
          unitCost: line.unitCost,
          returnQuantity: 0
        }));
        this.loading = false;
        this.cd.detectChanges();
      },
      () => {
        this.loading = false;
        this.cd.detectChanges();
      }
    );
  }

  lineTotal(line: ReturnLineRow): number {
    return (line.returnQuantity || 0) * (line.unitCost || 0);
  }

  get grandTotal(): number {
    return this.lines.reduce((sum, line) => sum + this.lineTotal(line), 0);
  }

  get hasReturnQty(): boolean {
    return this.lines.some((line) => (line.returnQuantity || 0) > 0);
  }

  save(): void {
    const payload = new CreatePurchaseReturnDto();
    payload.purchaseId = this.purchaseId;
    payload.returnDate = this.returnDate as any;
    payload.notes = this.notes;
    payload.lines = this.lines
      .filter((line) => (line.returnQuantity || 0) > 0)
      .map((line) => {
        const item = new CreatePurchaseReturnLineDto();
        item.purchaseLineId = line.purchaseLineId;
        item.quantity = line.returnQuantity;
        return item;
      });

    if (!payload.lines.length) {
      this.message.warn(this.l('NoReturnablePurchaseProducts'));
      return;
    }

    this.saving = true;
    this._purchaseReturnService.create(payload).subscribe(
      () => {
        this.notify.info(this.l('SavedSuccessfully'));
        this.bsModalRef.hide();
        this.onSave.emit();
      },
      () => {
        this.saving = false;
      }
    );
  }
}
