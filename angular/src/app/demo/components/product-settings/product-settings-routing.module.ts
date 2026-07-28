import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';
import { ProductSettingsComponent } from './product-settings.component';

@NgModule({
    imports: [
        RouterModule.forChild([
            {
                path: '',
                component: ProductSettingsComponent,
                data: { breadcrumb: 'Settings' },
            },
        ]),
    ],
    exports: [RouterModule],
})
export class ProductSettingsRoutingModule {}
