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
  UnitServiceProxy,
  UnitDto
} from '@shared/service-proxies/service-proxies';

@Component({
  templateUrl: 'edit-unit-dialog.component.html'
})
export class EditUnitDialogComponent extends AppComponentBase
  implements OnInit {
  saving = false;
  unit: UnitDto = new UnitDto();
  id: number;

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
    this._unitService.get(this.id).subscribe((result: UnitDto) => {
      this.unit = result;
      this.cd.detectChanges();
    });
  }

  save(): void {
    this.saving = true;

    this._unitService.update(this.unit).subscribe(
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
