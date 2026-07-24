import { Component, Injector, OnInit, Output, EventEmitter, ChangeDetectorRef } from '@angular/core';
import { BsModalRef } from 'ngx-bootstrap/modal';
import { AppComponentBase } from '@shared/app-component-base';
import { SupplierServiceProxy, SupplierDto } from '@shared/service-proxies/service-proxies';

@Component({ templateUrl: 'edit-supplier-dialog.component.html' })
export class EditSupplierDialogComponent extends AppComponentBase implements OnInit {
  saving = false;
  item: SupplierDto = new SupplierDto();
  id: number;
  @Output() onSave = new EventEmitter<any>();

  constructor(injector: Injector, public _service: SupplierServiceProxy, public bsModalRef: BsModalRef, private cd: ChangeDetectorRef) {
    super(injector);
  }

  ngOnInit(): void {
    this._service.get(this.id).subscribe((result: SupplierDto) => {
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
