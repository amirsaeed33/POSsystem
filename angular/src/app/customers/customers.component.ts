import { ChangeDetectorRef, Component, Injector } from '@angular/core';
import { finalize } from 'rxjs/operators';
import { BsModalService, BsModalRef } from 'ngx-bootstrap/modal';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { PagedListingComponentBase, PagedRequestDto } from '@shared/paged-listing-component-base';
import { CustomerServiceProxy, CustomerDto, CustomerDtoPagedResultDto } from '@shared/service-proxies/service-proxies';
import { CreateCustomerDialogComponent } from './create-customer/create-customer-dialog.component';
import { EditCustomerDialogComponent } from './edit-customer/edit-customer-dialog.component';

class PagedCustomersRequestDto extends PagedRequestDto { keyword: string; }

@Component({
  templateUrl: './customers.component.html',
  animations: [appModuleAnimation()]
})
export class CustomersComponent extends PagedListingComponentBase<CustomerDto> {
  items: CustomerDto[] = [];
  keyword = '';

  constructor(
    injector: Injector,
    private _service: CustomerServiceProxy,
    private _modalService: BsModalService,
    cd: ChangeDetectorRef
  ) { super(injector, cd); }

  list(request: PagedCustomersRequestDto, pageNumber: number, finishedCallback: Function): void {
    request.keyword = this.keyword;
    this._service.getAll(request.keyword, request.skipCount, request.maxResultCount)
      .pipe(finalize(() => finishedCallback()))
      .subscribe((result: CustomerDtoPagedResultDto) => {
        this.items = result.items;
        this.showPaging(result, pageNumber);
      });
  }

  delete(item: CustomerDto): void {
    abp.message.confirm(this.l('CustomerDeleteWarningMessage', item.name), undefined, (result: boolean) => {
      if (result) {
        this._service.delete(item.id).pipe(finalize(() => {
          abp.notify.success(this.l('SuccessfullyDeleted'));
          this.refresh();
        })).subscribe(() => {});
      }
    });
  }

  create(): void { this.showCreateOrEditDialog(); }
  edit(item: CustomerDto): void { this.showCreateOrEditDialog(item.id); }

  showCreateOrEditDialog(id?: number): void {
    let dialog: BsModalRef;
    if (!id) {
      dialog = this._modalService.show(CreateCustomerDialogComponent, { class: 'modal-lg' });
    } else {
      dialog = this._modalService.show(EditCustomerDialogComponent, { class: 'modal-lg', initialState: { id } });
    }
    dialog.content.onSave.subscribe(() => this.refresh());
  }
}
