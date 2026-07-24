import { Component, Injector, OnInit, Output, EventEmitter, ChangeDetectorRef } from '@angular/core';
import { BsModalRef } from 'ngx-bootstrap/modal';
import { AppComponentBase } from '@shared/app-component-base';
import { CreateSupplierDto, SupplierServiceProxy } from '@shared/service-proxies/service-proxies';

@Component({ templateUrl: 'create-supplier-dialog.component.html' })
export class CreateSupplierDialogComponent extends AppComponentBase implements OnInit {
  saving = false;
  item: CreateSupplierDto = new CreateSupplierDto();
  @Output() onSave = new EventEmitter<any>();

  constructor(injector: Injector, public _service: SupplierServiceProxy, public bsModalRef: BsModalRef, private cd: ChangeDetectorRef) {
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
