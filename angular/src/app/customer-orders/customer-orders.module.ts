import { NgModule } from '@angular/core';
import { SharedModule } from '@shared/shared.module';
import { CustomerOrdersRoutingModule } from './customer-orders-routing.module';
import { CustomerOrdersComponent } from './customer-orders.component';
import { CreateCustomerOrderDialogComponent } from './create-customer-order/create-customer-order-dialog.component';
import { ViewCustomerOrderDialogComponent } from './view-customer-order/view-customer-order-dialog.component';

@NgModule({
  declarations: [
    CustomerOrdersComponent,
    CreateCustomerOrderDialogComponent,
    ViewCustomerOrderDialogComponent,
  ],
  imports: [SharedModule, CustomerOrdersRoutingModule],
})
export class CustomerOrdersModule {}
