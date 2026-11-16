import { inject, NgModule } from '@angular/core';
import { CanActivateFn, Router, RouterModule } from '@angular/router';
import { OnlineOrderComponent } from './online-order.component';
import { OnlineProductDetailComponent } from './online-product-detail.component';
import { OnlineOrderHistoryComponent } from './online-order-history.component';

/** Old /track/:orderNo bookmarks → My Orders detail. */
const redirectTrackToHistory: CanActivateFn = (route) => {
    const router = inject(Router);
    const orderNo = (route.paramMap.get('orderNo') || '').trim();
    const branchId = route.queryParamMap.get('branchId');
    return router.createUrlTree(['/online-order/history'], {
        queryParams: {
            ...(orderNo ? { orderNo } : {}),
            ...(branchId ? { branchId } : {}),
        },
    });
};

@NgModule({
    imports: [
        RouterModule.forChild([
            { path: '', component: OnlineOrderComponent },
            { path: 'product', component: OnlineOrderComponent },
            { path: 'product/:id', component: OnlineProductDetailComponent },
            { path: 'track', redirectTo: 'history', pathMatch: 'full' },
            {
                path: 'track/:orderNo',
                canActivate: [redirectTrackToHistory],
                // Guard always redirects; component is only a config requirement.
                component: OnlineOrderHistoryComponent,
            },
            { path: 'history', component: OnlineOrderHistoryComponent },
        ]),
    ],
    exports: [RouterModule],
})
export class OnlineOrderRoutingModule {}
