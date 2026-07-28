import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TabViewModule } from 'primeng/tabview';
import { BrandsSharedModule } from '../brands/brands-shared.module';
import { CategoriesSharedModule } from '../categories/categories-shared.module';
import { UnitsSharedModule } from '../units/units-shared.module';
import { ProductSettingsRoutingModule } from './product-settings-routing.module';
import { ProductSettingsComponent } from './product-settings.component';

@NgModule({
    imports: [
        CommonModule,
        TabViewModule,
        BrandsSharedModule,
        UnitsSharedModule,
        CategoriesSharedModule,
        ProductSettingsRoutingModule,
    ],
    declarations: [ProductSettingsComponent],
})
export class ProductSettingsModule {}
