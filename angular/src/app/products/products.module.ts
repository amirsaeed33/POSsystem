import { NgModule } from '@angular/core';
import { SharedModule } from '@shared/shared.module';
import { ProductsRoutingModule } from './products-routing.module';
import { CreateProductDialogComponent } from './create-product/create-product-dialog.component';
import { EditProductDialogComponent } from './edit-product/edit-product-dialog.component';
import { ProductsComponent } from './products.component';

@NgModule({
    declarations: [
        CreateProductDialogComponent,
        EditProductDialogComponent,
        ProductsComponent,
    ],
    imports: [SharedModule, ProductsRoutingModule],
})
export class ProductsModule {}
