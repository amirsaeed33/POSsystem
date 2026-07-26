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
  SaleServiceProxy,
  SaleDto
} from '@shared/service-proxies/service-proxies';
import {
  CompanyProfileDto,
  CompanyProfileServiceProxy
} from '@shared/service-proxies/company-profile-service-proxy';

@Component({
  templateUrl: 'print-sale-invoice-dialog.component.html',
  styleUrls: ['print-sale-invoice-dialog.component.css']
})
export class PrintSaleInvoiceDialogComponent extends AppComponentBase
  implements OnInit {
  id: number;
  autoPrint = false;
  sale: SaleDto = new SaleDto();
  company: CompanyProfileDto | null = null;
  loading = true;
  today = new Date().toLocaleString();

  constructor(
    injector: Injector,
    public _saleService: SaleServiceProxy,
    private _companyProfileService: CompanyProfileServiceProxy,
    public bsModalRef: BsModalRef,
    private cd: ChangeDetectorRef
  ) {
    super(injector);
  }

  ngOnInit(): void {
    forkJoin({
      sale: this._saleService.get(this.id),
      company: this._companyProfileService.getCurrent().pipe(catchError(() => of(null)))
    }).subscribe(({ sale, company }) => {
      this.sale = sale;
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
