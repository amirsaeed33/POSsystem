import { Component, OnInit } from '@angular/core';
import { ConfirmationService, MessageService } from 'primeng/api';
import {
    HostCatalogItemDto,
    HostCatalogItemType,
    HostCatalogItemTypes,
} from 'src/app/demo/api/host-catalog';
import { HostCatalogService } from 'src/app/demo/service/host-catalog.service';

@Component({
    templateUrl: './host-catalog-list.component.html',
    providers: [MessageService, ConfirmationService],
})
export class HostCatalogListComponent implements OnInit {
    companyTypes: HostCatalogItemDto[] = [];
    children: HostCatalogItemDto[] = [];
    selectedCompanyType: HostCatalogItemDto | null = null;
    activeChildType: HostCatalogItemType = HostCatalogItemTypes.Category;
    loading = false;
    childrenLoading = false;

    dialogVisible = false;
    editingId: number | null = null;
    formType: HostCatalogItemType = HostCatalogItemTypes.CompanyType;
    formName = '';
    formSymbol = '';
    formIsActive = true;
    saving = false;

    readonly HostCatalogItemTypes = HostCatalogItemTypes;
    readonly childTabs = [
        { label: 'Categories', value: HostCatalogItemTypes.Category },
        { label: 'Units', value: HostCatalogItemTypes.Unit },
        { label: 'Brands', value: HostCatalogItemTypes.Brand },
    ];

    constructor(
        private hostCatalogService: HostCatalogService,
        private messageService: MessageService,
        private confirmationService: ConfirmationService
    ) {}

    ngOnInit(): void {
        this.loadCompanyTypes();
    }

    loadCompanyTypes(): void {
        this.loading = true;
        this.hostCatalogService
            .getAll({
                type: HostCatalogItemTypes.CompanyType,
                skipCount: 0,
                maxResultCount: 1000,
            })
            .then((result) => {
                this.companyTypes = result.items;
                if (
                    this.selectedCompanyType &&
                    !this.companyTypes.find(
                        (x) => x.id === this.selectedCompanyType!.id
                    )
                ) {
                    this.selectedCompanyType = null;
                    this.children = [];
                }
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to load company types',
                });
            })
            .finally(() => {
                this.loading = false;
            });
    }

    selectCompanyType(item: HostCatalogItemDto): void {
        this.selectedCompanyType = item;
        this.loadChildren();
    }

    onChildTabChange(type: HostCatalogItemType): void {
        this.activeChildType = type;
        this.loadChildren();
    }

    loadChildren(): void {
        if (!this.selectedCompanyType) {
            this.children = [];
            return;
        }

        this.childrenLoading = true;
        this.hostCatalogService
            .getAll({
                type: this.activeChildType,
                companyTypeId: this.selectedCompanyType.id,
                skipCount: 0,
                maxResultCount: 1000,
            })
            .then((result) => {
                this.children = result.items;
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to load items',
                });
            })
            .finally(() => {
                this.childrenLoading = false;
            });
    }

    openCreateCompanyType(): void {
        this.editingId = null;
        this.formType = HostCatalogItemTypes.CompanyType;
        this.formName = '';
        this.formSymbol = '';
        this.formIsActive = true;
        this.dialogVisible = true;
    }

    openCreateChild(): void {
        if (!this.selectedCompanyType) {
            return;
        }
        this.editingId = null;
        this.formType = this.activeChildType;
        this.formName = '';
        this.formSymbol = '';
        this.formIsActive = true;
        this.dialogVisible = true;
    }

    openEdit(item: HostCatalogItemDto): void {
        this.editingId = item.id;
        this.formType = (item.type || HostCatalogItemTypes.CompanyType) as HostCatalogItemType;
        this.formName = item.name;
        this.formSymbol = item.symbol || '';
        this.formIsActive = item.isActive;
        this.dialogVisible = true;
    }

    save(): void {
        const name = this.formName.trim();
        if (!name) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Validation',
                detail: 'Name is required',
            });
            return;
        }

        this.saving = true;
        const payload = {
            id: this.editingId || 0,
            type: this.formType,
            companyTypeId:
                this.formType === HostCatalogItemTypes.CompanyType
                    ? null
                    : this.selectedCompanyType?.id,
            name,
            symbol:
                this.formType === HostCatalogItemTypes.Unit
                    ? this.formSymbol.trim() || undefined
                    : undefined,
            isActive: this.formIsActive,
        };

        const request = this.editingId
            ? this.hostCatalogService.update(payload)
            : this.hostCatalogService.create(payload);

        request
            .then(() => {
                this.messageService.add({
                    severity: 'success',
                    summary: 'Success',
                    detail: this.editingId ? 'Updated' : 'Created',
                });
                this.dialogVisible = false;
                if (this.formType === HostCatalogItemTypes.CompanyType) {
                    this.loadCompanyTypes();
                } else {
                    this.loadChildren();
                }
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Save failed',
                });
            })
            .finally(() => {
                this.saving = false;
            });
    }

    onDelete(item: HostCatalogItemDto): void {
        this.confirmationService.confirm({
            message: `Delete "${item.name}"?`,
            header: 'Delete Confirmation',
            icon: 'pi pi-exclamation-triangle',
            acceptButtonStyleClass: 'p-button-danger',
            accept: () => {
                this.hostCatalogService
                    .delete(item.id)
                    .then(() => {
                        this.messageService.add({
                            severity: 'success',
                            summary: 'Success',
                            detail: 'Deleted',
                        });
                        if (item.type === HostCatalogItemTypes.CompanyType) {
                            if (this.selectedCompanyType?.id === item.id) {
                                this.selectedCompanyType = null;
                                this.children = [];
                            }
                            this.loadCompanyTypes();
                        } else {
                            this.loadChildren();
                        }
                    })
                    .catch((error) => {
                        this.messageService.add({
                            severity: 'error',
                            summary: 'Error',
                            detail: error?.message || 'Delete failed',
                        });
                    });
            },
        });
    }
}
