import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { MessageService } from 'primeng/api';

import {
    BranchDto,
    BranchStatuses,
    CreateBranchDto
} from 'src/app/demo/api/branch';
import { LookUpDto, LookUpTypes } from 'src/app/demo/api/lookup';
import { PermissionNames } from 'src/app/demo/api/permission-names';
import { BranchContextService } from 'src/app/demo/service/branch-context.service';
import { BranchService } from 'src/app/demo/service/branch.service';
import { LookUpService } from 'src/app/demo/service/lookup.service';
import { PermissionService } from 'src/app/demo/service/permission.service';

@Component({
    selector: 'app-branch-edit',
    templateUrl: './branch-edit.component.html',
    styleUrls: ['./branch-edit.component.scss'],
})
export class BranchEditComponent implements OnInit {

    branchId!: number;

    branch: BranchDto = this.emptyBranch();
    statusOptions: LookUpDto[] = [];

    imagePreview = '';
    saving = false;
    loading = false;
    private originalStatusId = 0;

    constructor(
        private branchService: BranchService,
        private branchContext: BranchContextService,
        private lookupService: LookUpService,
        private permissionService: PermissionService,
        private messageService: MessageService,
        private route: ActivatedRoute,
        private router: Router
    ) { }

    get canEditStatus(): boolean {
        return this.permissionService.isGranted(
            PermissionNames.BranchesApprove
        );
    }

    get hasImage(): boolean {
        return !!(this.imagePreview || this.branch.imageBase64 || this.branch.imagePath);
    }

    ngOnInit(): void {

        this.branchId = Number(
            this.route.snapshot.paramMap.get('id')
        );

        if (this.canEditStatus) {
            this.loadStatusOptions();
        }

        if (this.branchId) {
            this.loadBranch(this.branchId);
        }
    }


    clearImage(): void {
        this.imagePreview = '';
        this.branch.imageBase64 = '';
        this.branch.imagePath = '';
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

        const hasImage = this.hasImage;

        const imageBase64 =
            this.branch.imageBase64?.startsWith('data:image')
                ? this.branch.imageBase64
                : (hasImage ? undefined : null);

        const imagePath = hasImage ? (this.branch.imagePath ?? undefined) : null;


        const payload: CreateBranchDto = {
            name,
            code,
            isActive: this.branch.isActive,
            imageBase64,

            invoiceAddress:
                this.branch.invoiceAddress?.trim() || undefined,

            invoiceContactEmail:
                this.branch.invoiceContactEmail?.trim() || undefined,

            invoiceContactPhone:
                this.branch.invoiceContactPhone?.trim() || undefined,

            taxNumber:
                this.branch.taxNumber?.trim() || undefined,

            website:
                this.branch.website?.trim() || undefined,

            invoiceFooter:
                this.branch.invoiceFooter?.trim() || undefined,

            taxPercent: this.branch.taxPercent ?? 0,
            discountPercent: this.branch.discountPercent ?? 0,
            discountAmount: this.branch.discountAmount ?? 0,
        };


        const selectedStatus = this.statusOptions.find(
            (x) => x.id === this.branch.statusId
        );
        const requestingActivation =
            this.canEditStatus &&
            this.branch.statusId !== this.originalStatusId &&
            (selectedStatus?.name || '').toLowerCase() ===
                BranchStatuses.Approved.toLowerCase();

        this.branchService
            .update({
                id: this.branchId,
                ...payload,
                statusId: this.canEditStatus
                    ? this.branch.statusId
                    : undefined,
                isActive: payload.isActive ?? true,
                imagePath: imagePath,
            })
            .then((saved) => {

                this.messageService.add({
                    severity: 'success',
                    summary: requestingActivation
                        ? 'Activation email sent'
                        : 'Success',
                    detail: requestingActivation
                        ? 'An activation link was emailed. The branch stays pending until the link is opened.'
                        : 'Branch updated successfully',
                });


                if (
                    saved?.id &&
                    this.branchContext.getBranchId() === saved.id
                ) {
                    this.branchContext.setCurrentBranch(saved);
                }


                this.router.navigate(['/branches']);
            })
            .catch((error) => {

                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail:
                        error?.message ||
                        'Failed to update branch',
                });

            })
            .finally(() => {
                this.saving = false;
            });
    }


    onCancel(): void {
        this.router.navigate(['/branches']);
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
            input.value = '';
        };
        reader.readAsDataURL(file);
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

                this.branch = {
                    ...branch,
                    imageBase64: undefined
                };
                this.originalStatusId = branch.statusId || 0;

                this.imagePreview =
                    this.branchService.getImageUrl(
                        branch.imagePath
                    );
            })
            .catch((error) => {

                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail:
                        error?.message ||
                        'Failed to load branch',
                });

                this.router.navigate(['/branches']);

            })
            .finally(() => {
                this.loading = false;
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
}