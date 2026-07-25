import { ChangeDetectorRef, Component, Injector, OnInit } from '@angular/core';
import { BsModalRef } from 'ngx-bootstrap/modal';
import { AppComponentBase } from '@shared/app-component-base';
import {
  StockAdjustmentDto,
  StockAdjustmentServiceProxy
} from '@shared/service-proxies/stock-adjustment-service-proxy';

@Component({
  templateUrl: 'view-stock-adjustment-dialog.component.html'
})
export class ViewStockAdjustmentDialogComponent extends AppComponentBase implements OnInit {
  id: number;
  loading = true;
  adjustment: StockAdjustmentDto = new StockAdjustmentDto();

  constructor(
    injector: Injector,
    private _adjustmentService: StockAdjustmentServiceProxy,
    public bsModalRef: BsModalRef,
    private cd: ChangeDetectorRef
  ) {
    super(injector);
  }

  ngOnInit(): void {
    this._adjustmentService.get(this.id).subscribe((result) => {
      this.adjustment = result;
      this.loading = false;
      this.cd.detectChanges();
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
}
