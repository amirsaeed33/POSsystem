import { NgModule } from '@angular/core';
import { CategoriesRoutingModule } from './categories-routing.module';
import { CategoriesSharedModule } from './categories-shared.module';

@NgModule({
    imports: [CategoriesSharedModule, CategoriesRoutingModule],
})
export class CategoriesModule {}
