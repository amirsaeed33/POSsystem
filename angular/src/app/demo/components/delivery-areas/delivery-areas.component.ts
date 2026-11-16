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
import { Table } from 'primeng/table';
import { RowActionItem } from 'src/app/shared/components/row-actions/row-actions.component';
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
    locating = false;
    editingAreaId: number | null = null;
    mapsPasteUrl = '';

    form: CreateDeliveryAreaDto = {
        branchId: undefined,
        name: '',
        deliveryFee: 100,
        minimumOrderAmount: 0,
        estimatedDeliveryTime: '30-45 mins',
        coverageDetails: '',
        latitude: null,
        longitude: null,
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

    onGlobalFilter(table: Table, event: Event): void {
        table.filterGlobal((event.target as HTMLInputElement).value, 'contains');
    }

    getRowActions(area: DeliveryAreaDto): RowActionItem[] {
        return [
            {
                label: 'Edit',
                icon: 'pi pi-pencil',
                visible: this.canManage,
                action: () => this.openEditDialog(area),
            },
            {
                label: area.isActive ? 'Deactivate' : 'Activate',
                icon: area.isActive ? 'pi pi-times-circle' : 'pi pi-check-circle',
                visible: this.canManage,
                action: () => this.toggleStatus(area),
            },
            {
                label: 'Delete',
                icon: 'pi pi-trash',
                danger: true,
                visible: this.canManage,
                action: () => this.confirmDelete(area),
            },
        ];
    }

    openCreateDialog(): void {
        this.dialogTitle = 'Add Delivery Area';
        this.editingAreaId = null;
        this.mapsPasteUrl = '';
        this.form = {
            branchId: this.selectedBranchId ?? this.branchContext.getBranchId() ?? undefined,
            name: '',
            deliveryFee: 100,
            minimumOrderAmount: 0,
            estimatedDeliveryTime: '30-45 mins',
            coverageDetails: '',
            latitude: null,
            longitude: null,
            isActive: true,
        };
        this.dialogVisible = true;
    }

    openEditDialog(area: DeliveryAreaDto): void {
        this.dialogTitle = `Edit Delivery Area - ${area.name}`;
        this.editingAreaId = area.id;
        this.mapsPasteUrl = '';
        this.form = {
            branchId: area.branchId,
            name: area.name,
            deliveryFee: area.deliveryFee,
            minimumOrderAmount: area.minimumOrderAmount,
            estimatedDeliveryTime: area.estimatedDeliveryTime || '',
            coverageDetails: area.coverageDetails || '',
            latitude: area.latitude ?? null,
            longitude: area.longitude ?? null,
            isActive: area.isActive,
        };
        this.dialogVisible = true;
    }

    get googleMapsAreaLink(): string | null {
        if (this.form.latitude != null && this.form.longitude != null) {
            return `https://maps.google.com/?q=${this.form.latitude},${this.form.longitude}`;
        }
        return null;
    }

    openGoogleMapsPicker(): void {
        const url =
            this.form.latitude != null && this.form.longitude != null
                ? `https://www.google.com/maps/@${this.form.latitude},${this.form.longitude},16z`
                : 'https://www.google.com/maps';
        window.open(url, '_blank', 'noopener,noreferrer');
        this.messageService.add({
            severity: 'info',
            summary: 'Google Maps',
            detail: 'Right-click a point → copy coordinates, or paste the Maps URL below.',
            life: 6000,
        });
    }

    detectCurrentLocation(): void {
        if (!navigator.geolocation) {
            this.messageService.add({
                severity: 'error',
                summary: 'GPS Not Supported',
                detail: 'Geolocation is not supported by your browser.',
            });
            return;
        }
        this.locating = true;
        navigator.geolocation.getCurrentPosition(
            (pos) => {
                this.form.latitude = Number(pos.coords.latitude.toFixed(6));
                this.form.longitude = Number(pos.coords.longitude.toFixed(6));
                this.locating = false;
                this.messageService.add({
                    severity: 'success',
                    summary: 'Location Captured',
                    detail: `Coordinates set to (${this.form.latitude}, ${this.form.longitude})`,
                });
            },
            () => {
                this.locating = false;
                this.messageService.add({
                    severity: 'warn',
                    summary: 'GPS Detection',
                    detail: 'Unable to detect GPS. Open Google Maps and paste the URL or enter lat/lng manually.',
                });
            },
            { enableHighAccuracy: true, timeout: 10000, maximumAge: 0 }
        );
    }

    clearAreaLocation(): void {
        this.form.latitude = null;
        this.form.longitude = null;
        this.mapsPasteUrl = '';
    }

    applyMapsPasteUrl(): void {
        const parsed = this.parseLatLngFromText(this.mapsPasteUrl);
        if (!parsed) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Invalid Maps Link',
                detail: 'Paste a Google Maps URL or coordinates like 31.5204, 74.3587',
            });
            return;
        }
        this.form.latitude = parsed.lat;
        this.form.longitude = parsed.lng;
        this.messageService.add({
            severity: 'success',
            summary: 'Location Set',
            detail: `Coordinates set to (${parsed.lat}, ${parsed.lng})`,
        });
    }

    private parseLatLngFromText(raw: string): { lat: number; lng: number } | null {
        const text = (raw || '').trim();
        if (!text) {
            return null;
        }

        const atMatch = text.match(/@(-?\d+(?:\.\d+)?),\s*(-?\d+(?:\.\d+)?)/);
        if (atMatch) {
            return this.toLatLng(atMatch[1], atMatch[2]);
        }

        const qMatch = text.match(/[?&]q=(-?\d+(?:\.\d+)?),\s*(-?\d+(?:\.\d+)?)/i);
        if (qMatch) {
            return this.toLatLng(qMatch[1], qMatch[2]);
        }

        const queryMatch = text.match(/[?&]query=(-?\d+(?:\.\d+)?),\s*(-?\d+(?:\.\d+)?)/i);
        if (queryMatch) {
            return this.toLatLng(queryMatch[1], queryMatch[2]);
        }

        const plainMatch = text.match(/^(-?\d+(?:\.\d+)?)\s*,\s*(-?\d+(?:\.\d+)?)$/);
        if (plainMatch) {
            return this.toLatLng(plainMatch[1], plainMatch[2]);
        }

        return null;
    }

    private toLatLng(latRaw: string, lngRaw: string): { lat: number; lng: number } | null {
        const lat = Number(latRaw);
        const lng = Number(lngRaw);
        if (!Number.isFinite(lat) || !Number.isFinite(lng)) {
            return null;
        }
        if (lat < -90 || lat > 90 || lng < -180 || lng > 180) {
            return null;
        }
        return {
            lat: Number(lat.toFixed(6)),
            lng: Number(lng.toFixed(6)),
        };
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
            const latitude =
                this.form.latitude != null && Number.isFinite(Number(this.form.latitude))
                    ? Number(this.form.latitude)
                    : null;
            const longitude =
                this.form.longitude != null && Number.isFinite(Number(this.form.longitude))
                    ? Number(this.form.longitude)
                    : null;

            if (this.editingAreaId) {
                await this.deliveryAreaService.update({
                    id: this.editingAreaId,
                    branchId: activeBranchId || 0,
                    name: this.form.name.trim(),
                    deliveryFee: this.form.deliveryFee,
                    minimumOrderAmount: this.form.minimumOrderAmount || 0,
                    estimatedDeliveryTime: this.form.estimatedDeliveryTime?.trim(),
                    coverageDetails: this.form.coverageDetails?.trim(),
                    latitude,
                    longitude,
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
                    latitude,
                    longitude,
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
