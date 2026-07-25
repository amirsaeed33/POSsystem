import {
  ChangeDetectorRef,
  Component,
  ElementRef,
  Injector,
  OnInit,
  ViewChild
} from '@angular/core';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/app-component-base';
import {
  CreateSaleDto,
  CreateSaleLineDto,
  CustomerDto,
  CustomerServiceProxy,
  ProductDto,
  SaleServiceProxy
} from '@shared/service-proxies/service-proxies';

export interface PosCartLine {
  productId: number;
  productName: string;
  quantity: number;
  unitPrice: number;
  stockQuantity: number;
}

@Component({
  templateUrl: './pos.component.html',
  styleUrls: ['./pos.component.less'],
  animations: [appModuleAnimation()]
})
export class PosComponent extends AppComponentBase implements OnInit {
  @ViewChild('barcodeInput') barcodeInput: ElementRef<HTMLInputElement>;

  saving = false;
  scanning = false;
  barcode = '';
  customers: CustomerDto[] = [];
  cart: PosCartLine[] = [];

  customerId: number;
  notes = '';
  discountAmount = 0;
  discountPercent = 0;
  taxPercent = 0;
  paymentType = 0; // Cash
  cashAmount = 0;
  cardAmount = 0;

  paymentTypes = [
    { value: 0, label: 'PaymentTypeCash' },
    { value: 1, label: 'PaymentTypeCard' },
    { value: 2, label: 'PaymentTypeCredit' },
    { value: 3, label: 'PaymentTypeMixed' }
  ];

  constructor(
    injector: Injector,
    private _saleService: SaleServiceProxy,
    private _customerService: CustomerServiceProxy,
    private cd: ChangeDetectorRef
  ) {
    super(injector);
  }

  ngOnInit(): void {
    this._customerService.getAll(undefined, 0, 1000).subscribe((result) => {
      this.customers = result.items || [];
      const walkIn = this.customers.find((c) =>
        (c.name || '').toLowerCase().includes('walk')
      );
      this.customerId = walkIn?.id || this.customers[0]?.id;
      this.cd.detectChanges();
      this.focusBarcode();
    });
  }

  focusBarcode(): void {
    setTimeout(() => this.barcodeInput?.nativeElement?.focus(), 50);
  }

  get subTotal(): number {
    return this.cart.reduce((sum, line) => sum + line.quantity * line.unitPrice, 0);
  }

  get computedDiscount(): number {
    let discount = this.discountAmount || 0;
    if ((this.discountPercent || 0) > 0 && discount <= 0) {
      discount = Math.round((this.subTotal * this.discountPercent) / 100 * 100) / 100;
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
    return Math.round((taxable * (this.taxPercent || 0)) / 100 * 100) / 100;
  }

  get grandTotal(): number {
    return Math.round((this.subTotal - this.computedDiscount + this.taxAmount) * 100) / 100;
  }

  get creditAmount(): number {
    if (this.paymentType === 2) {
      return this.grandTotal;
    }
    if (this.paymentType === 3) {
      return Math.max(0, Math.round((this.grandTotal - (this.cashAmount || 0) - (this.cardAmount || 0)) * 100) / 100);
    }
    return 0;
  }

  onPaymentTypeChange(): void {
    if (this.paymentType === 0 || this.paymentType === 1 || this.paymentType === 2) {
      this.cashAmount = 0;
      this.cardAmount = 0;
    } else if (this.paymentType === 3) {
      this.cashAmount = this.grandTotal;
      this.cardAmount = 0;
    }
    this.focusBarcode();
  }

  onBarcodeEnter(): void {
    const code = (this.barcode || '').trim();
    if (!code || this.scanning || this.saving) {
      return;
    }

    this.scanning = true;
    this._saleService.getProductByBarcode(code).subscribe(
      (product) => {
        this.addProduct(product);
        this.barcode = '';
        this.scanning = false;
        this.cd.detectChanges();
        this.focusBarcode();
      },
      () => {
        this.scanning = false;
        this.barcode = '';
        this.cd.detectChanges();
        this.focusBarcode();
      }
    );
  }

  addProduct(product: ProductDto): void {
    const existing = this.cart.find((x) => x.productId === product.id);
    if (existing) {
      if (existing.quantity + 1 > (product.stockQuantity || 0)) {
        this.notify.warn(this.l('InsufficientStock') || 'Insufficient stock');
        return;
      }
      existing.quantity += 1;
      return;
    }

    if ((product.stockQuantity || 0) < 1) {
      this.notify.warn(this.l('OutOfStock'));
      return;
    }

    this.cart.push({
      productId: product.id,
      productName: product.name,
      quantity: 1,
      unitPrice: this.getUnitPrice(product),
      stockQuantity: product.stockQuantity || 0
    });
  }

  getUnitPrice(product: ProductDto): number {
    const customer = this.customers.find((c) => c.id === this.customerId);
    const isWholesaler = customer?.customerType === 1;
    if (isWholesaler) {
      return product.wholesalePrice > 0 ? product.wholesalePrice : product.price || 0;
    }
    return product.price || 0;
  }

  onCustomerSelected(): void {
    // Re-price cart when customer type changes would need product reload; keep current prices.
    this.focusBarcode();
  }

  updateQty(line: PosCartLine, qty: number): void {
    if (qty <= 0) {
      this.removeLine(line);
      return;
    }
    if (qty > line.stockQuantity) {
      this.notify.warn(this.l('InsufficientStock') || 'Insufficient stock');
      line.quantity = line.stockQuantity;
      return;
    }
    line.quantity = qty;
  }

  removeLine(line: PosCartLine): void {
    this.cart = this.cart.filter((x) => x !== line);
    this.focusBarcode();
  }

  clearCart(): void {
    this.cart = [];
    this.discountAmount = 0;
    this.discountPercent = 0;
    this.taxPercent = 0;
    this.notes = '';
    this.paymentType = 0;
    this.cashAmount = 0;
    this.cardAmount = 0;
    this.focusBarcode();
  }

  canSave(): boolean {
    return !!this.customerId && this.cart.length > 0 && !this.saving;
  }

  buildSaleDto(): CreateSaleDto {
    const sale = new CreateSaleDto();
    sale.customerId = this.customerId;
    sale.saleDate = this.toDateInputValue() as any;
    sale.notes = this.notes;
    sale.discountAmount = this.discountAmount || 0;
    sale.discountPercent = this.discountPercent || 0;
    sale.taxPercent = this.taxPercent || 0;
    sale.paymentType = this.paymentType;
    sale.cashAmount = this.paymentType === 3 ? this.cashAmount || 0 : 0;
    sale.cardAmount = this.paymentType === 3 ? this.cardAmount || 0 : 0;
    sale.lines = this.cart.map((line) => {
      const dto = new CreateSaleLineDto();
      dto.productId = line.productId;
      dto.quantity = line.quantity;
      dto.unitPrice = line.unitPrice;
      return dto;
    });
    return sale;
  }

  completeSale(): void {
    if (!this.canSave()) {
      return;
    }

    if (this.paymentType === 3) {
      const paid = (this.cashAmount || 0) + (this.cardAmount || 0);
      if (paid > this.grandTotal + 0.001) {
        this.notify.warn('Cash + card cannot exceed total');
        return;
      }
    }

    this.saving = true;
    this._saleService.create(this.buildSaleDto()).subscribe(
      () => {
        this.notify.info(this.l('SavedSuccessfully'));
        this.clearCart();
        this.saving = false;
        this.cd.detectChanges();
        this.focusBarcode();
      },
      () => {
        this.saving = false;
        this.cd.detectChanges();
        this.focusBarcode();
      }
    );
  }
}
