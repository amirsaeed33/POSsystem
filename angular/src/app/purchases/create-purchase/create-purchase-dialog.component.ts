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
  CreatePurchaseDto,
  CreatePurchaseLineDto,
  PurchaseServiceProxy,
  ProductServiceProxy,
  SupplierServiceProxy,
  ProductDto,
  SupplierDto
} from '@shared/service-proxies/service-proxies';
import { PrintPurchaseInvoiceDialogComponent } from '../print-purchase-invoice/print-purchase-invoice-dialog.component';

@Component({
  templateUrl: 'create-purchase-dialog.component.html'
})
export class CreatePurchaseDialogComponent extends AppComponentBase
  implements OnInit {
  saving = false;
  purchase: CreatePurchaseDto = new CreatePurchaseDto();
  products: ProductDto[] = [];
  suppliers: SupplierDto[] = [];

  @ViewChild('createForm') createForm: NgForm;
  @ViewChildren('lineProductSelect') lineProductSelects: QueryList<NgSelectComponent>;

  @Output() onSave = new EventEmitter<any>();

  constructor(
    injector: Injector,
    public _purchaseService: PurchaseServiceProxy,
    private _productService: ProductServiceProxy,
    private _supplierService: SupplierServiceProxy,
    private _modalService: BsModalService,
    public bsModalRef: BsModalRef,
    private cd: ChangeDetectorRef
  ) {
    super(injector);
  }

  ngOnInit(): void {
    this.purchase.purchaseDate = this.toDateInputValue() as any;
    this.purchase.lines = [];
    this.addLine();

    forkJoin([
      this._productService.getAll(undefined, undefined, undefined, undefined, 0, 1000),
      this._supplierService.getAll(undefined, 0, 1000)
    ]).subscribe(([products, suppliers]) => {
      this.products = products.items || [];
      this.suppliers = suppliers.items || [];
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
    const line = new CreatePurchaseLineDto();
    line.quantity = 1;
    line.unitCost = 0;
    this.purchase.lines.push(line);
  }

  removeLine(index: number): void {
    if (this.purchase.lines.length > 1) {
      this.purchase.lines.splice(index, 1);
    }
  }

  onProductSelected(line: CreatePurchaseLineDto): void {
    const product = this.products.find((p) => p.id === line.productId);
    if (product) {
      line.unitCost = product.price || 0;
    }
  }

  onUnitCostTab(event: KeyboardEvent, index: number): void {
    if (event.shiftKey) {
      return;
    }

    const line = this.purchase.lines[index];
    if (!line?.productId) {
      return;
    }

    if (index !== this.purchase.lines.length - 1) {
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

  lineTotal(line: CreatePurchaseLineDto): number {
    return (line.quantity || 0) * (line.unitCost || 0);
  }

  get grandTotal(): number {
    return (this.purchase.lines || []).reduce((sum, line) => sum + this.lineTotal(line), 0);
  }

  isFormValid(): boolean {
    return !!this.createForm?.form?.valid;
  }

  save(): void {
    if (!this.isFormValid() || this.saving) {
      return;
    }

    this.saving = true;
    this._purchaseService.create(this.purchase).subscribe(
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
    this._purchaseService.create(this.purchase).subscribe(
      (result) => {
        this.notify.info(this.l('SavedSuccessfully'));
        this.onSave.emit();
        this.bsModalRef.hide();
        this._modalService.show(PrintPurchaseInvoiceDialogComponent, {
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
