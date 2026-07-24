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
import {
  CreateExpenseDto,
  ExpenseServiceProxy,
  BusinessAccountServiceProxy,
  BusinessAccountDto
} from '@shared/service-proxies/service-proxies';

@Component({
  templateUrl: 'create-expense-dialog.component.html'
})
export class CreateExpenseDialogComponent extends AppComponentBase
  implements OnInit {
  saving = false;
  expense: CreateExpenseDto = new CreateExpenseDto();
  paymentAccounts: BusinessAccountDto[] = [];

  @Output() onSave = new EventEmitter<any>();

  constructor(
    injector: Injector,
    public _expenseService: ExpenseServiceProxy,
    private _accountService: BusinessAccountServiceProxy,
    public bsModalRef: BsModalRef,
    private cd: ChangeDetectorRef
  ) {
    super(injector);
  }

  ngOnInit(): void {
    this.expense.expenseDate = this.toDateInputValue() as any;
    this.expense.amount = 0;

    // Ensures Cash/Bank system accounts exist, then load payment sources
    this._accountService.getAll(undefined, 0, 1000).subscribe((result) => {
      const excludedTypes = ['Purchase', 'Sale', 'Expense', 'Customer', 'Supplier'];
      const items = result.items || [];
      this.paymentAccounts = items.filter(
        (a) => a.isActive !== false && excludedTypes.indexOf(a.accountType) < 0
      );

      if (!this.paymentAccounts.length) {
        this.paymentAccounts = items.filter((a) => a.isActive !== false);
      }

      if (this.paymentAccounts.length === 1) {
        this.expense.paymentAccountId = this.paymentAccounts[0].id;
      }

      this.cd.detectChanges();
    });
  }

  save(): void {
    this.saving = true;

    this._expenseService.create(this.expense).subscribe(
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
