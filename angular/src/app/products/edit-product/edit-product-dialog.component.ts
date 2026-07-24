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
import { AppConsts } from '@shared/AppConsts';
import {
  ProductServiceProxy,
  ProductDto,
  CategoryServiceProxy,
  BrandServiceProxy,
  UnitServiceProxy,
  CategoryDto,
  BrandDto,
  UnitDto
} from '@shared/service-proxies/service-proxies';

@Component({
  templateUrl: 'edit-product-dialog.component.html'
})
export class EditProductDialogComponent extends AppComponentBase
  implements OnInit {
  saving = false;
  product: ProductDto = new ProductDto();
  categories: CategoryDto[] = [];
  brands: BrandDto[] = [];
  units: UnitDto[] = [];
  imagePreview: string | ArrayBuffer = '';
  id: number;

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
    forkJoin([
      this._productService.get(this.id),
      this._categoryService.getAll('', 0, 1000),
      this._brandService.getAll('', 0, 1000),
      this._unitService.getAll('', 0, 1000)
    ]).subscribe(([product, categories, brands, units]) => {
      this.product = product;
      this.product.imageBase64 = undefined;
      this.categories = categories.items || [];
      this.brands = brands.items || [];
      this.units = units.items || [];
      if (product.imagePath) {
        this.imagePreview = AppConsts.remoteServiceBaseUrl + product.imagePath;
      }
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
      const result = reader.result as string;
      if (!result || !result.startsWith('data:image')) {
        return;
      }
      this.imagePreview = result;
      this.product.imageBase64 = result;
      this.cd.detectChanges();
    };
    reader.readAsDataURL(file);
  }

  save(): void {
    this.saving = true;

    const updateInput = this.product.clone();
    if (!updateInput.imageBase64 || !updateInput.imageBase64.startsWith('data:image')) {
      updateInput.imageBase64 = undefined;
    }

    this._productService.update(updateInput).subscribe(
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
