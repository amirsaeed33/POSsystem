import { NgModule } from '@angular/core';
import { SharedModule } from '@shared/shared.module';
import { BrandsRoutingModule } from './brands-routing.module';
import { CreateBrandDialogComponent } from './create-brand/create-brand-dialog.component';
import { EditBrandDialogComponent } from './edit-brand/edit-brand-dialog.component';
import { BrandsComponent } from './brands.component';

@NgModule({
    declarations: [
        CreateBrandDialogComponent,
        EditBrandDialogComponent,
        BrandsComponent,
    ],
    imports: [SharedModule, BrandsRoutingModule],
})
export class BrandsModule {}
