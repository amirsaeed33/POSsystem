import {
    Component,
    EventEmitter,
    Input,
    OnChanges,
    Output,
    SimpleChanges,
} from '@angular/core';
import { MessageService } from 'primeng/api';
import {
    BranchDto,
    BranchStatuses,
    CreateBranchDto,
} from 'src/app/demo/api/branch';
import {
    HostCatalogByCompanyTypeDto,
    HostCatalogItemDto,
} from 'src/app/demo/api/host-catalog';
import { LookUpDto, LookUpTypes } from 'src/app/demo/api/lookup';
import { PermissionNames } from 'src/app/demo/api/permission-names';
import { BranchContextService } from 'src/app/demo/service/branch-context.service';
import { BranchService } from 'src/app/demo/service/branch.service';
import { HostCatalogService } from 'src/app/demo/service/host-catalog.service';
import { LookUpService } from 'src/app/demo/service/lookup.service';
import { PermissionService } from 'src/app/demo/service/permission.service';

@Component({
    selector: 'app-branch-form-dialog',
    templateUrl: './branch-form-dialog.component.html',
    styleUrls: ['./branch-form-dialog.component.scss'],
})
export class BranchFormDialogComponent implements OnChanges {
    @Input() visible = false;
    @Input() branchId: number | null = null;
    @Output() visibleChange = new EventEmitter<boolean>();
    @Output() saved = new EventEmitter<void>();

    branch: BranchDto = this.emptyBranch();
    statusOptions: LookUpDto[] = [];
    imagePreview = '';
    saving = false;
    loading = false;
    private originalStatusId = 0;

    companyTypes: HostCatalogItemDto[] = [];
    selectedCompanyTypeId: number | null = null;
    seedCatalog: HostCatalogByCompanyTypeDto | null = null;
    selectedHostItemIds: number[] = [];
    seedLoading = false;

    constructor(
        private branchService: BranchService,
        private branchContext: BranchContextService,
        private lookupService: LookUpService,
        private hostCatalogService: HostCatalogService,
        private permissionService: PermissionService,
        private messageService: MessageService
    ) {}

    get dialogTitle(): string {
        return this.branchId ? 'Edit Branch' : 'Create Branch';
    }

    get isEditMode(): boolean {
        return !!this.branchId;
    }

    get isCreateMode(): boolean {
        return !this.branchId;
    }

    get dialogStyle(): Record<string, string> {
        return this.isEditMode
            ? {
                  width: '100vw',
                  height: '100vh',
                  maxHeight: '100vh',
                  margin: '0',
              }
            : { width: '48rem' };
    }

    get dialogContentStyle(): Record<string, string> | null {
        return this.isEditMode
            ? { height: 'calc(100vh - 9rem)', overflow: 'auto' }
            : { maxHeight: '70vh', overflow: 'auto' };
    }

    /** Host admin with approve permission — Status dropdown on edit. */
    get canEditStatus(): boolean {
        return (
            !!this.branchId &&
            this.permissionService.isGranted(PermissionNames.BranchesApprove)
        );
    }

    ngOnChanges(changes: SimpleChanges): void {
        if (changes['visible'] && this.visible) {
            this.resetForm();
            if (this.canEditStatus) {
                this.loadStatusOptions();
            }
            if (this.branchId) {
                this.loadBranch(this.branchId);
            } else {
                this.loadCompanyTypes();
            }
        }
    }

    onVisibleChange(visible: boolean): void {
        this.visible = visible;
        this.visibleChange.emit(visible);
    }

    onHide(): void {
        this.onVisibleChange(false);
    }

    onFileSelected(event: Event): void {
        const input = event.target as HTMLInputElement;
        if (!input.files?.length) {
            return;
        }

        const file = input.files[0];
        const reader = new FileReader();
        reader.onload = () => {
            const result = reader.result as string;
            if (!result?.startsWith('data:image')) {
                return;
            }
            this.imagePreview = result;
            this.branch.imageBase64 = result;
        };
        reader.readAsDataURL(file);
    }

    onCompanyTypeChange(): void {
        this.selectedHostItemIds = [];
        this.seedCatalog = null;
        if (!this.selectedCompanyTypeId) {
            return;
        }

        this.seedLoading = true;
        this.hostCatalogService
            .getCatalogByCompanyType(this.selectedCompanyTypeId)
            .then((catalog) => {
                this.seedCatalog = catalog;
                this.selectedHostItemIds = [
                    ...catalog.categories.map((x) => x.id),
                    ...catalog.units.map((x) => x.id),
                    ...catalog.brands.map((x) => x.id),
                ];
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to load seed catalog',
                });
            })
            .finally(() => {
                this.seedLoading = false;
            });
    }

    isHostItemSelected(id: number): boolean {
        return this.selectedHostItemIds.includes(id);
    }

    toggleHostItem(id: number, checked: boolean): void {
        if (checked) {
            if (!this.selectedHostItemIds.includes(id)) {
                this.selectedHostItemIds = [...this.selectedHostItemIds, id];
            }
        } else {
            this.selectedHostItemIds = this.selectedHostItemIds.filter(
                (x) => x !== id
            );
        }
    }

    save(): void {
        const name = (this.branch.name || '').trim();
        const code = (this.branch.code || '').trim();
        if (!name || !code) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Validation',
                detail: 'Name and code are required',
            });
            return;
        }

        if (this.canEditStatus && !this.branch.statusId) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Validation',
                detail: 'Status is required',
            });
            return;
        }

        if (this.isCreateMode) {
            if (!this.selectedCompanyTypeId) {
                this.messageService.add({
                    severity: 'warn',
                    summary: 'Validation',
                    detail: 'Select your company type',
                });
                return;
            }
            if (!this.selectedHostItemIds.length) {
                this.messageService.add({
                    severity: 'warn',
                    summary: 'Validation',
                    detail: 'Select at least one category, unit, or brand to seed',
                });
                return;
            }
        }

        this.saving = true;
        const imageBase64 = this.branch.imageBase64?.startsWith('data:image')
            ? this.branch.imageBase64
            : undefined;

        const createPayload: CreateBranchDto = {
            name,
            code,
            isActive: this.branch.isActive,
            imageBase64,
            invoiceAddress: this.branch.invoiceAddress?.trim() || undefined,
            invoiceContactEmail:
                this.branch.invoiceContactEmail?.trim() || undefined,
            invoiceContactPhone:
                this.branch.invoiceContactPhone?.trim() || undefined,
            taxNumber: this.branch.taxNumber?.trim() || undefined,
            website: this.branch.website?.trim() || undefined,
            invoiceFooter: this.branch.invoiceFooter?.trim() || undefined,
            taxPercent: this.branch.taxPercent ?? 0,
            discountPercent: this.branch.discountPercent ?? 0,
            discountAmount: this.branch.discountAmount ?? 0,
            companyTypeId: this.selectedCompanyTypeId || undefined,
            hostCatalogItemIds: this.selectedHostItemIds,
        };

        const selectedStatus = this.statusOptions.find(
            (x) => x.id === this.branch.statusId
        );
        const requestingActivation =
            this.canEditStatus &&
            this.branch.statusId !== this.originalStatusId &&
            (selectedStatus?.name || '').toLowerCase() ===
                BranchStatuses.Approved.toLowerCase();

        const request = this.branchId
            ? this.branchService.update({
                  id: this.branchId,
                  ...createPayload,
                  statusId: this.canEditStatus
                      ? this.branch.statusId
                      : undefined,
                  isActive: createPayload.isActive ?? true,
                  imagePath: this.branch.imagePath,
              })
            : this.branchService.create(createPayload);

        request
            .then(async (saved) => {
                this.messageService.add({
                    severity: 'success',
                    summary: requestingActivation
                        ? 'Activation email sent'
                        : 'Success',
                    detail: requestingActivation
                        ? 'Seed data copied and activation email sent. Branch stays pending until the link is opened.'
                        : this.branchId
                          ? 'Branch updated successfully'
                          : 'Branch created successfully. Awaiting host approval to seed catalog.',
                });
                if (
                    saved?.id &&
                    this.branchContext.getBranchId() === saved.id
                ) {
                    this.branchContext.setCurrentBranch(saved);
                }
                this.saved.emit();
                this.onHide();
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to save branch',
                });
            })
            .finally(() => {
                this.saving = false;
            });
    }

    private emptyBranch(): BranchDto {
        return {
            id: 0,
            name: '',
            code: '',
            statusId: 0,
            status: BranchStatuses.Pending,
            creationTime: undefined,
            isActive: true,
            imagePath: undefined,
            imageBase64: undefined,
            invoiceAddress: '',
            invoiceContactEmail: '',
            invoiceContactPhone: '',
            taxNumber: '',
            website: '',
            invoiceFooter: '',
            taxPercent: 0,
            discountPercent: 0,
            discountAmount: 0,
        };
    }

    private resetForm(): void {
        this.branch = this.emptyBranch();
        this.imagePreview = '';
        this.saving = false;
        this.loading = false;
        this.originalStatusId = 0;
        this.companyTypes = [];
        this.selectedCompanyTypeId = null;
        this.seedCatalog = null;
        this.selectedHostItemIds = [];
        this.seedLoading = false;
    }

    private loadCompanyTypes(): void {
        this.hostCatalogService
            .getCompanyTypesForSeed()
            .then((items) => {
                this.companyTypes = items || [];
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to load company types',
                });
            });
    }

    private loadStatusOptions(): void {
        this.lookupService
            .getByType(LookUpTypes.BranchStatus)
            .then((items) => {
                this.statusOptions = items || [];
            })
            .catch(() => {
                this.statusOptions = [];
            });
    }

    private loadBranch(id: number): void {
        this.loading = true;
        this.branchService
            .get(id)
            .then((branch) => {
                this.branch = { ...branch, imageBase64: undefined };
                this.originalStatusId = branch.statusId || 0;
                this.imagePreview = this.branchService.getImageUrl(
                    branch.imagePath
                );
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to load branch',
                });
                this.onHide();
            })
            .finally(() => {
                this.loading = false;
            });
    }
}
