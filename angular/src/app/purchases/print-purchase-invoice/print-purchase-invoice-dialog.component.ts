import {
  Component,
  Injector,
  OnInit,
  ChangeDetectorRef
} from '@angular/core';
import { BsModalRef } from 'ngx-bootstrap/modal';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { AppComponentBase } from '@shared/app-component-base';
import { AppConsts } from '@shared/AppConsts';
import {
  PurchaseServiceProxy,
  PurchaseDto
} from '@shared/service-proxies/service-proxies';
import {
  CompanyProfileDto,
  CompanyProfileServiceProxy
} from '@shared/service-proxies/company-profile-service-proxy';

@Component({
  templateUrl: 'print-purchase-invoice-dialog.component.html',
  styleUrls: ['print-purchase-invoice-dialog.component.css']
})
export class PrintPurchaseInvoiceDialogComponent extends AppComponentBase
  implements OnInit {
  id: number;
  autoPrint = false;
  purchase: PurchaseDto = new PurchaseDto();
  company: CompanyProfileDto | null = null;
  loading = true;
  today = new Date().toLocaleString();

  constructor(
    injector: Injector,
    public _purchaseService: PurchaseServiceProxy,
    private _companyProfileService: CompanyProfileServiceProxy,
    public bsModalRef: BsModalRef,
    private cd: ChangeDetectorRef
  ) {
    super(injector);
  }

  ngOnInit(): void {
    forkJoin({
      purchase: this._purchaseService.get(this.id),
      company: this._companyProfileService.getCurrent().pipe(catchError(() => of(null)))
    }).subscribe(({ purchase, company }) => {
      this.purchase = purchase;
      this.company = company;
      this.loading = false;
      this.cd.detectChanges();
      if (this.autoPrint) {
        this.print();
      }
    });
  }

  get companyLogoUrl(): string {
    if (!this.company?.imagePath) {
      return '';
    }
    return AppConsts.remoteServiceBaseUrl + this.company.imagePath;
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
