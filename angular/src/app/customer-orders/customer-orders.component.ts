import { ChangeDetectorRef, Component, Injector } from '@angular/core';
import { finalize } from 'rxjs/operators';
import { BsModalService, BsModalRef } from 'ngx-bootstrap/modal';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import {
  PagedListingComponentBase,
  PagedRequestDto,
} from '@shared/paged-listing-component-base';
import {
  CustomerOrderServiceProxy,
  CustomerOrderDto,
  CustomerOrderDtoPagedResultDto,
  CustomerOrderStatus,
} from '@shared/service-proxies/customer-order-service-proxy';
import { CreateCustomerOrderDialogComponent } from './create-customer-order/create-customer-order-dialog.component';
import { ViewCustomerOrderDialogComponent } from './view-customer-order/view-customer-order-dialog.component';

class PagedOrdersRequestDto extends PagedRequestDto {
  keyword: string;
  status: CustomerOrderStatus | undefined;
}

@Component({
  templateUrl: './customer-orders.component.html',
  animations: [appModuleAnimation()]
})
export class CustomerOrdersComponent extends PagedListingComponentBase<CustomerOrderDto> {
  orders: CustomerOrderDto[] = [];
  keyword = '';
  statusFilter: number | '' = '';
  statuses = CustomerOrderStatus;

  constructor(
    injector: Injector,
    private _orderService: CustomerOrderServiceProxy,
    private _modalService: BsModalService,
    cd: ChangeDetectorRef
  ) {
    super(injector, cd);
  }

  list(
    request: PagedOrdersRequestDto,
    pageNumber: number,
    finishedCallback: Function
  ): void {
    request.keyword = this.keyword;
    request.status = this.statusFilter === '' ? undefined : (this.statusFilter as CustomerOrderStatus);

    this._orderService
      .getAll(request.keyword, request.status, request.skipCount, request.maxResultCount)
      .pipe(finalize(() => finishedCallback()))
      .subscribe((result: CustomerOrderDtoPagedResultDto) => {
        this.orders = result.items;
        this.showPaging(result, pageNumber);
      });
  }

  createOrder(): void {
    const dialog: BsModalRef = this._modalService.show(CreateCustomerOrderDialogComponent, {
      class: 'modal-xl',
    });
    dialog.content.onSave.subscribe(() => this.refresh());
  }

  viewOrder(order: CustomerOrderDto): void {
    this._modalService.show(ViewCustomerOrderDialogComponent, {
      class: 'modal-lg',
      initialState: { id: order.id },
    });
  }

  approve(order: CustomerOrderDto): void {
    abp.message.confirm(
      this.l('OrderApproveConfirmMessage', order.orderNo || ('#' + order.id)),
      undefined,
      (result: boolean) => {
        if (!result) {
          return;
        }
        this._orderService.approve(order.id).subscribe((sale) => {
          abp.notify.success(
            this.l('OrderApprovedSuccessfully') +
              (sale?.invoiceNo ? ' (' + sale.invoiceNo + ')' : '')
          );
          this.refresh();
        });
      }
    );
  }

  reject(order: CustomerOrderDto): void {
    abp.message.confirm(
      this.l('OrderRejectConfirmMessage', order.orderNo || ('#' + order.id)),
      undefined,
      (result: boolean) => {
        if (!result) {
          return;
        }
        this._orderService.reject(order.id).subscribe(() => {
          abp.notify.success(this.l('OrderRejectedSuccessfully'));
          this.refresh();
        });
      }
    );
  }

  delete(order: CustomerOrderDto): void {
    abp.message.confirm(
      this.l('OrderDeleteWarningMessage', order.orderNo || ('#' + order.id)),
      undefined,
      (result: boolean) => {
        if (result) {
          this._orderService
            .delete(order.id)
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

  isPending(order: CustomerOrderDto): boolean {
    return order.status === CustomerOrderStatus.Pending;
  }

  statusBadgeClass(status: CustomerOrderStatus): string {
    if (status === CustomerOrderStatus.Approved) return 'badge-success';
    if (status === CustomerOrderStatus.Rejected) return 'badge-danger';
    return 'badge-warning';
  }

  statusLabel(status: CustomerOrderStatus): string {
    if (status === CustomerOrderStatus.Approved) return this.l('OrderApproved');
    if (status === CustomerOrderStatus.Rejected) return this.l('OrderRejected');
    return this.l('OrderPending');
  }
}
