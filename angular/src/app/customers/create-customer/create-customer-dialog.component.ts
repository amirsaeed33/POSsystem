import { Component, Injector, OnInit, Output, EventEmitter, ChangeDetectorRef } from '@angular/core';
import { BsModalRef } from 'ngx-bootstrap/modal';
import { AppComponentBase } from '@shared/app-component-base';
import { CreateCustomerDto, CustomerServiceProxy } from '@shared/service-proxies/service-proxies';

@Component({ templateUrl: 'create-customer-dialog.component.html' })
export class CreateCustomerDialogComponent extends AppComponentBase implements OnInit {
  saving = false;
  item: CreateCustomerDto = new CreateCustomerDto();
  @Output() onSave = new EventEmitter<any>();

  constructor(injector: Injector, public _service: CustomerServiceProxy, public bsModalRef: BsModalRef, private cd: ChangeDetectorRef) {
    super(injector);
  }

  ngOnInit(): void { this.cd.detectChanges(); }

  save(): void {
    this.saving = true;
    this._service.create(this.item).subscribe(() => {
      this.notify.info(this.l('SavedSuccessfully'));
      this.bsModalRef.hide();
      this.onSave.emit();
    }, () => { this.saving = false; });
  }
}
