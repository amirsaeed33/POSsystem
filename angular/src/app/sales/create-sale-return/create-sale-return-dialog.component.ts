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
  SaleReturnServiceProxy,
  CreateSaleReturnDto,
  CreateSaleReturnLineDto,
  SaleReturnableDto,
  SaleReturnableLineDto
} from '@shared/service-proxies/service-proxies';

class ReturnLineRow {
  saleLineId: number;
  productName: string;
  soldQuantity: number;
  returnedQuantity: number;
  returnableQuantity: number;
  unitPrice: number;
  returnQuantity: number;
}

@Component({
  templateUrl: 'create-sale-return-dialog.component.html'
})
export class CreateSaleReturnDialogComponent extends AppComponentBase
  implements OnInit {
  saving = false;
  loading = true;
  saleId: number;
  saleInfo: SaleReturnableDto = new SaleReturnableDto();
  lines: ReturnLineRow[] = [];
  returnDate: string;
  notes: string;

  @Output() onSave = new EventEmitter<any>();

  public _saleReturnService = inject(SaleReturnServiceProxy);
  public bsModalRef = inject(BsModalRef);
  private cd = inject(ChangeDetectorRef);

  constructor(injector: Injector) {
    super(injector);
  }

  ngOnInit(): void {
    this.returnDate = this.toDateInputValue();
    this._saleReturnService.getReturnableSale(this.saleId).subscribe(
      (result) => {
        this.saleInfo = result;
        this.lines = (result.lines || []).map((line: SaleReturnableLineDto) => ({
          saleLineId: line.saleLineId,
          productName: line.productName,
          soldQuantity: line.soldQuantity,
          returnedQuantity: line.returnedQuantity,
          returnableQuantity: line.returnableQuantity,
          unitPrice: line.unitPrice,
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
    return (line.returnQuantity || 0) * (line.unitPrice || 0);
  }

  get grandTotal(): number {
    return this.lines.reduce((sum, line) => sum + this.lineTotal(line), 0);
  }

  get hasReturnQty(): boolean {
    return this.lines.some((line) => (line.returnQuantity || 0) > 0);
  }

  save(): void {
    const payload = new CreateSaleReturnDto();
    payload.saleId = this.saleId;
    payload.returnDate = this.returnDate as any;
    payload.notes = this.notes;
    payload.lines = this.lines
      .filter((line) => (line.returnQuantity || 0) > 0)
      .map((line) => {
        const item = new CreateSaleReturnLineDto();
        item.saleLineId = line.saleLineId;
        item.quantity = line.returnQuantity;
        return item;
      });

    if (!payload.lines.length) {
      this.message.warn(this.l('NoReturnableProducts'));
      return;
    }

    this.saving = true;
    this._saleReturnService.create(payload).subscribe(
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
