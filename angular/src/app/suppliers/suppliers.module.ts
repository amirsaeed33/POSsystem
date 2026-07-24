import { NgModule } from '@angular/core';
import { SharedModule } from '@shared/shared.module';
import { SuppliersRoutingModule } from './suppliers-routing.module';
import { CreateSupplierDialogComponent } from './create-supplier/create-supplier-dialog.component';
import { EditSupplierDialogComponent } from './edit-supplier/edit-supplier-dialog.component';
import { SuppliersComponent } from './suppliers.component';

@NgModule({
    declarations: [
        CreateSupplierDialogComponent,
        EditSupplierDialogComponent,
        SuppliersComponent,
    ],
    imports: [SharedModule, SuppliersRoutingModule],
})
export class SuppliersModule {}
