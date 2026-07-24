import { Component, Injector, OnInit, ChangeDetectorRef } from '@angular/core';
import { BsModalRef } from 'ngx-bootstrap/modal';
import { AppComponentBase } from '@shared/app-component-base';
import {
  CustomerOrderServiceProxy,
  CustomerOrderDto,
  CustomerOrderStatus
} from '@shared/service-proxies/customer-order-service-proxy';

@Component({
  templateUrl: 'view-customer-order-dialog.component.html'
})
export class ViewCustomerOrderDialogComponent extends AppComponentBase implements OnInit {
  id: number;
  order: CustomerOrderDto = new CustomerOrderDto();
  loading = true;

  constructor(
    injector: Injector,
    public _orderService: CustomerOrderServiceProxy,
    public bsModalRef: BsModalRef,
    private cd: ChangeDetectorRef
  ) {
    super(injector);
  }

  ngOnInit(): void {
    this._orderService.get(this.id).subscribe((result) => {
      this.order = result;
      this.loading = false;
      this.cd.detectChanges();
    });
  }

  statusLabel(status: CustomerOrderStatus): string {
    if (status === CustomerOrderStatus.Approved) return this.l('OrderApproved');
    if (status === CustomerOrderStatus.Rejected) return this.l('OrderRejected');
    return this.l('OrderPending');
  }
}
