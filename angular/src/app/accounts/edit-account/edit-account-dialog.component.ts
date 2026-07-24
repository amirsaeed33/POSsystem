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
  BusinessAccountServiceProxy,
  BusinessAccountDto
} from '@shared/service-proxies/service-proxies';

@Component({
  templateUrl: 'edit-account-dialog.component.html'
})
export class EditAccountDialogComponent extends AppComponentBase
  implements OnInit {
  saving = false;
  account: BusinessAccountDto = new BusinessAccountDto();
  id: number;
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
    this._accountService.get(this.id).subscribe((result: BusinessAccountDto) => {
      this.account = result;
      this.cd.detectChanges();
    });
  }

  save(): void {
    this.saving = true;

    this._accountService.update(this.account).subscribe(
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
