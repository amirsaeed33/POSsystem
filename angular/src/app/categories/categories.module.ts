import { NgModule } from '@angular/core';
import { SharedModule } from '@shared/shared.module';
import { CategoriesRoutingModule } from './categories-routing.module';
import { CreateCategoryDialogComponent } from './create-category/create-category-dialog.component';
import { EditCategoryDialogComponent } from './edit-category/edit-category-dialog.component';
import { CategoriesComponent } from './categories.component';

@NgModule({
    declarations: [
        CreateCategoryDialogComponent,
        EditCategoryDialogComponent,
        CategoriesComponent,
    ],
    imports: [SharedModule, CategoriesRoutingModule],
})
export class CategoriesModule {}
