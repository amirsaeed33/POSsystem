import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { PermissionNames } from 'src/app/demo/api/permission-names';
import { PermissionService } from 'src/app/demo/service/permission.service';

import { BranchContextService } from 'src/app/demo/service/branch-context.service';

type SettingsTab = 'brands' | 'units' | 'categories';

@Component({
    selector: 'app-product-settings',
    templateUrl: './product-settings.component.html',
})
export class ProductSettingsComponent implements OnInit {
    activeIndex = 0;
    canBrands = false;
    canUnits = false;
    canCategories = false;

    private readonly tabOrder: SettingsTab[] = [];

    get tabOrderHasTabs(): boolean {
        return this.tabOrder.length > 0;
    }

    constructor(
        private permissionService: PermissionService,
        private branchContext: BranchContextService,
        private route: ActivatedRoute,
        private router: Router
    ) {}

    async ngOnInit(): Promise<void> {
        try {
            await this.permissionService.ensureLoaded();
        } catch {
            // Use cached grants if available.
        }

        const isRestaurant = this.branchContext.isRestaurant();
        this.canBrands = !isRestaurant && this.permissionService.isGranted(PermissionNames.Brands);
        this.canUnits = this.permissionService.isGranted(PermissionNames.Units);
        this.canCategories = this.permissionService.isGranted(
            PermissionNames.Categories
        );

        if (this.canBrands) {
            this.tabOrder.push('brands');
        }
        if (this.canUnits) {
            this.tabOrder.push('units');
        }
        if (this.canCategories) {
            this.tabOrder.push('categories');
        }

        let requested =
            (this.route.snapshot.queryParamMap.get('tab') as SettingsTab) ||
            'brands';
        if (requested === 'brands' && !this.canBrands) {
            requested = this.tabOrder[0] || 'units';
        }
        this.activeIndex = Math.max(0, this.tabOrder.indexOf(requested));
    }

    onTabChange(index: number): void {
        this.activeIndex = index;
        const tab = this.tabOrder[index];
        if (!tab) {
            return;
        }
        this.router.navigate([], {
            relativeTo: this.route,
            queryParams: { tab },
            queryParamsHandling: 'merge',
            replaceUrl: true,
        });
    }
}
