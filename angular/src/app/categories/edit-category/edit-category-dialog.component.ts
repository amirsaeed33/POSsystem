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
  CategoryServiceProxy,
  CategoryDto
} from '@shared/service-proxies/service-proxies';

@Component({
  templateUrl: 'edit-category-dialog.component.html'
})
export class EditCategoryDialogComponent extends AppComponentBase
  implements OnInit {
  saving = false;
  category: CategoryDto = new CategoryDto();
  id: number;

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
    this._categoryService.get(this.id).subscribe((result: CategoryDto) => {
      this.category = result;
      this.cd.detectChanges();
    });
  }

  save(): void {
    this.saving = true;

    this._categoryService.update(this.category).subscribe(
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
