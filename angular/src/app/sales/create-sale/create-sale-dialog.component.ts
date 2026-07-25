import {
  Component,
  Injector,
  OnInit,
  Output,
  EventEmitter,
  ChangeDetectorRef,
  HostListener,
  ViewChildren,
  QueryList,
  ViewChild
} from '@angular/core';
import { NgForm } from '@angular/forms';
import { BsModalRef, BsModalService } from 'ngx-bootstrap/modal';
import { NgSelectComponent } from '@ng-select/ng-select';
import { forkJoin } from 'rxjs';
import { AppComponentBase } from '@shared/app-component-base';
import {
  CreateSaleDto,
  CreateSaleLineDto,
  SaleServiceProxy,
  ProductServiceProxy,
  CustomerServiceProxy,
  ProductDto,
  CustomerDto
} from '@shared/service-proxies/service-proxies';
import { PrintSaleInvoiceDialogComponent } from '../print-sale-invoice/print-sale-invoice-dialog.component';

@Component({
  templateUrl: 'create-sale-dialog.component.html'
})
export class CreateSaleDialogComponent extends AppComponentBase
  implements OnInit {
  saving = false;
  sale: CreateSaleDto = new CreateSaleDto();
  products: ProductDto[] = [];
  customers: CustomerDto[] = [];

  @ViewChild('createForm') createForm: NgForm;
  @ViewChildren('lineProductSelect') lineProductSelects: QueryList<NgSelectComponent>;

  @Output() onSave = new EventEmitter<any>();

  constructor(
    injector: Injector,
    public _saleService: SaleServiceProxy,
    private _productService: ProductServiceProxy,
    private _customerService: CustomerServiceProxy,
    private _modalService: BsModalService,
    public bsModalRef: BsModalRef,
    private cd: ChangeDetectorRef
  ) {
    super(injector);
  }

  paymentTypes = [
    { value: 0, label: 'PaymentTypeCash' },
    { value: 1, label: 'PaymentTypeCard' },
    { value: 2, label: 'PaymentTypeCredit' },
    { value: 3, label: 'PaymentTypeMixed' }
  ];

  ngOnInit(): void {
    this.sale.saleDate = this.toDateInputValue() as any;
    this.sale.paymentType = 2;
    this.sale.discountAmount = 0;
    this.sale.discountPercent = 0;
    this.sale.taxPercent = 0;
    this.sale.cashAmount = 0;
    this.sale.cardAmount = 0;
    this.sale.lines = [];
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

  @HostListener('document:keydown', ['$event'])
  handleKeyboard(event: KeyboardEvent): void {
    if (!(event.ctrlKey || event.metaKey) || this.saving) {
      return;
    }

    const key = (event.key || '').toLowerCase();
    if (key === 's') {
      event.preventDefault();
      if (this.isFormValid()) {
        this.save();
      }
    } else if (key === 'p') {
      event.preventDefault();
      if (this.isFormValid()) {
        this.saveAndPrint();
      }
    }
  }

  addLine(): void {
    const line = new CreateSaleLineDto();
    line.quantity = 1;
    line.unitPrice = 0;
    this.sale.lines.push(line);
  }

  removeLine(index: number): void {
    if (this.sale.lines.length > 1) {
      this.sale.lines.splice(index, 1);
    }
  }

  onCustomerSelected(): void {
    (this.sale.lines || []).forEach((line) => {
      if (line.productId) {
        this.onProductSelected(line);
      }
    });
    this.cd.detectChanges();
  }

  onProductSelected(line: CreateSaleLineDto): void {
    const product = this.products.find((p) => p.id === line.productId);
    if (product) {
      line.unitPrice = this.getUnitPriceForCustomer(product);
    }
  }

  getUnitPriceForCustomer(product: ProductDto): number {
    const customer = this.customers.find((c) => c.id === this.sale.customerId);
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

    const line = this.sale.lines[index];
    if (!line?.productId) {
      return;
    }

    if (index !== this.sale.lines.length - 1) {
      return;
    }

    event.preventDefault();
    this.addLine();
    this.cd.detectChanges();
    setTimeout(() => {
      const selects = this.lineProductSelects?.toArray() || [];
      const next = selects[index + 1];
      if (next) {
        next.focus();
      }
    });
  }

  lineTotal(line: CreateSaleLineDto): number {
    return (line.quantity || 0) * (line.unitPrice || 0);
  }

  get subTotal(): number {
    return (this.sale.lines || []).reduce((sum, line) => sum + this.lineTotal(line), 0);
  }

  get computedDiscount(): number {
    let discount = this.sale.discountAmount || 0;
    if ((this.sale.discountPercent || 0) > 0 && discount <= 0) {
      discount = Math.round((this.subTotal * this.sale.discountPercent) / 100 * 100) / 100;
    }
    if (discount < 0) {
      discount = 0;
    }
    if (discount > this.subTotal) {
      discount = this.subTotal;
    }
    return discount;
  }

  get taxAmount(): number {
    const taxable = this.subTotal - this.computedDiscount;
    return Math.round((taxable * (this.sale.taxPercent || 0)) / 100 * 100) / 100;
  }

  get grandTotal(): number {
    return Math.round((this.subTotal - this.computedDiscount + this.taxAmount) * 100) / 100;
  }

  onPaymentTypeChange(): void {
    if (this.sale.paymentType !== 3) {
      this.sale.cashAmount = 0;
      this.sale.cardAmount = 0;
    }
  }

  isFormValid(): boolean {
    return !!this.createForm?.form?.valid;
  }

  save(): void {
    if (!this.isFormValid() || this.saving) {
      return;
    }

    this.saving = true;
    this._saleService.create(this.sale).subscribe(
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

  saveAndPrint(): void {
    if (!this.isFormValid() || this.saving) {
      return;
    }

    this.saving = true;
    this._saleService.create(this.sale).subscribe(
      (result) => {
        this.notify.info(this.l('SavedSuccessfully'));
        this.onSave.emit();
        this.bsModalRef.hide();
        this._modalService.show(PrintSaleInvoiceDialogComponent, {
          class: 'modal-lg',
          initialState: { id: result.id, autoPrint: true }
        });
      },
      () => {
        this.saving = false;
        this.cd.detectChanges();
      }
    );
  }
}
