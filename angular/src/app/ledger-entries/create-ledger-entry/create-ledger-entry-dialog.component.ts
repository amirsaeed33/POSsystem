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
  CreateLedgerEntryDto,
  LedgerEntryServiceProxy,
  BusinessAccountServiceProxy,
  BusinessAccountDto
} from '@shared/service-proxies/service-proxies';

@Component({
  templateUrl: 'create-ledger-entry-dialog.component.html'
})
export class CreateLedgerEntryDialogComponent extends AppComponentBase
  implements OnInit {
  saving = false;
  entry: CreateLedgerEntryDto = new CreateLedgerEntryDto();
  accounts: BusinessAccountDto[] = [];
  accountId: number | undefined;
  voucherTypes = [
    { value: 'Invoice', label: 'Invoice' },
    { value: 'Payment', label: 'Payment' },
    { value: 'Journal', label: 'Journal' },
    { value: 'OpeningBalance', label: 'Opening Balance' },
    { value: 'Adjustment', label: 'Adjustment' }
  ];

  @Output() onSave = new EventEmitter<any>();

  constructor(
    injector: Injector,
    public _ledgerService: LedgerEntryServiceProxy,
    public _accountService: BusinessAccountServiceProxy,
    public bsModalRef: BsModalRef,
    private cd: ChangeDetectorRef
  ) {
    super(injector);
  }

  ngOnInit(): void {
    this.entry.transactionDate = this.toDateInputValue() as any;
    this.entry.debit = 0;
    this.entry.credit = 0;
    if (this.accountId) {
      this.entry.accountId = this.accountId;
    }

    this._accountService.getAll(undefined, undefined, 1000).subscribe((result) => {
      this.accounts = result.items;
      this.cd.detectChanges();
    });
  }

  save(): void {
    this.saving = true;

    this._ledgerService.create(this.entry).subscribe(
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
