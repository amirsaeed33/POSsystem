import {
  Component,
  Injector,
  OnInit,
  Output,
  EventEmitter,
  ChangeDetectorRef
} from '@angular/core';
import { BsModalRef } from 'ngx-bootstrap/modal';
import { AppComponentBase } from '@shared/app-component-base';
import { ProductDto, ProductServiceProxy } from '@shared/service-proxies/service-proxies';
import {
  CreateStockAdjustmentDto,
  CreateStockAdjustmentLineDto,
  StockAdjustmentReasons,
  StockAdjustmentServiceProxy
} from '@shared/service-proxies/stock-adjustment-service-proxy';

@Component({
  templateUrl: 'create-stock-adjustment-dialog.component.html'
})
export class CreateStockAdjustmentDialogComponent extends AppComponentBase implements OnInit {
  saving = false;
  adjustment: CreateStockAdjustmentDto = new CreateStockAdjustmentDto();
  products: ProductDto[] = [];

  reasons = [
    { value: StockAdjustmentReasons.Opening, label: 'AdjustmentReasonOpening' },
    { value: StockAdjustmentReasons.Damage, label: 'AdjustmentReasonDamage' },
    { value: StockAdjustmentReasons.Loss, label: 'AdjustmentReasonLoss' },
    { value: StockAdjustmentReasons.Recount, label: 'AdjustmentReasonRecount' },
    { value: StockAdjustmentReasons.Other, label: 'AdjustmentReasonOther' }
  ];

  @Output() onSave = new EventEmitter<any>();

  constructor(
    injector: Injector,
    public _adjustmentService: StockAdjustmentServiceProxy,
    private _productService: ProductServiceProxy,
    public bsModalRef: BsModalRef,
    private cd: ChangeDetectorRef
  ) {
    super(injector);
  }

  ngOnInit(): void {
    this.adjustment.adjustmentDate = this.toDateInputValue() as any;
    this.adjustment.reason = StockAdjustmentReasons.Other;
    this.adjustment.lines = [];
    this.addLine();

    this._productService.getAll(undefined, undefined, undefined, undefined, 0, 1000).subscribe((result) => {
      this.products = result.items || [];
      this.cd.detectChanges();
    });
  }

  addLine(): void {
    const line = new CreateStockAdjustmentLineDto();
    line.quantityChange = 1;
    this.adjustment.lines.push(line);
  }

  removeLine(index: number): void {
    if (this.adjustment.lines.length > 1) {
      this.adjustment.lines.splice(index, 1);
    }
  }

  save(): void {
    this.saving = true;
    this._adjustmentService.create(this.adjustment).subscribe(
      () => {
        this.notify.info(this.l('SavedSuccessfully'));
        this.bsModalRef.hide();
        this.onSave.emit();
      },
      () => {
        this.saving = false;
        this.cd.detectChanges();
      }
    );
  }
}
