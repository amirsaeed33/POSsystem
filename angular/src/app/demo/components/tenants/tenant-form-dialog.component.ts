import {
    Component,
    EventEmitter,
    Input,
    OnChanges,
    Output,
    SimpleChanges,
} from '@angular/core';
import { MessageService } from 'primeng/api';
import { CreateTenantDto, TenantDto } from 'src/app/demo/api/tenant';
import { TenantService } from 'src/app/demo/service/tenant.service';

@Component({
    selector: 'app-tenant-form-dialog',
    templateUrl: './tenant-form-dialog.component.html',
})
export class TenantFormDialogComponent implements OnChanges {
    @Input() visible = false;
    @Input() tenantId: number | null = null;
    @Output() visibleChange = new EventEmitter<boolean>();
    @Output() saved = new EventEmitter<void>();

    tenant: CreateTenantDto = this.emptyTenant();
    saving = false;
    loading = false;

    constructor(
        private tenantService: TenantService,
        private messageService: MessageService
    ) {}

    get dialogTitle(): string {
        return this.tenantId ? 'Edit Tenant' : 'Create Tenant';
    }

    get isEdit(): boolean {
        return !!this.tenantId;
    }

    ngOnChanges(changes: SimpleChanges): void {
        if (changes['visible'] && this.visible) {
            this.resetForm();
            if (this.tenantId) {
                this.loadTenant(this.tenantId);
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

    save(): void {
        const tenancyName = (this.tenant.tenancyName || '').trim();
        const name = (this.tenant.name || '').trim();
        const adminEmailAddress = (
            this.tenant.adminEmailAddress || ''
        ).trim();

        if (!tenancyName) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Validation',
                detail: 'Tenancy name is required',
            });
            return;
        }
        if (!name) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Validation',
                detail: 'Name is required',
            });
            return;
        }
        if (!this.isEdit && !adminEmailAddress) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Validation',
                detail: 'Admin email address is required',
            });
            return;
        }

        this.saving = true;

        const request = this.tenantId
            ? this.tenantService.update({
                  id: this.tenantId,
                  tenancyName,
                  name,
                  isActive: this.tenant.isActive !== false,
              } as TenantDto)
            : this.tenantService.create({
                  tenancyName,
                  name,
                  adminEmailAddress,
                  connectionString:
                      this.tenant.connectionString?.trim() || undefined,
                  isActive: this.tenant.isActive !== false,
              });

        request
            .then(() => {
                this.messageService.add({
                    severity: 'success',
                    summary: 'Success',
                    detail: this.tenantId
                        ? 'Tenant updated successfully'
                        : 'Tenant created successfully',
                });
                this.saved.emit();
                this.onHide();
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to save tenant',
                });
            })
            .finally(() => {
                this.saving = false;
            });
    }

    private emptyTenant(): CreateTenantDto {
        return {
            tenancyName: '',
            name: '',
            adminEmailAddress: '',
            connectionString: '',
            isActive: true,
        };
    }

    private resetForm(): void {
        this.tenant = this.emptyTenant();
        this.saving = false;
        this.loading = false;
    }

    private loadTenant(id: number): void {
        this.loading = true;
        this.tenantService
            .get(id)
            .then((tenant) => {
                this.tenant = {
                    tenancyName: tenant.tenancyName,
                    name: tenant.name,
                    adminEmailAddress: '',
                    connectionString: '',
                    isActive: tenant.isActive,
                };
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to load tenant',
                });
                this.onHide();
            })
            .finally(() => {
                this.loading = false;
            });
    }
}
