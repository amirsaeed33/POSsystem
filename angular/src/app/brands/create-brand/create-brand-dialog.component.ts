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
  CreateBrandDto,
  BrandServiceProxy
} from '@shared/service-proxies/service-proxies';

@Component({
  templateUrl: 'create-brand-dialog.component.html'
})
export class CreateBrandDialogComponent extends AppComponentBase
  implements OnInit {
  saving = false;
  brand: CreateBrandDto = new CreateBrandDto();

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
    this.cd.detectChanges();
  }

  save(): void {
    this.saving = true;

    this._brandService.create(this.brand).subscribe(
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
