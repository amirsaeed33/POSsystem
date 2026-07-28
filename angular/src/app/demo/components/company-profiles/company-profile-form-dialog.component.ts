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
    CompanyProfileDto,
    CreateCompanyProfileDto,
} from 'src/app/demo/api/company-profile';
import { CompanyProfileService } from 'src/app/demo/service/company-profile.service';

@Component({
    selector: 'app-company-profile-form-dialog',
    templateUrl: './company-profile-form-dialog.component.html',
})
export class CompanyProfileFormDialogComponent implements OnChanges {
    @Input() visible = false;
    @Input() profileId: number | null = null;
    @Output() visibleChange = new EventEmitter<boolean>();
    @Output() saved = new EventEmitter<void>();

    profile: CompanyProfileDto = this.emptyProfile();
    imagePreview = '';
    saving = false;
    loading = false;
    receiptPreviewVisible = false;

    constructor(
        private companyProfileService: CompanyProfileService,
        private messageService: MessageService
    ) {}

    get dialogTitle(): string {
        return this.profileId
            ? 'Edit Company Profile'
            : 'Create Company Profile';
    }

    openReceiptPreview(): void {
        if (!(this.profile.name || '').trim()) {
            return;
        }
        this.receiptPreviewVisible = true;
    }

    ngOnChanges(changes: SimpleChanges): void {
        if (changes['visible'] && this.visible) {
            this.resetForm();
            if (this.profileId) {
                this.loadProfile(this.profileId);
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
            this.profile.imageBase64 = result;
        };
        reader.readAsDataURL(file);
    }

    save(): void {
        const name = (this.profile.name || '').trim();
        if (!name) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Validation',
                detail: 'Name is required',
            });
            return;
        }

        this.saving = true;
        const imageBase64 = this.profile.imageBase64?.startsWith('data:image')
            ? this.profile.imageBase64
            : undefined;

        const createPayload: CreateCompanyProfileDto = {
            name,
            imageBase64,
            invoiceAddress: this.profile.invoiceAddress?.trim() || undefined,
            invoiceContactEmail:
                this.profile.invoiceContactEmail?.trim() || undefined,
            invoiceContactPhone:
                this.profile.invoiceContactPhone?.trim() || undefined,
            taxNumber: this.profile.taxNumber?.trim() || undefined,
            website: this.profile.website?.trim() || undefined,
            invoiceFooter: this.profile.invoiceFooter?.trim() || undefined,
        };

        const request = this.profileId
            ? this.companyProfileService.update({
                  id: this.profileId,
                  ...createPayload,
                  imagePath: this.profile.imagePath,
              })
            : this.companyProfileService.create(createPayload);

        request
            .then(() => {
                this.messageService.add({
                    severity: 'success',
                    summary: 'Success',
                    detail: this.profileId
                        ? 'Company profile updated successfully'
                        : 'Company profile created successfully',
                });
                this.saved.emit();
                this.onHide();
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail:
                        error?.message || 'Failed to save company profile',
                });
            })
            .finally(() => {
                this.saving = false;
            });
    }

    private emptyProfile(): CompanyProfileDto {
        return {
            id: 0,
            name: '',
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
        this.profile = this.emptyProfile();
        this.imagePreview = '';
        this.saving = false;
        this.loading = false;
    }

    private loadProfile(id: number): void {
        this.loading = true;
        this.companyProfileService
            .get(id)
            .then((profile) => {
                this.profile = { ...profile, imageBase64: undefined };
                this.imagePreview =
                    this.companyProfileService.getImageUrl(profile.imagePath);
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail:
                        error?.message || 'Failed to load company profile',
                });
                this.onHide();
            })
            .finally(() => {
                this.loading = false;
            });
    }
}
