import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';
import { CategoryListComponent } from './category-list.component';

@NgModule({
    imports: [
        RouterModule.forChild([
            {
                path: '',
                component: CategoryListComponent,
                data: { breadcrumb: 'Categories' },
            },
        ]),
    ],
    exports: [RouterModule],
})
export class CategoriesRoutingModule {}
