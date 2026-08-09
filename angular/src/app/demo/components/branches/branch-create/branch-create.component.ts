import { animate, style, transition, trigger } from '@angular/animations';
import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { MessageService } from 'primeng/api';
import { BranchDto, CreateBranchDto } from 'src/app/demo/api/branch';
import {
    HostCatalogByCompanyTypeDto,
    HostCatalogItemDto,
} from 'src/app/demo/api/host-catalog';
import { AuthService } from 'src/app/demo/service/auth.service';
import { BranchContextService } from 'src/app/demo/service/branch-context.service';
import { BranchService } from 'src/app/demo/service/branch.service';
import { HostCatalogService } from 'src/app/demo/service/host-catalog.service';
import { SessionService } from 'src/app/demo/service/session.service';

@Component({
    templateUrl: './branch-create.component.html',
    styleUrls: ['./branch-create.component.scss'],
    providers: [MessageService],
    animations: [
        trigger('panelSlide', [
            transition(
                ':enter',
                [
                    style({
                        transform: 'translateX({{fromX}})',
                        opacity: 0,
                    }),
                    animate(
                        '420ms {{delay}}ms cubic-bezier(0.22, 1, 0.36, 1)',
                        style({ transform: 'translateX(0)', opacity: 1 })
                    ),
                ],
                { params: { fromX: '48px', delay: 40 } }
            ),
        ]),
        trigger('fadeItem', [
            transition(
                ':enter',
                [
                    style({ transform: 'translateY(10px)', opacity: 0 }),
                    animate(
                        '320ms {{delay}}ms cubic-bezier(0.22, 1, 0.36, 1)',
                        style({ transform: 'translateY(0)', opacity: 1 })
                    ),
                ],
                { params: { delay: 0 } }
            ),
        ]),
    ],
})
export class BranchCreateComponent implements OnInit {
    activeStep = 0;
    slideFromX = '48px';
    saving = false;
    seedLoading = false;
    companyTypesLoading = false;

    readonly steps = [
        { label: 'Branch details' },
        { label: 'Invoice' },
        { label: 'Company type' },
        { label: 'Catalog' },
    ];

    branch: BranchDto = this.emptyBranch();
    companyTypes: HostCatalogItemDto[] = [];
    selectedCompanyTypeId: number | null = null;
    seedCatalog: HostCatalogByCompanyTypeDto | null = null;
    selectedHostItemIds: number[] = [];

    constructor(
        private branchService: BranchService,
        private branchContext: BranchContextService,
        private hostCatalogService: HostCatalogService,
        private sessionService: SessionService,
        private authService: AuthService,
        private router: Router,
        private messageService: MessageService
    ) {}

    ngOnInit(): void {
        this.loadCompanyTypes();
    }

    goNextFromDetails(): void {
        this.goToStep(1);
    }

    goNextFromInvoice(): void {
        this.goToStep(2);
    }

    goNextFromCompanyType(): void {
        this.goToStep(3);
    }

    goBack(): void {
        if (this.activeStep > 0) {
            this.goToStep(this.activeStep - 1);
        }
    }

    /** Jump directly via stepper click or Continue/Back. */
    goToStep(step: number): void {
        if (this.saving || step === this.activeStep) {
            return;
        }
        if (step < 0 || step >= this.steps.length) {
            return;
        }

        // Going forward: enforce prerequisites for the target step.
        if (step > this.activeStep) {
            if (step >= 1 && !this.hasBranchDetails()) {
                this.messageService.add({
                    severity: 'warn',
                    summary: 'Validation',
                    detail: 'Name and code are required',
                });
                this.setStep(0);
                return;
            }
            if (step >= 3 && !this.selectedCompanyTypeId) {
                this.messageService.add({
                    severity: 'warn',
                    summary: 'Company type',
                    detail: 'Select a company type to continue.',
                });
                this.setStep(2);
                return;
            }
        }

        this.setStep(step);
    }

    private hasBranchDetails(): boolean {
        return !!(this.branch.name || '').trim() && !!(this.branch.code || '').trim();
    }

    selectCompanyType(id: number): void {
        if (this.selectedCompanyTypeId === id && this.seedCatalog) {
            return;
        }
        this.selectedCompanyTypeId = id;
        this.onCompanyTypeChange();
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

    itemDelay(index: number): number {
        return Math.min(index * 40, 280);
    }

    submit(): void {
        const name = (this.branch.name || '').trim();
        const code = (this.branch.code || '').trim();
        if (!name || !code) {
            this.setStep(0);
            this.messageService.add({
                severity: 'warn',
                summary: 'Validation',
                detail: 'Name and code are required',
            });
            return;
        }
        if (!this.selectedCompanyTypeId) {
            this.setStep(1);
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
                detail: 'Select at least one category, unit, or brand',
            });
            return;
        }

        const optional = (value?: string | null) => {
            const trimmed = (value || '').trim();
            return trimmed ? trimmed : undefined;
        };

        const payload: CreateBranchDto = {
            name,
            code,
            isActive: this.branch.isActive ?? true,
            invoiceAddress: optional(this.branch.invoiceAddress),
            invoiceContactEmail: optional(this.branch.invoiceContactEmail),
            invoiceContactPhone: optional(this.branch.invoiceContactPhone),
            taxNumber: optional(this.branch.taxNumber),
            website: optional(this.branch.website),
            invoiceFooter: optional(this.branch.invoiceFooter),
            taxPercent: this.branch.taxPercent ?? 0,
            discountPercent: this.branch.discountPercent ?? 0,
            discountAmount: this.branch.discountAmount ?? 0,
            companyTypeId: this.selectedCompanyTypeId,
            hostCatalogItemIds: this.selectedHostItemIds,
        };

        this.saving = true;
        this.branchService
            .create(payload)
            .then(async (saved) => {
                if (saved) {
                    this.branchContext.setCurrentBranch(saved);
                }

                const sessionInfo =
                    await this.sessionService.getCurrentLoginInformations();
                if (sessionInfo?.user) {
                    this.authService.setUserInfo(sessionInfo.user);
                }

                this.messageService.add({
                    severity: 'success',
                    summary: 'Branch created',
                    detail: 'Awaiting host approval to seed catalog and activate.',
                });
                this.router.navigateByUrl('/branches');
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to create branch',
                });
            })
            .finally(() => {
                this.saving = false;
            });
    }

    private setStep(step: number): void {
        this.slideFromX = step > this.activeStep ? '48px' : '-48px';
        this.activeStep = step;
    }

    private loadCompanyTypes(): void {
        this.companyTypesLoading = true;
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
            })
            .finally(() => {
                this.companyTypesLoading = false;
            });
    }

    private emptyBranch(): BranchDto {
        return {
            id: 0,
            name: '',
            code: '',
            statusId: 0,
            status: 'Pending',
            isActive: true,
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
