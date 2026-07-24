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
  CreateCategoryDto,
  CategoryServiceProxy
} from '@shared/service-proxies/service-proxies';

@Component({
  templateUrl: 'create-category-dialog.component.html'
})
export class CreateCategoryDialogComponent extends AppComponentBase
  implements OnInit {
  saving = false;
  category: CreateCategoryDto = new CreateCategoryDto();

  @Output() onSave = new EventEmitter<any>();

  constructor(
    injector: Injector,
    public _categoryService: CategoryServiceProxy,
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

    this._categoryService.create(this.category).subscribe(
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
