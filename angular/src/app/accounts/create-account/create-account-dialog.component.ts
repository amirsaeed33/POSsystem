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
  CreateBusinessAccountDto,
  BusinessAccountServiceProxy
} from '@shared/service-proxies/service-proxies';

@Component({
  templateUrl: 'create-account-dialog.component.html'
})
export class CreateAccountDialogComponent extends AppComponentBase
  implements OnInit {
  saving = false;
  account: CreateBusinessAccountDto = new CreateBusinessAccountDto();
  accountTypes = ['Cash', 'Bank', 'Credit Card', 'Purchase', 'Sale', 'Expense', 'Other'];

  @Output() onSave = new EventEmitter<any>();

  constructor(
    injector: Injector,
    public _accountService: BusinessAccountServiceProxy,
    public bsModalRef: BsModalRef,
    private cd: ChangeDetectorRef
  ) {
    super(injector);
  }

  ngOnInit(): void {
    this.account.isActive = true;
    this.account.openingBalance = 0;
    this.cd.detectChanges();
  }

  save(): void {
    this.saving = true;

    this._accountService.create(this.account).subscribe(
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
