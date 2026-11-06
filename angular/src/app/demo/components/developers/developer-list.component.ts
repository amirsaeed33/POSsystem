import { Component, OnInit } from '@angular/core';
import { ConfirmationService, MessageService } from 'primeng/api';
import { DeveloperDto } from 'src/app/demo/api/developer';
import { PermissionNames } from 'src/app/demo/api/permission-names';
import { DeveloperService } from 'src/app/demo/service/developer.service';
import { PermissionService } from 'src/app/demo/service/permission.service';
import { RowActionItem } from 'src/app/shared/components/row-actions/row-actions.component';

@Component({
    selector: 'app-developer-list',
    templateUrl: './developer-list.component.html',
    styleUrls: ['./developer-list.component.scss'],
    providers: [MessageService, ConfirmationService],
})
export class DeveloperListComponent implements OnInit {
    developers: DeveloperDto[] = [];
    loading = false;
    keyword = '';

    dialogVisible = false;
    saving = false;
    editing: DeveloperDto | null = null;

    form: DeveloperDto = this.emptyForm();
    profilePreview: string | null = null;
    backgroundPreview: string | null = null;

    constructor(
        private developerService: DeveloperService,
        private messageService: MessageService,
        private confirmationService: ConfirmationService,
        private permissionService: PermissionService
    ) {}

    get canCreate(): boolean {
        return this.permissionService.isGranted(PermissionNames.DevelopersCreate);
    }

    get canEdit(): boolean {
        return this.permissionService.isGranted(PermissionNames.DevelopersEdit);
    }

    get canDelete(): boolean {
        return this.permissionService.isGranted(PermissionNames.DevelopersDelete);
    }

    ngOnInit(): void {
        void this.load();
    }

    async load(): Promise<void> {
        this.loading = true;
        try {
            const result = await this.developerService.getAll({
                keyword: this.keyword?.trim() || undefined,
                maxResultCount: 200,
            });
            this.developers = result.items || [];
        } catch (err: any) {
            this.messageService.add({
                severity: 'error',
                summary: 'Error',
                detail: err?.message || 'Failed to load developers',
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
        this.profilePreview = null;
        this.backgroundPreview = null;
        this.dialogVisible = true;
    }

    openEdit(dev: DeveloperDto): void {
        this.editing = { ...dev };
        this.form = {
            ...dev,
            profileImageBase64: null,
            backgroundImageBase64: null,
            clearProfileImage: false,
            clearBackgroundImage: false,
        };
        this.profilePreview = this.developerService.getImageUrl(dev.profileImagePath) || null;
        this.backgroundPreview =
            this.developerService.getImageUrl(dev.backgroundImagePath) || null;
        this.dialogVisible = true;
    }

    getRowActions(dev: DeveloperDto): RowActionItem[] {
        return [
            {
                label: 'Edit',
                icon: 'pi pi-pencil',
                visible: this.canEdit,
                action: () => this.openEdit(dev),
            },
            {
                label: 'Delete',
                icon: 'pi pi-trash',
                danger: true,
                visible: this.canDelete,
                action: () => this.confirmDelete(dev),
            },
        ];
    }

    tagList(dev: DeveloperDto): string[] {
        return (dev.tags || '')
            .split(',')
            .map((t) => t.trim())
            .filter((t) => !!t);
    }

    educationLines(dev: DeveloperDto): string[] {
        return (dev.education || '')
            .split(/\r?\n/)
            .map((l) => l.trim())
            .filter((l) => !!l);
    }

    initials(dev: DeveloperDto): string {
        const parts = (dev.name || '')
            .trim()
            .split(/\s+/)
            .filter(Boolean);
        if (!parts.length) return 'DV';
        if (parts.length === 1) return parts[0].slice(0, 2).toUpperCase();
        return (parts[0][0] + parts[1][0]).toUpperCase();
    }

    coverUrl(dev: DeveloperDto): string {
        return this.developerService.getImageUrl(dev.backgroundImagePath);
    }

    profileUrl(dev: DeveloperDto): string {
        return this.developerService.getImageUrl(dev.profileImagePath);
    }

    onProfileSelected(event: Event): void {
        const input = event.target as HTMLInputElement;
        const file = input.files?.[0];
        if (!file) return;
        const reader = new FileReader();
        reader.onload = () => {
            const dataUrl = String(reader.result || '');
            this.profilePreview = dataUrl;
            this.form.profileImageBase64 = dataUrl;
            this.form.clearProfileImage = false;
        };
        reader.readAsDataURL(file);
        input.value = '';
    }

    onBackgroundSelected(event: Event): void {
        const input = event.target as HTMLInputElement;
        const file = input.files?.[0];
        if (!file) return;
        const reader = new FileReader();
        reader.onload = () => {
            const dataUrl = String(reader.result || '');
            this.backgroundPreview = dataUrl;
            this.form.backgroundImageBase64 = dataUrl;
            this.form.clearBackgroundImage = false;
        };
        reader.readAsDataURL(file);
        input.value = '';
    }

    clearProfile(): void {
        this.profilePreview = null;
        this.form.profileImageBase64 = null;
        this.form.clearProfileImage = true;
        this.form.profileImagePath = undefined;
    }

    clearBackground(): void {
        this.backgroundPreview = null;
        this.form.backgroundImageBase64 = null;
        this.form.clearBackgroundImage = true;
        this.form.backgroundImagePath = undefined;
    }

    closeDialog(): void {
        if (this.saving) return;
        this.dialogVisible = false;
    }

    async save(): Promise<void> {
        const name = (this.form.name || '').trim();
        if (!name) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Validation',
                detail: 'Name is required.',
            });
            return;
        }

        this.saving = true;
        try {
            if (this.editing?.id) {
                await this.developerService.update({
                    ...this.form,
                    id: this.editing.id,
                    name,
                });
                this.messageService.add({
                    severity: 'success',
                    summary: 'Updated',
                    detail: 'Developer updated.',
                });
            } else {
                await this.developerService.create({
                    name,
                    email: this.form.email,
                    phoneNumber: this.form.phoneNumber,
                    description: this.form.description,
                    education: this.form.education,
                    tags: this.form.tags,
                    isActive: this.form.isActive !== false,
                    sortOrder: this.form.sortOrder || 0,
                    profileImageBase64: this.form.profileImageBase64,
                    backgroundImageBase64: this.form.backgroundImageBase64,
                });
                this.messageService.add({
                    severity: 'success',
                    summary: 'Created',
                    detail: 'Developer created.',
                });
            }
            this.dialogVisible = false;
            await this.load();
        } catch (err: any) {
            this.messageService.add({
                severity: 'error',
                summary: 'Save failed',
                detail: err?.message || 'Could not save developer',
            });
        } finally {
            this.saving = false;
        }
    }

    confirmDelete(dev: DeveloperDto): void {
        this.confirmationService.confirm({
            message: `Delete developer "${dev.name}"?`,
            header: 'Confirm',
            icon: 'pi pi-exclamation-triangle',
            acceptButtonStyleClass: 'p-button-danger',
            accept: () => void this.delete(dev),
        });
    }

    private async delete(dev: DeveloperDto): Promise<void> {
        if (!dev.id) return;
        try {
            await this.developerService.delete(dev.id);
            this.messageService.add({
                severity: 'success',
                summary: 'Deleted',
                detail: 'Developer removed.',
            });
            await this.load();
        } catch (err: any) {
            this.messageService.add({
                severity: 'error',
                summary: 'Delete failed',
                detail: err?.message || 'Could not delete developer',
            });
        }
    }

    private emptyForm(): DeveloperDto {
        return {
            name: '',
            email: '',
            phoneNumber: '',
            description: '',
            education: '',
            tags: '',
            isActive: true,
            sortOrder: 0,
            profileImageBase64: null,
            backgroundImageBase64: null,
            clearProfileImage: false,
            clearBackgroundImage: false,
        };
    }
}
