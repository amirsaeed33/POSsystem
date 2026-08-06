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
import { LookUpDto, LookUpTypes } from 'src/app/demo/api/lookup';
import { PermissionNames } from 'src/app/demo/api/permission-names';
import { BranchService } from 'src/app/demo/service/branch.service';
import { LookUpService } from 'src/app/demo/service/lookup.service';
import { PermissionService } from 'src/app/demo/service/permission.service';

@Component({
    selector: 'app-branch-form-dialog',
    templateUrl: './branch-form-dialog.component.html',
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

    constructor(
        private branchService: BranchService,
        private lookupService: LookUpService,
        private permissionService: PermissionService,
        private messageService: MessageService
    ) {}

    get dialogTitle(): string {
        return this.branchId ? 'Edit Branch' : 'Create Branch';
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

        this.saving = true;
        const imageBase64 = this.branch.imageBase64?.startsWith('data:image')
            ? this.branch.imageBase64
            : undefined;

        const createPayload: CreateBranchDto = {
            name,
            code,
            isActive: this.branch.isActive,
            isDefault: this.branch.isDefault,
            imageBase64,
            invoiceAddress: this.branch.invoiceAddress?.trim() || undefined,
            invoiceContactEmail:
                this.branch.invoiceContactEmail?.trim() || undefined,
            invoiceContactPhone:
                this.branch.invoiceContactPhone?.trim() || undefined,
            taxNumber: this.branch.taxNumber?.trim() || undefined,
            website: this.branch.website?.trim() || undefined,
            invoiceFooter: this.branch.invoiceFooter?.trim() || undefined,
        };

        const request = this.branchId
            ? this.branchService.update({
                  id: this.branchId,
                  ...createPayload,
                  statusId: this.canEditStatus
                      ? this.branch.statusId
                      : undefined,
                  isActive: createPayload.isActive ?? true,
                  isDefault: createPayload.isDefault ?? false,
                  imagePath: this.branch.imagePath,
              })
            : this.branchService.create(createPayload);

        request
            .then(() => {
                this.messageService.add({
                    severity: 'success',
                    summary: 'Success',
                    detail: this.branchId
                        ? 'Branch updated successfully'
                        : 'Branch created successfully',
                });
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
            isDefault: false,
            imagePath: undefined,
            imageBase64: undefined,
            invoiceAddress: '',
            invoiceContactEmail: '',
            invoiceContactPhone: '',
            taxNumber: '',
            website: '',
            invoiceFooter: '',
        };
    }

    private resetForm(): void {
        this.branch = this.emptyBranch();
        this.imagePreview = '';
        this.saving = false;
        this.loading = false;
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
