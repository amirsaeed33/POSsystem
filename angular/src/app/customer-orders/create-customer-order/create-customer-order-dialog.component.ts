import {
  Component,
  Injector,
  OnInit,
  Output,
  EventEmitter,
  ChangeDetectorRef,
  ViewChildren,
  QueryList,
  ViewChild
} from '@angular/core';
import { NgForm } from '@angular/forms';
import { BsModalRef } from 'ngx-bootstrap/modal';
import { NgSelectComponent } from '@ng-select/ng-select';
import { forkJoin } from 'rxjs';
import { AppComponentBase } from '@shared/app-component-base';
import {
  CreateCustomerOrderDto,
  CreateCustomerOrderLineDto,
  CustomerOrderServiceProxy,
} from '@shared/service-proxies/customer-order-service-proxy';
import {
  ProductServiceProxy,
  CustomerServiceProxy,
  ProductDto,
  CustomerDto
} from '@shared/service-proxies/service-proxies';

@Component({
  templateUrl: 'create-customer-order-dialog.component.html'
})
export class CreateCustomerOrderDialogComponent extends AppComponentBase implements OnInit {
  saving = false;
  order: CreateCustomerOrderDto = new CreateCustomerOrderDto();
  products: ProductDto[] = [];
  customers: CustomerDto[] = [];

  @ViewChild('createForm') createForm: NgForm;
  @ViewChildren('lineProductSelect') lineProductSelects: QueryList<NgSelectComponent>;
  @Output() onSave = new EventEmitter<any>();

  constructor(
    injector: Injector,
    public _orderService: CustomerOrderServiceProxy,
    private _productService: ProductServiceProxy,
    private _customerService: CustomerServiceProxy,
    public bsModalRef: BsModalRef,
    private cd: ChangeDetectorRef
  ) {
    super(injector);
  }

  ngOnInit(): void {
    this.order.orderDate = this.toDateInputValue() as any;
    this.order.lines = [];
    this.addLine();

    forkJoin([
      this._productService.getAll(undefined, undefined, undefined, undefined, 0, 1000),
      this._customerService.getAll(undefined, 0, 1000)
    ]).subscribe(([products, customers]) => {
      this.products = products.items || [];
      this.customers = customers.items || [];
      this.cd.detectChanges();
    });
  }

  addLine(): void {
    const line = new CreateCustomerOrderLineDto();
    line.quantity = 1;
    line.unitPrice = 0;
    this.order.lines.push(line);
  }

  removeLine(index: number): void {
    if (this.order.lines.length > 1) {
      this.order.lines.splice(index, 1);
    }
  }

  onCustomerSelected(): void {
    (this.order.lines || []).forEach((line) => {
      if (line.productId) {
        this.onProductSelected(line);
      }
    });
    this.cd.detectChanges();
  }

  onProductSelected(line: CreateCustomerOrderLineDto): void {
    const product = this.products.find((p) => p.id === line.productId);
    if (product) {
      line.unitPrice = this.getUnitPriceForCustomer(product);
    }
  }

  getUnitPriceForCustomer(product: ProductDto): number {
    const customer = this.customers.find((c) => c.id === this.order.customerId);
    const isWholesaler = customer?.customerType === 1;
    if (isWholesaler) {
      return product.wholesalePrice > 0 ? product.wholesalePrice : product.price || 0;
    }
    return product.price || 0;
  }

  onUnitPriceTab(event: KeyboardEvent, index: number): void {
    if (event.shiftKey) {
      return;
    }
    const line = this.order.lines[index];
    if (!line?.productId || index !== this.order.lines.length - 1) {
      return;
    }
    event.preventDefault();
    this.addLine();
    this.cd.detectChanges();
    setTimeout(() => {
      const selects = this.lineProductSelects?.toArray() || [];
      selects[index + 1]?.focus();
    });
  }

  lineTotal(line: CreateCustomerOrderLineDto): number {
    return (line.quantity || 0) * (line.unitPrice || 0);
  }

  get grandTotal(): number {
    return (this.order.lines || []).reduce((sum, line) => sum + this.lineTotal(line), 0);
  }

  save(): void {
    if (!this.createForm?.form?.valid || this.saving) {
      return;
    }
    this.saving = true;
    this._orderService.create(this.order).subscribe(
      () => {
        this.notify.info(this.l('SavedSuccessfully'));
        this.bsModalRef.hide();
        this.onSave.emit();
      },
      () => {
        this.saving = false;
        this.cd.detectChanges();
      }
    );
  }
}
