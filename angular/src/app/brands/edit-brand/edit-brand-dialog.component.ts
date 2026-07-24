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
  BrandServiceProxy,
  BrandDto
} from '@shared/service-proxies/service-proxies';

@Component({
  templateUrl: 'edit-brand-dialog.component.html'
})
export class EditBrandDialogComponent extends AppComponentBase
  implements OnInit {
  saving = false;
  brand: BrandDto = new BrandDto();
  id: number;

  @Output() onSave = new EventEmitter<any>();

  constructor(
    injector: Injector,
    public _brandService: BrandServiceProxy,
    public bsModalRef: BsModalRef,
    private cd: ChangeDetectorRef
  ) {
    super(injector);
  }

  ngOnInit(): void {
    this._brandService.get(this.id).subscribe((result: BrandDto) => {
      this.brand = result;
      this.cd.detectChanges();
    });
  }

  save(): void {
    this.saving = true;

    this._brandService.update(this.brand).subscribe(
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
