import {
  Component,
  Injector,
  OnInit,
  ChangeDetectorRef
} from '@angular/core';
import { BsModalRef } from 'ngx-bootstrap/modal';
import { AppComponentBase } from '@shared/app-component-base';
import {
  ExpenseServiceProxy,
  ExpenseDto
} from '@shared/service-proxies/service-proxies';

@Component({
  templateUrl: 'print-expense-invoice-dialog.component.html',
  styleUrls: ['print-expense-invoice-dialog.component.css']
})
export class PrintExpenseInvoiceDialogComponent extends AppComponentBase
  implements OnInit {
  id: number;
  expense: ExpenseDto = new ExpenseDto();
  loading = true;
  today = new Date().toLocaleString();

  constructor(
    injector: Injector,
    public _expenseService: ExpenseServiceProxy,
    public bsModalRef: BsModalRef,
    private cd: ChangeDetectorRef
  ) {
    super(injector);
  }

  ngOnInit(): void {
    this._expenseService.get(this.id).subscribe((result) => {
      this.expense = result;
      this.loading = false;
      this.cd.detectChanges();
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
