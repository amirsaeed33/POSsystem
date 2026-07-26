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
  ExpenseServiceProxy,
  ExpenseDto
} from '@shared/service-proxies/service-proxies';
import {
  CompanyProfileDto,
  CompanyProfileServiceProxy
} from '@shared/service-proxies/company-profile-service-proxy';

@Component({
  templateUrl: 'print-expense-invoice-dialog.component.html',
  styleUrls: ['print-expense-invoice-dialog.component.css']
})
export class PrintExpenseInvoiceDialogComponent extends AppComponentBase
  implements OnInit {
  id: number;
  expense: ExpenseDto = new ExpenseDto();
  company: CompanyProfileDto | null = null;
  loading = true;
  today = new Date().toLocaleString();

  constructor(
    injector: Injector,
    public _expenseService: ExpenseServiceProxy,
    private _companyProfileService: CompanyProfileServiceProxy,
    public bsModalRef: BsModalRef,
    private cd: ChangeDetectorRef
  ) {
    super(injector);
  }

  ngOnInit(): void {
    forkJoin({
      expense: this._expenseService.get(this.id),
      company: this._companyProfileService.getCurrent().pipe(catchError(() => of(null)))
    }).subscribe(({ expense, company }) => {
      this.expense = expense;
      this.company = company;
      this.loading = false;
      this.cd.detectChanges();
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
