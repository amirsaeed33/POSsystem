import { Component, OnDestroy, OnInit } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { PrimeNGConfig } from 'primeng/api';
import { Subscription } from 'rxjs';
import { BranchContextService } from './demo/service/branch-context.service';

@Component({
    selector: 'app-root',
    templateUrl: './app.component.html',
    styleUrls: ['./app.component.scss']
})
export class AppComponent implements OnInit, OnDestroy {
    private static readonly APP_TITLE = 'Al Jannat';
    private branchSub?: Subscription;

    constructor(
        private primengConfig: PrimeNGConfig,
        private title: Title,
        private branchContext: BranchContextService
    ) { }

    ngOnInit(): void {
        this.primengConfig.ripple = true;
        this.primengConfig.zIndex = {
            modal: 1200,
            overlay: 1000,
            menu: 1000,
            tooltip: 1100
        };

        this.title.setTitle(AppComponent.APP_TITLE);
        this.branchSub = this.branchContext.currentBranch$.subscribe((branch) => {
            const branchName = (branch?.name || '').trim();
            this.title.setTitle(
                branchName
                    ? `${AppComponent.APP_TITLE} - ${branchName}`
                    : AppComponent.APP_TITLE
            );
        });
    }

    ngOnDestroy(): void {
        this.branchSub?.unsubscribe();
    }
}
