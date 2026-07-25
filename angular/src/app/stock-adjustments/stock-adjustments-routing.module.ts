import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { StockAdjustmentsComponent } from './stock-adjustments.component';

const routes: Routes = [
    {
        path: '',
        component: StockAdjustmentsComponent,
        pathMatch: 'full',
    },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class StockAdjustmentsRoutingModule {}
