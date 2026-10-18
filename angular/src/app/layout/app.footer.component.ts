import { Component, OnDestroy, OnInit } from '@angular/core';
import { LayoutService } from "./service/app.layout.service";
import { BranchContextService } from '../demo/service/branch-context.service';
import { BranchService } from '../demo/service/branch.service';
import { BranchDto } from '../demo/api/branch';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-footer',
  templateUrl: './app.footer.component.html'
})
export class AppFooterComponent implements OnInit, OnDestroy {
    branchName = 'SmartPos';
    branchLogo: string | null = null;
    branchLogoFailed = false;
    private branchSubscription?: Subscription;

    constructor(
        public layoutService: LayoutService,
        private branchContext: BranchContextService,
        private branchService: BranchService
    ) {}

    get colorScheme(): string {
        return this.layoutService.config().colorScheme;
    }

    get logo(): string {
        if (this.branchLogo && !this.branchLogoFailed) {
            return this.branchLogo;
        }
        return 'assets/layout/images/logo-' + (this.colorScheme === 'light' ? 'dark' : 'light') + '.png';
    }

    onLogoError(): void {
        this.branchLogoFailed = true;
    }

    ngOnInit(): void {
        const branch = this.branchContext.getCurrentBranch();
        this.updateBranchInfo(branch);
        this.branchSubscription = this.branchContext.currentBranch$.subscribe((b) => {
            this.updateBranchInfo(b);
        });
    }

    private updateBranchInfo(branch: BranchDto | null): void {
        if (branch?.name) {
            this.branchName = branch.name;
        }
        if (branch?.imagePath) {
            this.branchLogo = this.branchService.getImageUrl(branch.imagePath);
            this.branchLogoFailed = false;
        } else {
            this.branchLogo = null;
        }
    }

    ngOnDestroy(): void {
        if (this.branchSubscription) {
            this.branchSubscription.unsubscribe();
        }
    }
}
