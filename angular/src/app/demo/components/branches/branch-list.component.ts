import { Component, OnInit } from '@angular/core';
import { ConfirmationService, MessageService } from 'primeng/api';
import { BranchDto, BranchStatuses } from 'src/app/demo/api/branch';
import { LookUpDto, LookUpTypes } from 'src/app/demo/api/lookup';
import { PermissionNames } from 'src/app/demo/api/permission-names';
import { BranchService } from 'src/app/demo/service/branch.service';
import { LookUpService } from 'src/app/demo/service/lookup.service';
import { PermissionService } from 'src/app/demo/service/permission.service';

@Component({
    templateUrl: './branch-list.component.html',
    styleUrls: ['./branch-list.component.scss'],
    providers: [MessageService, ConfirmationService],
})
export class BranchListComponent implements OnInit {
    branches: BranchDto[] = [];
    pendingApprovals: BranchDto[] = [];
    statusLookups: LookUpDto[] = [];
    loading = false;
    pendingLoading = false;
    totalRecords = 0;
    keyword = '';

    dialogVisible = false;
    editingBranchId: number | null = null;

    readonly BranchStatuses = BranchStatuses;
    readonly canApprove = () =>
        this.permissionService.isGranted(PermissionNames.BranchesApprove);

    /** Host admins approve locations; only tenants create them. */
    get canCreateBranch(): boolean {
        return !this.canApprove();
    }

    constructor(
        private branchService: BranchService,
        private lookupService: LookUpService,
        private permissionService: PermissionService,
        private messageService: MessageService,
        private confirmationService: ConfirmationService
    ) {}

    ngOnInit(): void {
        this.loadStatusLookups();
        this.loadBranches();
        if (this.canApprove()) {
            this.loadPendingApprovals();
        }
    }

    loadStatusLookups(): void {
        this.lookupService
            .getByType(LookUpTypes.BranchStatus)
            .then((items: LookUpDto[]) => {
                this.statusLookups = items || [];
            })
            .catch(() => {
                this.statusLookups = [];
            });
    }

    loadBranches(): void {
        this.loading = true;
        this.branchService
            .getAll({
                keyword: this.keyword?.trim() || undefined,
                skipCount: 0,
                maxResultCount: 1000,
            })
            .then((result) => {
                this.branches = result.items;
                this.totalRecords = result.totalCount;
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to load branches',
                });
            })
            .finally(() => {
                this.loading = false;
            });
    }

    loadPendingApprovals(): void {
        this.pendingLoading = true;
        this.branchService
            .getPendingApprovals()
            .then((items) => {
                this.pendingApprovals = items;
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to load pending approvals',
                });
            })
            .finally(() => {
                this.pendingLoading = false;
            });
    }

    onSearch(): void {
        this.loadBranches();
    }

    statusLabel(branchOrStatus?: BranchDto | string): string {
        if (branchOrStatus && typeof branchOrStatus !== 'string') {
            const branch = branchOrStatus;
            if (branch.statusDisplayName) {
                return branch.statusDisplayName;
            }
            const byId = this.statusLookups.find((x) => x.id === branch.statusId);
            if (byId?.displayName) {
                return byId.displayName;
            }
            return branch.status || BranchStatuses.Pending;
        }

        const code = branchOrStatus || BranchStatuses.Pending;
        const match = this.statusLookups.find(
            (x) => (x.name || '').toLowerCase() === code.toLowerCase()
        );
        return match?.displayName || code;
    }

    getImageUrl(branch: BranchDto): string {
        return this.branchService.getImageUrl(branch.imagePath);
    }

    branchInitials(branch: BranchDto): string {
        const source = (branch.name || branch.code || '?').trim();
        const parts = source.split(/\s+/).filter(Boolean);
        if (parts.length >= 2) {
            return (parts[0][0] + parts[1][0]).toUpperCase();
        }
        return source.substring(0, 2).toUpperCase();
    }

    openCreateDialog(): void {
        this.editingBranchId = null;
        this.dialogVisible = true;
    }

    openEditDialog(branch: BranchDto): void {
        this.editingBranchId = branch.id;
        this.dialogVisible = true;
    }

    onDialogSaved(): void {
        this.loadBranches();
        if (this.canApprove()) {
            this.loadPendingApprovals();
        }
    }

    onDelete(branch: BranchDto): void {
        this.confirmationService.confirm({
            message: `Are you sure you want to delete branch "${branch.name}"?`,
            header: 'Delete Confirmation',
            icon: 'pi pi-exclamation-triangle',
            acceptButtonStyleClass: 'p-button-danger',
            accept: () => {
                this.branchService
                    .delete(branch.id)
                    .then(() => {
                        this.messageService.add({
                            severity: 'success',
                            summary: 'Success',
                            detail: 'Branch deleted successfully',
                        });
                        this.loadBranches();
                    })
                    .catch((error) => {
                        this.messageService.add({
                            severity: 'error',
                            summary: 'Error',
                            detail: error?.message || 'Failed to delete branch',
                        });
                    });
            },
        });
    }
}
