import { Component, OnInit } from '@angular/core';
import { Table } from 'primeng/table';
import { ConfirmationService, MessageService } from 'primeng/api';
import { CompanyProfileDto } from 'src/app/demo/api/company-profile';
import { CompanyProfileService } from 'src/app/demo/service/company-profile.service';

@Component({
    templateUrl: './company-profile-list.component.html',
    providers: [MessageService, ConfirmationService],
})
export class CompanyProfileListComponent implements OnInit {
    profiles: CompanyProfileDto[] = [];
    loading = false;
    totalRecords = 0;
    keyword = '';

    dialogVisible = false;
    editingProfileId: number | null = null;

    receiptPreviewVisible = false;
    previewProfile: CompanyProfileDto | null = null;

    constructor(
        private companyProfileService: CompanyProfileService,
        private messageService: MessageService,
        private confirmationService: ConfirmationService
    ) {}

    ngOnInit(): void {
        this.loadProfiles();
    }

    loadProfiles(): void {
        this.loading = true;
        this.companyProfileService
            .getAll({
                keyword: this.keyword?.trim() || undefined,
                skipCount: 0,
                maxResultCount: 1000,
            })
            .then((result) => {
                this.profiles = result.items;
                this.totalRecords = result.totalCount;
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail:
                        error?.message || 'Failed to load company profiles',
                });
            })
            .finally(() => {
                this.loading = false;
            });
    }

    onSearch(): void {
        this.loadProfiles();
    }

    onGlobalFilter(table: Table, event: Event): void {
        table.filterGlobal(
            (event.target as HTMLInputElement).value,
            'contains'
        );
    }

    openCreateDialog(): void {
        this.editingProfileId = null;
        this.dialogVisible = true;
    }

    openEditDialog(profile: CompanyProfileDto): void {
        this.editingProfileId = profile.id;
        this.dialogVisible = true;
    }

    openReceiptPreview(profile: CompanyProfileDto): void {
        this.previewProfile = profile;
        this.receiptPreviewVisible = true;
    }

    onDialogSaved(): void {
        this.loadProfiles();
    }

    onDelete(profile: CompanyProfileDto): void {
        this.confirmationService.confirm({
            message: `Are you sure you want to delete company profile "${profile.name}"?`,
            header: 'Delete Confirmation',
            icon: 'pi pi-exclamation-triangle',
            acceptButtonStyleClass: 'p-button-danger',
            accept: () => {
                this.companyProfileService
                    .delete(profile.id)
                    .then(() => {
                        this.messageService.add({
                            severity: 'success',
                            summary: 'Success',
                            detail: 'Company profile deleted successfully',
                        });
                        this.loadProfiles();
                    })
                    .catch((error) => {
                        this.messageService.add({
                            severity: 'error',
                            summary: 'Error',
                            detail:
                                error?.message ||
                                'Failed to delete company profile',
                        });
                    });
            },
        });
    }
}
