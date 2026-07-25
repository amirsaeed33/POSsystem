import {
  Component,
  Injector,
  OnInit,
  Output,
  EventEmitter,
  ChangeDetectorRef
} from '@angular/core';
import { BsModalRef } from 'ngx-bootstrap/modal';
import { forkJoin } from 'rxjs';
import { AppComponentBase } from '@shared/app-component-base';
import {
  CreateProductDto,
  ProductServiceProxy,
  CategoryServiceProxy,
  BrandServiceProxy,
  UnitServiceProxy,
  CategoryDto,
  BrandDto,
  UnitDto
} from '@shared/service-proxies/service-proxies';

@Component({
  templateUrl: 'create-product-dialog.component.html'
})
export class CreateProductDialogComponent extends AppComponentBase
  implements OnInit {
  saving = false;
  product: CreateProductDto = new CreateProductDto();
  categories: CategoryDto[] = [];
  brands: BrandDto[] = [];
  units: UnitDto[] = [];
  imagePreview: string | ArrayBuffer = '';

  @Output() onSave = new EventEmitter<any>();

  constructor(
    injector: Injector,
    public _productService: ProductServiceProxy,
    private _categoryService: CategoryServiceProxy,
    private _brandService: BrandServiceProxy,
    private _unitService: UnitServiceProxy,
    public bsModalRef: BsModalRef,
    private cd: ChangeDetectorRef
  ) {
    super(injector);
  }

  ngOnInit(): void {
    this.product.price = 0;
    this.product.wholesalePrice = 0;
    this.product.costPrice = 0;
    this.product.alertQuantityLimit = 10;
    forkJoin([
      this._categoryService.getAll('', 0, 1000),
      this._brandService.getAll('', 0, 1000),
      this._unitService.getAll('', 0, 1000)
    ]).subscribe(([categories, brands, units]) => {
      this.categories = categories.items || [];
      this.brands = brands.items || [];
      this.units = units.items || [];
      this.cd.detectChanges();
    });
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!input.files || input.files.length === 0) {
      return;
    }

    const file = input.files[0];
    const reader = new FileReader();
    reader.onload = () => {
      this.imagePreview = reader.result;
      this.product.imageBase64 = reader.result as string;
      this.cd.detectChanges();
    };
    reader.readAsDataURL(file);
  }

  save(): void {
    this.saving = true;

    this._productService.create(this.product).subscribe(
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
