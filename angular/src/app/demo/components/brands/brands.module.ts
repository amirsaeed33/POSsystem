import { NgModule } from '@angular/core';
import { BrandsRoutingModule } from './brands-routing.module';
import { BrandsSharedModule } from './brands-shared.module';

@NgModule({
    imports: [BrandsSharedModule, BrandsRoutingModule],
})
export class BrandsModule {}
