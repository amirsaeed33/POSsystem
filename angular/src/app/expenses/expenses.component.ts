import { ChangeDetectorRef, Component, Injector } from '@angular/core';
import { finalize } from 'rxjs/operators';
import { BsModalService, BsModalRef } from 'ngx-bootstrap/modal';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import {
  PagedListingComponentBase,
  PagedRequestDto,
} from '@shared/paged-listing-component-base';
import {
  ExpenseServiceProxy,
  ExpenseDto,
  ExpenseDtoPagedResultDto,
} from '@shared/service-proxies/service-proxies';
import { CreateExpenseDialogComponent } from './create-expense/create-expense-dialog.component';
import { PrintExpenseInvoiceDialogComponent } from './print-expense-invoice/print-expense-invoice-dialog.component';

class PagedExpensesRequestDto extends PagedRequestDto {
  keyword: string;
}

@Component({
  templateUrl: './expenses.component.html',
  animations: [appModuleAnimation()]
})
export class ExpensesComponent extends PagedListingComponentBase<ExpenseDto> {
  expenses: ExpenseDto[] = [];
  keyword = '';

  constructor(
    injector: Injector,
    private _expenseService: ExpenseServiceProxy,
    private _modalService: BsModalService,
    cd: ChangeDetectorRef
  ) {
    super(injector, cd);
  }

  list(
    request: PagedExpensesRequestDto,
    pageNumber: number,
    finishedCallback: Function
  ): void {
    request.keyword = this.keyword;

    this._expenseService
      .getAll(request.keyword, undefined, request.skipCount, request.maxResultCount)
      .pipe(finalize(() => finishedCallback()))
      .subscribe((result: ExpenseDtoPagedResultDto) => {
        this.expenses = result.items;
        this.showPaging(result, pageNumber);
      });
  }

  delete(expense: ExpenseDto): void {
    abp.message.confirm(
      this.l('ExpenseDeleteWarningMessage', expense.referenceNo || ('#' + expense.id)),
      undefined,
      (result: boolean) => {
        if (result) {
          this._expenseService
            .delete(expense.id)
            .pipe(
              finalize(() => {
                abp.notify.success(this.l('SuccessfullyDeleted'));
                this.refresh();
              })
            )
            .subscribe(() => {});
        }
      }
    );
  }

  createExpense(): void {
    const dialog: BsModalRef = this._modalService.show(CreateExpenseDialogComponent, {
      class: 'modal-lg',
    });
    dialog.content.onSave.subscribe(() => this.refresh());
  }

  printInvoice(expense: ExpenseDto): void {
    this._modalService.show(PrintExpenseInvoiceDialogComponent, {
      class: 'modal-lg',
      initialState: { id: expense.id },
    });
  }
}
