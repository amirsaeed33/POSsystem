import {
  Component,
  Injector,
  OnInit,
  ChangeDetectorRef
} from '@angular/core';
import { BsModalRef } from 'ngx-bootstrap/modal';
import { AppComponentBase } from '@shared/app-component-base';
import {
  PurchaseServiceProxy,
  PurchaseDto
} from '@shared/service-proxies/service-proxies';

@Component({
  templateUrl: 'print-purchase-invoice-dialog.component.html',
  styleUrls: ['print-purchase-invoice-dialog.component.css']
})
export class PrintPurchaseInvoiceDialogComponent extends AppComponentBase
  implements OnInit {
  id: number;
  autoPrint = false;
  purchase: PurchaseDto = new PurchaseDto();
  loading = true;
  today = new Date().toLocaleString();

  constructor(
    injector: Injector,
    public _purchaseService: PurchaseServiceProxy,
    public bsModalRef: BsModalRef,
    private cd: ChangeDetectorRef
  ) {
    super(injector);
  }

  ngOnInit(): void {
    this._purchaseService.get(this.id).subscribe((result) => {
      this.purchase = result;
      this.loading = false;
      this.cd.detectChanges();
      if (this.autoPrint) {
        this.print();
      }
    });
  }

  print(): void {
    setTimeout(() => {
      document.body.classList.add('printing-invoice');
      const cleanup = () => {
        document.body.classList.remove('printing-invoice');
        window.removeEventListener('afterprint', cleanup);
      };
      window.addEventListener('afterprint', cleanup);
      window.print();
      setTimeout(cleanup, 1000);
    }, 100);
  }
}
