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
  CreateUnitDto,
  UnitServiceProxy
} from '@shared/service-proxies/service-proxies';

@Component({
  templateUrl: 'create-unit-dialog.component.html'
})
export class CreateUnitDialogComponent extends AppComponentBase
  implements OnInit {
  saving = false;
  unit: CreateUnitDto = new CreateUnitDto();

  @Output() onSave = new EventEmitter<any>();

  constructor(
    injector: Injector,
    public _unitService: UnitServiceProxy,
    public bsModalRef: BsModalRef,
    private cd: ChangeDetectorRef
  ) {
    super(injector);
  }

  ngOnInit(): void {
    this.cd.detectChanges();
  }

  save(): void {
    this.saving = true;

    this._unitService.create(this.unit).subscribe(
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
