import { Component, OnDestroy, OnInit } from '@angular/core';
import { PrimeNGConfig } from 'primeng/api';
import { Subscription } from 'rxjs';
import { AppBrowserTitleService } from './demo/service/app-browser-title.service';
import { BranchContextService } from './demo/service/branch-context.service';
import { BranchService } from './demo/service/branch.service';

@Component({
    selector: 'app-root',
    templateUrl: './app.component.html',
    styleUrls: ['./app.component.scss']
})
export class AppComponent implements OnInit, OnDestroy {
    private branchSub?: Subscription;

    constructor(
        private primengConfig: PrimeNGConfig,
        private branchContext: BranchContextService,
        private branchService: BranchService,
        private browserTitle: AppBrowserTitleService
    ) { }

    ngOnInit(): void {
        this.primengConfig.ripple = true;
        this.primengConfig.zIndex = {
            modal: 1200,
            overlay: 1000,
            menu: 1000,
            tooltip: 1100
        };

        this.browserTitle.apply();
        this.branchSub = this.branchContext.currentBranch$.subscribe((branch) => {
            this.browserTitle.setStaffBranchName(branch?.name);
            const logoUrl = branch?.imagePath
                ? this.branchService.getImageUrl(branch.imagePath)
                : '';
            this.browserTitle.setStaffBranchLogoUrl(logoUrl || null);
        });
    }

    ngOnDestroy(): void {
        this.branchSub?.unsubscribe();
    }
}
