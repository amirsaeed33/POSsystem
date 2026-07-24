import { ChangeDetectorRef, Component, Injector } from '@angular/core';
import { finalize } from 'rxjs/operators';
import { BsModalService, BsModalRef } from 'ngx-bootstrap/modal';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { PagedListingComponentBase, PagedRequestDto } from '@shared/paged-listing-component-base';
import { SupplierServiceProxy, SupplierDto, SupplierDtoPagedResultDto } from '@shared/service-proxies/service-proxies';
import { CreateSupplierDialogComponent } from './create-supplier/create-supplier-dialog.component';
import { EditSupplierDialogComponent } from './edit-supplier/edit-supplier-dialog.component';

class PagedSuppliersRequestDto extends PagedRequestDto { keyword: string; }

@Component({
  templateUrl: './suppliers.component.html',
  animations: [appModuleAnimation()]
})
export class SuppliersComponent extends PagedListingComponentBase<SupplierDto> {
  items: SupplierDto[] = [];
  keyword = '';

  constructor(
    injector: Injector,
    private _service: SupplierServiceProxy,
    private _modalService: BsModalService,
    cd: ChangeDetectorRef
  ) { super(injector, cd); }

  list(request: PagedSuppliersRequestDto, pageNumber: number, finishedCallback: Function): void {
    request.keyword = this.keyword;
    this._service.getAll(request.keyword, request.skipCount, request.maxResultCount)
      .pipe(finalize(() => finishedCallback()))
      .subscribe((result: SupplierDtoPagedResultDto) => {
        this.items = result.items;
        this.showPaging(result, pageNumber);
      });
  }

  delete(item: SupplierDto): void {
    abp.message.confirm(this.l('SupplierDeleteWarningMessage', item.name), undefined, (result: boolean) => {
      if (result) {
        this._service.delete(item.id).pipe(finalize(() => {
          abp.notify.success(this.l('SuccessfullyDeleted'));
          this.refresh();
        })).subscribe(() => {});
      }
    });
  }

  create(): void { this.showCreateOrEditDialog(); }
  edit(item: SupplierDto): void { this.showCreateOrEditDialog(item.id); }

  showCreateOrEditDialog(id?: number): void {
    let dialog: BsModalRef;
    if (!id) {
      dialog = this._modalService.show(CreateSupplierDialogComponent, { class: 'modal-lg' });
    } else {
      dialog = this._modalService.show(EditSupplierDialogComponent, { class: 'modal-lg', initialState: { id } });
    }
    dialog.content.onSave.subscribe(() => this.refresh());
  }
}
