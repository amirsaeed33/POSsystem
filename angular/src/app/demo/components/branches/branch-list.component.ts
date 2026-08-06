import { Component, OnInit, ViewChild } from '@angular/core';
import { ConfirmationService, MenuItem, MessageService } from 'primeng/api';
import { Menu } from 'primeng/menu';
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
    @ViewChild('cardMenu') cardMenu!: Menu;

    branches: BranchDto[] = [];
    pendingApprovals: BranchDto[] = [];
    statusLookups: LookUpDto[] = [];
    loading = false;
    pendingLoading = false;
    totalRecords = 0;
    keyword = '';

    dialogVisible = false;
    editingBranchId: number | null = null;
    cardMenuItems: MenuItem[] = [];

    readonly BranchStatuses = BranchStatuses;
    readonly canApprove = () =>
        this.permissionService.isGranted(PermissionNames.BranchesApprove);

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

    private statusIdByName(name: string): number | null {
        const match = this.statusLookups.find(
            (x) => (x.name || '').toLowerCase() === name.toLowerCase()
        );
        return match?.id ?? null;
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

    openCardMenu(event: Event, branch: BranchDto): void {
        event.preventDefault();
        event.stopPropagation();
        this.cardMenuItems = this.buildCardMenu(branch);
        this.cardMenu.toggle(event);
    }

    private buildCardMenu(branch: BranchDto): MenuItem[] {
        const items: MenuItem[] = [];
        const hostReview = this.canApprove() && !!branch.tenantId;
        const canManage = !this.canApprove() || !branch.tenantId;

        if (canManage) {
            items.push({
                label: 'Edit',
                icon: 'pi pi-pencil',
                command: () => this.openEditDialog(branch),
            });
        }

        if (hostReview) {
            items.push({
                label: 'Approve',
                icon: 'pi pi-check',
                disabled: branch.status === BranchStatuses.Approved,
                command: () => this.approve(branch),
            });
            items.push({
                label: 'Reject',
                icon: 'pi pi-times',
                disabled: branch.status === BranchStatuses.Rejected,
                command: () => this.reject(branch),
            });
            items.push({
                label: 'Set Pending',
                icon: 'pi pi-replay',
                disabled: branch.status === BranchStatuses.Pending,
                command: () => this.setPending(branch),
            });
        }

        if (canManage) {
            if (items.length) {
                items.push({ separator: true });
            }
            items.push({
                label: 'Delete',
                icon: 'pi pi-trash',
                styleClass: 'branch-menu-danger',
                command: () => this.onDelete(branch),
            });
        }

        return items;
    }

    onDialogSaved(): void {
        this.loadBranches();
        if (this.canApprove()) {
            this.loadPendingApprovals();
        }
    }

    approve(branch: BranchDto): void {
        this.setStatus(branch, BranchStatuses.Approved, 'approved');
    }

    reject(branch: BranchDto): void {
        this.confirmationService.confirm({
            message: `Reject branch "${branch.name}" for tenant "${branch.tenancyName || branch.tenantId}"?`,
            header: 'Reject branch',
            icon: 'pi pi-exclamation-triangle',
            acceptButtonStyleClass: 'p-button-danger',
            accept: () => this.setStatus(branch, BranchStatuses.Rejected, 'rejected'),
        });
    }

    setPending(branch: BranchDto): void {
        this.confirmationService.confirm({
            message: `Set branch "${branch.name}" back to Pending? Tenant users will be blocked until approved again.`,
            header: 'Set Pending',
            icon: 'pi pi-exclamation-triangle',
            accept: () => this.setStatus(branch, BranchStatuses.Pending, 'set to pending'),
        });
    }

    private setStatus(branch: BranchDto, statusName: string, verb: string): void {
        const statusId = this.statusIdByName(statusName);
        if (!statusId) {
            this.messageService.add({
                severity: 'error',
                summary: 'Error',
                detail: `Branch status "${statusName}" was not found in lookups`,
            });
            return;
        }

        this.branchService
            .changeStatus(branch.id, statusId)
            .then(() => {
                this.messageService.add({
                    severity: 'success',
                    summary: 'Success',
                    detail: `Branch ${verb} successfully`,
                });
                this.loadPendingApprovals();
                this.loadBranches();
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || `Failed to ${verb} branch`,
                });
            });
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
