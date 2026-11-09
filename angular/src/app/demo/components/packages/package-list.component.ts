import { Component, OnInit } from '@angular/core';
import { ConfirmationService, MessageService } from 'primeng/api';
import { PackageDto } from 'src/app/demo/api/package';
import { PermissionNames } from 'src/app/demo/api/permission-names';
import { PackageService } from 'src/app/demo/service/package.service';
import { PermissionService } from 'src/app/demo/service/permission.service';
import { RowActionItem } from 'src/app/shared/components/row-actions/row-actions.component';

@Component({
    selector: 'app-package-list',
    templateUrl: './package-list.component.html',
    styleUrls: ['./package-list.component.scss'],
    providers: [MessageService, ConfirmationService],
})
export class PackageListComponent implements OnInit {
    packages: PackageDto[] = [];
    loading = false;
    keyword = '';

    dialogVisible = false;
    saving = false;
    editing: PackageDto | null = null;
    form: PackageDto = this.emptyForm();

    constructor(
        private packageService: PackageService,
        private messageService: MessageService,
        private confirmationService: ConfirmationService,
        private permissionService: PermissionService
    ) {}

    get canCreate(): boolean {
        return this.permissionService.isGranted(PermissionNames.PackagesCreate);
    }

    get canEdit(): boolean {
        return this.permissionService.isGranted(PermissionNames.PackagesEdit);
    }

    get canDelete(): boolean {
        return this.permissionService.isGranted(PermissionNames.PackagesDelete);
    }

    ngOnInit(): void {
        void this.load();
    }

    async load(): Promise<void> {
        this.loading = true;
        try {
            const result = await this.packageService.getAll({
                keyword: this.keyword?.trim() || undefined,
                maxResultCount: 200,
            });
            this.packages = result.items || [];
        } catch (err: any) {
            this.messageService.add({
                severity: 'error',
                summary: 'Error',
                detail: err?.message || 'Failed to load packages',
            });
        } finally {
            this.loading = false;
        }
    }

    onSearch(): void {
        void this.load();
    }

    openCreate(): void {
        this.editing = null;
        this.form = this.emptyForm();
        this.dialogVisible = true;
    }

    openEdit(pkg: PackageDto): void {
        this.editing = { ...pkg };
        this.form = { ...pkg };
        this.dialogVisible = true;
    }

    closeDialog(): void {
        this.dialogVisible = false;
        this.editing = null;
        this.form = this.emptyForm();
    }

    getRowActions(pkg: PackageDto): RowActionItem[] {
        return [
            {
                label: 'Edit',
                icon: 'pi pi-pencil',
                visible: this.canEdit,
                action: () => this.openEdit(pkg),
            },
            {
                label: 'Delete',
                icon: 'pi pi-trash',
                danger: true,
                visible: this.canDelete,
                action: () => this.confirmDelete(pkg),
            },
        ];
    }

    featureLines(pkg: PackageDto): string[] {
        return (pkg.features || '')
            .split(/\r?\n/)
            .map((l) => l.trim())
            .filter(Boolean);
    }

    async save(): Promise<void> {
        if (!this.form.name?.trim() || !this.form.price?.trim()) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Validation',
                detail: 'Name and price are required.',
            });
            return;
        }

        this.saving = true;
        try {
            const payload: PackageDto = {
                ...this.form,
                name: this.form.name.trim(),
                price: this.form.price.trim(),
                period: this.form.period?.trim() || undefined,
                blurb: this.form.blurb?.trim() || undefined,
                features: this.form.features?.trim() || undefined,
                ctaLabel: this.form.ctaLabel?.trim() || 'Get started',
                sortOrder: this.form.sortOrder ?? 0,
            };

            if (this.editing?.id) {
                await this.packageService.update({ ...payload, id: this.editing.id });
                this.messageService.add({
                    severity: 'success',
                    summary: 'Saved',
                    detail: 'Package updated.',
                });
            } else {
                await this.packageService.create(payload);
                this.messageService.add({
                    severity: 'success',
                    summary: 'Created',
                    detail: 'Package created.',
                });
            }

            this.closeDialog();
            await this.load();
        } catch (err: any) {
            this.messageService.add({
                severity: 'error',
                summary: 'Error',
                detail: err?.message || 'Failed to save package',
            });
        } finally {
            this.saving = false;
        }
    }

    confirmDelete(pkg: PackageDto): void {
        this.confirmationService.confirm({
            message: `Delete package "${pkg.name}"?`,
            header: 'Confirm',
            icon: 'pi pi-exclamation-triangle',
            accept: () => void this.delete(pkg),
        });
    }

    private async delete(pkg: PackageDto): Promise<void> {
        if (!pkg.id) return;
        try {
            await this.packageService.delete(pkg.id);
            this.messageService.add({
                severity: 'success',
                summary: 'Deleted',
                detail: 'Package deleted.',
            });
            await this.load();
        } catch (err: any) {
            this.messageService.add({
                severity: 'error',
                summary: 'Error',
                detail: err?.message || 'Failed to delete package',
            });
        }
    }

    private emptyForm(): PackageDto {
        return {
            name: '',
            price: '',
            period: '/ month',
            blurb: '',
            features: '',
            ctaLabel: 'Get started',
            isFeatured: false,
            contactViaWhatsApp: false,
            isActive: true,
            sortOrder: 0,
        };
    }
}
