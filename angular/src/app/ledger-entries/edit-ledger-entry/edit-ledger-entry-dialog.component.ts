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
  LedgerEntryServiceProxy,
  LedgerEntryDto,
  BusinessAccountServiceProxy,
  BusinessAccountDto
} from '@shared/service-proxies/service-proxies';

@Component({
  templateUrl: 'edit-ledger-entry-dialog.component.html'
})
export class EditLedgerEntryDialogComponent extends AppComponentBase
  implements OnInit {
  saving = false;
  entry: LedgerEntryDto = new LedgerEntryDto();
  accounts: BusinessAccountDto[] = [];
  id: number;
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
    this._accountService.getAll(undefined, undefined, 1000).subscribe((result) => {
      this.accounts = result.items;
      this.cd.detectChanges();
    });

    this._ledgerService.get(this.id).subscribe((result: LedgerEntryDto) => {
      this.entry = result;
      this.entry.transactionDate = this.toDateInputValue(result.transactionDate) as any;
      this.cd.detectChanges();
    });
  }

  save(): void {
    this.saving = true;

    this._ledgerService.update(this.entry).subscribe(
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
