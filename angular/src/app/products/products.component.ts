import {
  ChangeDetectorRef,
  Component,
  Injector,
  ViewChild,
} from '@angular/core';
import { finalize } from 'rxjs/operators';
import { BsModalService, BsModalRef, ModalDirective } from 'ngx-bootstrap/modal';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import {
  PagedListingComponentBase,
  PagedRequestDto,
} from '@shared/paged-listing-component-base';
import {
  ProductServiceProxy,
  ProductDto,
  ProductDtoPagedResultDto,
} from '@shared/service-proxies/service-proxies';
import { AppConsts } from '@shared/AppConsts';
import { CreateProductDialogComponent } from './create-product/create-product-dialog.component';
import { EditProductDialogComponent } from './edit-product/edit-product-dialog.component';

class PagedProductsRequestDto extends PagedRequestDto {
  keyword: string;
}

@Component({
  templateUrl: './products.component.html',
  styleUrls: ['./products.component.less'],
  animations: [appModuleAnimation()]
})
export class ProductsComponent extends PagedListingComponentBase<ProductDto> {
  @ViewChild('imageModal', { static: true }) imageModal: ModalDirective;

  products: ProductDto[] = [];
  keyword = '';
  selectedImageUrl = '';
  selectedImageName = '';

  constructor(
    injector: Injector,
    private _productService: ProductServiceProxy,
    private _modalService: BsModalService,
    cd: ChangeDetectorRef
  ) {
    super(injector, cd);
  }

  list(
    request: PagedProductsRequestDto,
    pageNumber: number,
    finishedCallback: Function
  ): void {
    request.keyword = this.keyword;

    this._productService
      .getAll(
        request.keyword,
        undefined,
        undefined,
        undefined,
        request.skipCount,
        request.maxResultCount
      )
      .pipe(
        finalize(() => {
          finishedCallback();
        })
      )
      .subscribe((result: ProductDtoPagedResultDto) => {
        this.products = result.items;
        this.showPaging(result, pageNumber);
      });
  }

  getImageUrl(product: ProductDto): string {
    if (!product.imagePath) {
      return '';
    }
    return AppConsts.remoteServiceBaseUrl + product.imagePath;
  }

  viewImage(product: ProductDto): void {
    if (!product?.imagePath) {
      return;
    }

    this.selectedImageUrl = this.getImageUrl(product);
    this.selectedImageName = product.name;
    this.imageModal.show();
  }

  closeImage(): void {
    this.imageModal.hide();
    this.selectedImageUrl = '';
    this.selectedImageName = '';
  }

  delete(product: ProductDto): void {
    abp.message.confirm(
      this.l('ProductDeleteWarningMessage', product.name),
      undefined,
      (result: boolean) => {
        if (result) {
          this._productService
            .delete(product.id)
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

  createProduct(): void {
    this.showCreateOrEditProductDialog();
  }

  editProduct(product: ProductDto): void {
    this.showCreateOrEditProductDialog(product.id);
  }

  showCreateOrEditProductDialog(id?: number): void {
    let createOrEditProductDialog: BsModalRef;
    if (!id) {
      createOrEditProductDialog = this._modalService.show(
        CreateProductDialogComponent,
        {
          class: 'modal-lg',
        }
      );
    } else {
      createOrEditProductDialog = this._modalService.show(
        EditProductDialogComponent,
        {
          class: 'modal-lg',
          initialState: {
            id: id,
          },
        }
      );
    }

    createOrEditProductDialog.content.onSave.subscribe(() => {
      this.refresh();
    });
  }
}
