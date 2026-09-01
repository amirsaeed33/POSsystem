import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { PermissionNames } from 'src/app/demo/api/permission-names';
import { PermissionService } from 'src/app/demo/service/permission.service';

type StaffTab = 'staff' | 'attendance' | 'payroll';

@Component({
    selector: 'app-staff-hub',
    templateUrl: './staff-hub.component.html',
})
export class StaffHubComponent implements OnInit {
    activeIndex = 0;
    canStaff = false;
    canAttendance = false;
    canPayroll = false;

    private readonly tabOrder: StaffTab[] = [];

    get tabOrderHasTabs(): boolean {
        return this.tabOrder.length > 0;
    }

    constructor(
        private permissionService: PermissionService,
        private route: ActivatedRoute,
        private router: Router
    ) {}

    async ngOnInit(): Promise<void> {
        try {
            await this.permissionService.ensureLoaded();
        } catch {
            // Use cached grants if available.
        }

        this.canStaff = this.permissionService.isGranted(PermissionNames.Staff);
        this.canAttendance = false; // Hidden as per request
        this.canPayroll = this.permissionService.isGranted(
            PermissionNames.StaffPayroll
        );

        if (this.canStaff) {
            this.tabOrder.push('staff');
        }
        if (this.canPayroll) {
            this.tabOrder.push('payroll');
        }

        const requested =
            (this.route.snapshot.queryParamMap.get('tab') as StaffTab) ||
            'staff';
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
