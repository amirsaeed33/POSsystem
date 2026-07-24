import { NgModule } from '@angular/core';
import { SharedModule } from '@shared/shared.module';
import { CustomersRoutingModule } from './customers-routing.module';
import { CreateCustomerDialogComponent } from './create-customer/create-customer-dialog.component';
import { EditCustomerDialogComponent } from './edit-customer/edit-customer-dialog.component';
import { CustomersComponent } from './customers.component';

@NgModule({
    declarations: [
        CreateCustomerDialogComponent,
        EditCustomerDialogComponent,
        CustomersComponent,
    ],
    imports: [SharedModule, CustomersRoutingModule],
})
export class CustomersModule {}
