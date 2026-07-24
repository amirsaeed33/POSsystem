import { Component, Injector, OnInit, Output, EventEmitter, ChangeDetectorRef } from '@angular/core';
import { BsModalRef } from 'ngx-bootstrap/modal';
import { AppComponentBase } from '@shared/app-component-base';
import { CustomerServiceProxy, CustomerDto } from '@shared/service-proxies/service-proxies';

@Component({ templateUrl: 'edit-customer-dialog.component.html' })
export class EditCustomerDialogComponent extends AppComponentBase implements OnInit {
  saving = false;
  item: CustomerDto = new CustomerDto();
  id: number;
  @Output() onSave = new EventEmitter<any>();

  constructor(injector: Injector, public _service: CustomerServiceProxy, public bsModalRef: BsModalRef, private cd: ChangeDetectorRef) {
    super(injector);
  }

  ngOnInit(): void {
    this._service.get(this.id).subscribe((result: CustomerDto) => {
      this.item = result;
      this.cd.detectChanges();
    });
  }

  save(): void {
    this.saving = true;
    this._service.update(this.item).subscribe(() => {
      this.notify.info(this.l('SavedSuccessfully'));
      this.bsModalRef.hide();
      this.onSave.emit();
    }, () => { this.saving = false; });
  }
}
