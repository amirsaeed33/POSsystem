import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { BranchService } from 'src/app/demo/service/branch.service';

@Component({
    templateUrl: './activate-branch.component.html',
})
export class ActivateBranchComponent implements OnInit {
    loading = true;
    success = false;
    errorMessage = '';
    branchName = '';
    tenantName = '';

    constructor(
        private route: ActivatedRoute,
        private router: Router,
        private branchService: BranchService
    ) {}

    ngOnInit(): void {
        this.route.queryParamMap.subscribe((params) => {
            const token = (params.get('token') || '').trim();
            if (!token) {
                this.loading = false;
                this.errorMessage = 'Activation token is missing.';
                return;
            }

            this.loading = true;
            this.errorMessage = '';
            this.branchService
                .activateBranch(token)
                .then((result) => {
                    this.success = true;
                    this.branchName = result.branchName || '';
                    this.tenantName = result.tenantName || result.tenancyName || '';
                })
                .catch((error) => {
                    this.errorMessage =
                        error?.message || 'Failed to activate branch.';
                })
                .finally(() => {
                    this.loading = false;
                });
        });
    }

    goToLogin(): void {
        this.router.navigate(['/auth/login']);
    }
}
