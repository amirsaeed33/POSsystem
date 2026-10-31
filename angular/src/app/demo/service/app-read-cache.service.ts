import { Injectable } from '@angular/core';
import { BrandService } from './brand.service';
import { CategoryService } from './category.service';
import { CustomerService } from './customer.service';
import { DealService } from './deal.service';
import { DeliveryAreaService } from './delivery-area.service';
import { OnlineOrderHeroSlideService } from './online-order-hero-slide.service';
import { ProductModifierService } from './product-modifier.service';
import { SaleService } from './sale.service';
import { UnitService } from './unit.service';
import { CustomerOrderService } from './customer-order.service';
import { LookUpService } from './lookup.service';

/**
 * Clears branch-scoped client caches when the active branch changes.
 */
@Injectable({ providedIn: 'root' })
export class AppReadCacheService {
    constructor(
        private categoryService: CategoryService,
        private brandService: BrandService,
        private unitService: UnitService,
        private customerService: CustomerService,
        private saleService: SaleService,
        private dealService: DealService,
        private productModifierService: ProductModifierService,
        private deliveryAreaService: DeliveryAreaService,
        private heroSlideService: OnlineOrderHeroSlideService,
        private customerOrderService: CustomerOrderService,
        private lookUpService: LookUpService
    ) {}

    clearBranchScoped(): void {
        this.categoryService.clearCache();
        this.brandService.clearCache();
        this.unitService.clearCache();
        this.customerService.clearCache();
        this.saleService.clearPosCaches();
        this.dealService.clearCache();
        this.productModifierService.clearCache();
        this.deliveryAreaService.clearCache();
        this.heroSlideService.clearCache();
        this.customerOrderService.clearCache();
        this.lookUpService.clearCache();
    }
}
