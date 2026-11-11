import { Component, OnInit } from '@angular/core';
import { ConfirmationService, MessageService } from 'primeng/api';
import { PermissionNames } from 'src/app/demo/api/permission-names';
import { SellerDto, SellerStatus, SellerStatuses } from 'src/app/demo/api/seller';
import { PermissionService } from 'src/app/demo/service/permission.service';
import { SellerService } from 'src/app/demo/service/seller.service';
import { RowActionItem } from 'src/app/shared/components/row-actions/row-actions.component';

@Component({
    selector: 'app-seller-list',
    templateUrl: './seller-list.component.html',
    styleUrls: ['./seller-list.component.scss'],
    providers: [MessageService, ConfirmationService],
})
export class SellerListComponent implements OnInit {
    sellers: SellerDto[] = [];
    loading = false;
    keyword = '';
    selectedStatus: SellerStatus | null = null;

    statusOptions = [
        { label: 'All statuses', value: null },
        { label: 'Pending', value: SellerStatuses.Pending },
        { label: 'Approved', value: SellerStatuses.Approved },
        { label: 'Rejected', value: SellerStatuses.Rejected },
    ];

    viewVisible = false;
    selected: SellerDto | null = null;

    rejectVisible = false;
    rejectTarget: SellerDto | null = null;
    rejectReason = '';
    rejecting = false;
    approvingId: number | null = null;

    readonly commissionPkr = 3000;

    readonly SellerStatuses = SellerStatuses;

    constructor(
        public sellerService: SellerService,
        private messageService: MessageService,
        private confirmationService: ConfirmationService,
        private permissionService: PermissionService
    ) {}

    get canApprove(): boolean {
        return this.permissionService.isGranted(PermissionNames.SellersApprove);
    }

    get canDelete(): boolean {
        return this.permissionService.isGranted(PermissionNames.SellersDelete);
    }

    get pendingCount(): number {
        return this.sellers.filter((s) => s.status === SellerStatuses.Pending).length;
    }

    ngOnInit(): void {
        void this.load();
    }

    initials(seller: SellerDto): string {
        const parts = (seller.fullName || '')
            .trim()
            .split(/\s+/)
            .filter(Boolean);
        if (!parts.length) {
            return '?';
        }
        if (parts.length === 1) {
            return parts[0].substring(0, 2).toUpperCase();
        }
        return (
            parts[0].charAt(0) + parts[parts.length - 1].charAt(0)
        ).toUpperCase();
    }

    async load(): Promise<void> {
        this.loading = true;
        try {
            const result = await this.sellerService.getAll({
                keyword: this.keyword?.trim() || undefined,
                status: this.selectedStatus,
                maxResultCount: 200,
            });
            this.sellers = result.items || [];
        } catch (err: any) {
            this.messageService.add({
                severity: 'error',
                summary: 'Error',
                detail: err?.message || 'Failed to load sellers',
            });
        } finally {
            this.loading = false;
        }
    }

    onSearch(): void {
        void this.load();
    }

    statusSeverity(status: SellerStatus): 'success' | 'warning' | 'danger' | 'info' {
        switch (status) {
            case SellerStatuses.Approved:
                return 'success';
            case SellerStatuses.Rejected:
                return 'danger';
            default:
                return 'warning';
        }
    }

    openView(seller: SellerDto): void {
        this.selected = seller;
        this.viewVisible = true;
    }

    getRowActions(seller: SellerDto): RowActionItem[] {
        return [
            {
                label: 'View',
                icon: 'pi pi-eye',
                action: () => this.openView(seller),
            },
            {
                label: 'Approve',
                icon: 'pi pi-check',
                visible: this.canApprove && seller.status !== SellerStatuses.Approved,
                action: () => this.confirmApprove(seller),
            },
            {
                label: 'Reject',
                icon: 'pi pi-times',
                danger: true,
                visible: this.canApprove && seller.status !== SellerStatuses.Rejected,
                action: () => this.openReject(seller),
            },
            {
                label: 'Delete',
                icon: 'pi pi-trash',
                danger: true,
                visible: this.canDelete,
                action: () => this.confirmDelete(seller),
            },
        ];
    }

    confirmApprove(seller: SellerDto): void {
        this.confirmationService.confirm({
            header: 'Approve seller',
            message: `Approve ${seller.fullName}? They will appear on the public landing page.`,
            icon: 'pi pi-check-circle',
            acceptLabel: 'Approve',
            rejectLabel: 'Cancel',
            accept: () => void this.approve(seller),
        });
    }

    async approve(seller: SellerDto): Promise<void> {
        if (!seller.id) {
            return;
        }
        this.approvingId = seller.id;
        try {
            await this.sellerService.approve(seller.id);
            this.messageService.add({
                severity: 'success',
                summary: 'Approved',
                detail: `${seller.fullName} is now an available seller.`,
            });
            await this.load();
            if (this.selected?.id === seller.id) {
                this.viewVisible = false;
            }
        } catch (err: any) {
            this.messageService.add({
                severity: 'error',
                summary: 'Error',
                detail: err?.message || 'Failed to approve seller',
            });
        } finally {
            this.approvingId = null;
        }
    }

    openReject(seller: SellerDto): void {
        this.rejectTarget = seller;
        this.rejectReason = '';
        this.rejectVisible = true;
    }

    async submitReject(): Promise<void> {
        if (!this.rejectTarget?.id) {
            return;
        }
        this.rejecting = true;
        try {
            await this.sellerService.reject({
                id: this.rejectTarget.id,
                rejectionReason: this.rejectReason?.trim() || undefined,
            });
            this.rejectVisible = false;
            this.messageService.add({
                severity: 'info',
                summary: 'Rejected',
                detail: `${this.rejectTarget.fullName} was rejected.`,
            });
            await this.load();
            if (this.selected?.id === this.rejectTarget.id) {
                this.viewVisible = false;
            }
        } catch (err: any) {
            this.messageService.add({
                severity: 'error',
                summary: 'Error',
                detail: err?.message || 'Failed to reject seller',
            });
        } finally {
            this.rejecting = false;
        }
    }

    confirmDelete(seller: SellerDto): void {
        this.confirmationService.confirm({
            header: 'Delete application',
            message: `Delete application from ${seller.fullName}?`,
            icon: 'pi pi-exclamation-triangle',
            acceptButtonStyleClass: 'p-button-danger',
            accept: () => void this.delete(seller),
        });
    }

    async delete(seller: SellerDto): Promise<void> {
        if (!seller.id) {
            return;
        }
        try {
            await this.sellerService.delete(seller.id);
            this.messageService.add({
                severity: 'success',
                summary: 'Deleted',
                detail: 'Seller application removed.',
            });
            await this.load();
        } catch (err: any) {
            this.messageService.add({
                severity: 'error',
                summary: 'Error',
                detail: err?.message || 'Failed to delete seller',
            });
        }
    }
}
