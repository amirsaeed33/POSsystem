import { ChangeDetectorRef, Component, Injector } from '@angular/core';
import { finalize } from 'rxjs/operators';
import { BsModalService, BsModalRef } from 'ngx-bootstrap/modal';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import {
  PagedListingComponentBase,
  PagedRequestDto,
} from '@shared/paged-listing-component-base';
import {
  CategoryServiceProxy,
  CategoryDto,
  CategoryDtoPagedResultDto,
} from '@shared/service-proxies/service-proxies';
import { CreateCategoryDialogComponent } from './create-category/create-category-dialog.component';
import { EditCategoryDialogComponent } from './edit-category/edit-category-dialog.component';

class PagedCategoriesRequestDto extends PagedRequestDto {
  keyword: string;
}

@Component({
  templateUrl: './categories.component.html',
  animations: [appModuleAnimation()]
})
export class CategoriesComponent extends PagedListingComponentBase<CategoryDto> {
  categories: CategoryDto[] = [];
  keyword = '';

  constructor(
    injector: Injector,
    private _categoryService: CategoryServiceProxy,
    private _modalService: BsModalService,
    cd: ChangeDetectorRef
  ) {
    super(injector, cd);
  }

  list(
    request: PagedCategoriesRequestDto,
    pageNumber: number,
    finishedCallback: Function
  ): void {
    request.keyword = this.keyword;

    this._categoryService
      .getAll(
        request.keyword,
        request.skipCount,
        request.maxResultCount
      )
      .pipe(
        finalize(() => {
          finishedCallback();
        })
      )
      .subscribe((result: CategoryDtoPagedResultDto) => {
        this.categories = result.items;
        this.showPaging(result, pageNumber);
      });
  }

  delete(category: CategoryDto): void {
    abp.message.confirm(
      this.l('CategoryDeleteWarningMessage', category.name),
      undefined,
      (result: boolean) => {
        if (result) {
          this._categoryService
            .delete(category.id)
            .pipe(
              finalize(() => {
                abp.notify.success(this.l('SuccessfullyDeleted'));
                this.refresh();
              })
            )
            .subscribe(() => {});
        }
      }
    );
  }

  createCategory(): void {
    this.showCreateOrEditCategoryDialog();
  }

  editCategory(category: CategoryDto): void {
    this.showCreateOrEditCategoryDialog(category.id);
  }

  showCreateOrEditCategoryDialog(id?: number): void {
    let createOrEditCategoryDialog: BsModalRef;
    if (!id) {
      createOrEditCategoryDialog = this._modalService.show(
        CreateCategoryDialogComponent,
        {
          class: 'modal-lg',
        }
      );
    } else {
      createOrEditCategoryDialog = this._modalService.show(
        EditCategoryDialogComponent,
        {
          class: 'modal-lg',
          initialState: {
            id: id,
          },
        }
      );
    }

    createOrEditCategoryDialog.content.onSave.subscribe(() => {
      this.refresh();
    });
  }
}
