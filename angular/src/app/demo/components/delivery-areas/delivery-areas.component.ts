import { Component, OnInit, OnDestroy } from '@angular/core';
import { ConfirmationService, MessageService } from 'primeng/api';
import { Subscription } from 'rxjs';
import {
    DeliveryAreaDto,
    CreateDeliveryAreaDto,
    DeliveryAreaService,
} from '../../service/delivery-area.service';
import { BranchContextService } from '../../service/branch-context.service';
import { PermissionService } from '../../service/permission.service';
import { PermissionNames } from '../../api/permission-names';

@Component({
    selector: 'app-delivery-areas',
    templateUrl: './delivery-areas.component.html',
    styleUrls: ['./delivery-areas.component.scss'],
    providers: [MessageService, ConfirmationService],
})
export class DeliveryAreasComponent implements OnInit, OnDestroy {
    areas: DeliveryAreaDto[] = [];
    selectedBranchId: number | null = null;
    keyword = '';
    activeFilter: boolean | null = null;
    loading = false;
    totalRecords = 0;

    dialogVisible = false;
    dialogTitle = 'Add Delivery Area';
    saving = false;
    editingAreaId: number | null = null;

    form: CreateDeliveryAreaDto = {
        branchId: undefined,
        name: '',
        deliveryFee: 100,
        minimumOrderAmount: 0,
        estimatedDeliveryTime: '30-45 mins',
        coverageDetails: '',
        isActive: true,
    };

    canManage = true;

    activeFilterOptions = [
        { label: 'All Statuses', value: null },
        { label: 'Active Only', value: true },
        { label: 'Inactive Only', value: false },
    ];

    private branchSub?: Subscription;

    constructor(
        private deliveryAreaService: DeliveryAreaService,
        private branchContext: BranchContextService,
        private messageService: MessageService,
        private confirmationService: ConfirmationService,
        private permissionService: PermissionService
    ) {}

    ngOnInit(): void {
        this.canManage =
            this.permissionService.isGranted(PermissionNames.DeliveryAreas) ||
            this.permissionService.isGranted(PermissionNames.DeliveryAreasCreate) ||
            this.permissionService.isGranted(PermissionNames.DeliveryAreasEdit);

        this.branchSub = this.branchContext.currentBranch$.subscribe((branch) => {
            this.selectedBranchId = branch?.id ?? null;
            this.loadAreas();
        });
    }

    ngOnDestroy(): void {
        this.branchSub?.unsubscribe();
    }

    async loadAreas(): Promise<void> {
        this.loading = true;
        try {
            const branchId = this.selectedBranchId ?? this.branchContext.getBranchId() ?? undefined;
            const res = await this.deliveryAreaService.getAll({
                keyword: this.keyword?.trim() || undefined,
                branchId: branchId,
                isActive: this.activeFilter ?? undefined,
                skipCount: 0,
                maxResultCount: 1000,
            });
            this.areas = res.items;
            this.totalRecords = res.totalCount;
        } catch (error: any) {
            this.messageService.add({
                severity: 'error',
                summary: 'Error',
                detail: error?.message || 'Failed to load delivery areas',
            });
        } finally {
            this.loading = false;
        }
    }

    onSearch(): void {
        this.loadAreas();
    }

    onFilterChange(): void {
        this.loadAreas();
    }

    openCreateDialog(): void {
        this.dialogTitle = 'Add Delivery Area';
        this.editingAreaId = null;
        this.form = {
            branchId: this.selectedBranchId ?? this.branchContext.getBranchId() ?? undefined,
            name: '',
            deliveryFee: 100,
            minimumOrderAmount: 0,
            estimatedDeliveryTime: '30-45 mins',
            coverageDetails: '',
            isActive: true,
        };
        this.dialogVisible = true;
    }

    openEditDialog(area: DeliveryAreaDto): void {
        this.dialogTitle = `Edit Delivery Area - ${area.name}`;
        this.editingAreaId = area.id;
        this.form = {
            branchId: area.branchId,
            name: area.name,
            deliveryFee: area.deliveryFee,
            minimumOrderAmount: area.minimumOrderAmount,
            estimatedDeliveryTime: area.estimatedDeliveryTime || '',
            coverageDetails: area.coverageDetails || '',
            isActive: area.isActive,
        };
        this.dialogVisible = true;
    }

    async saveArea(): Promise<void> {
        if (!this.form.name || !this.form.name.trim()) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Validation',
                detail: 'Please enter a valid area name.',
            });
            return;
        }

        if (this.form.deliveryFee < 0) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Validation',
                detail: 'Delivery fee cannot be negative.',
            });
            return;
        }

        const activeBranchId = this.form.branchId || this.branchContext.getBranchId() || undefined;

        this.saving = true;
        try {
            if (this.editingAreaId) {
                await this.deliveryAreaService.update({
                    id: this.editingAreaId,
                    branchId: activeBranchId || 0,
                    name: this.form.name.trim(),
                    deliveryFee: this.form.deliveryFee,
                    minimumOrderAmount: this.form.minimumOrderAmount || 0,
                    estimatedDeliveryTime: this.form.estimatedDeliveryTime?.trim(),
                    coverageDetails: this.form.coverageDetails?.trim(),
                    isActive: this.form.isActive,
                });
                this.messageService.add({
                    severity: 'success',
                    summary: 'Updated',
                    detail: 'Delivery area updated successfully.',
                });
            } else {
                await this.deliveryAreaService.create({
                    branchId: activeBranchId,
                    name: this.form.name.trim(),
                    deliveryFee: this.form.deliveryFee,
                    minimumOrderAmount: this.form.minimumOrderAmount || 0,
                    estimatedDeliveryTime: this.form.estimatedDeliveryTime?.trim(),
                    coverageDetails: this.form.coverageDetails?.trim(),
                    isActive: this.form.isActive,
                });
                this.messageService.add({
                    severity: 'success',
                    summary: 'Created',
                    detail: 'Delivery area created successfully.',
                });
            }
            this.dialogVisible = false;
            await this.loadAreas();
        } catch (error: any) {
            this.messageService.add({
                severity: 'error',
                summary: 'Error',
                detail: error?.message || 'Failed to save delivery area',
            });
        } finally {
            this.saving = false;
        }
    }

    async toggleStatus(area: DeliveryAreaDto): Promise<void> {
        try {
            const updated = await this.deliveryAreaService.toggleActive(area.id);
            area.isActive = updated.isActive;
            this.messageService.add({
                severity: 'info',
                summary: 'Status Changed',
                detail: `${area.name} is now ${area.isActive ? 'Active' : 'Inactive'}.`,
            });
        } catch (error: any) {
            this.messageService.add({
                severity: 'error',
                summary: 'Error',
                detail: error?.message || 'Failed to change area status',
            });
        }
    }

    confirmDelete(area: DeliveryAreaDto): void {
        this.confirmationService.confirm({
            message: `Are you sure you want to delete delivery area "${area.name}"? Past orders will retain their historic fee record.`,
            header: 'Confirm Deletion',
            icon: 'pi pi-exclamation-triangle',
            acceptButtonStyleClass: 'p-button-danger',
            accept: async () => {
                try {
                    await this.deliveryAreaService.delete(area.id);
                    this.messageService.add({
                        severity: 'success',
                        summary: 'Deleted',
                        detail: `Delivery area "${area.name}" deleted successfully.`,
                    });
                    await this.loadAreas();
                } catch (error: any) {
                    this.messageService.add({
                        severity: 'error',
                        summary: 'Error',
                        detail: error?.message || 'Failed to delete delivery area',
                    });
                }
            },
        });
    }
}
