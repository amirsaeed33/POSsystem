import { Component, Input, OnInit } from '@angular/core';
import { MessageService } from 'primeng/api';
import { TenantAvailabilityState } from 'src/app/demo/api/account';
import { TenantContextService } from 'src/app/demo/service/tenant-context.service';

@Component({
    selector: 'app-tenant-change',
    templateUrl: './tenant-change.component.html',
})
export class TenantChangeComponent implements OnInit {
    @Input() tenancyName = '';
    @Input() name = '';

    dialogVisible = false;
    saving = false;
    editTenancyName = '';

    constructor(
        private tenantContext: TenantContextService,
        private messageService: MessageService
    ) {}

    get isMultiTenancyEnabled(): boolean {
        return this.tenantContext.isMultiTenancyEnabled();
    }

    ngOnInit(): void {
        if (!this.tenancyName) {
            const cached = this.tenantContext.getTenantInfo();
            if (cached) {
                this.tenancyName = cached.tenancyName || '';
                this.name = cached.name || '';
            }
        }
    }

    showChangeModal(): void {
        this.editTenancyName = this.tenancyName || '';
        this.dialogVisible = true;
    }

    onHide(): void {
        this.dialogVisible = false;
        this.saving = false;
    }

    save(): void {
        const value = (this.editTenancyName || '').trim();

        if (!value) {
            this.tenantContext.setTenantId(undefined);
            this.dialogVisible = false;
            location.reload();
            return;
        }

        this.saving = true;
        this.tenantContext
            .isTenantAvailable({ tenancyName: value })
            .then((result) => {
                switch (result.state) {
                    case TenantAvailabilityState.Available:
                        this.tenantContext.setTenantId(result.tenantId);
                        this.tenantContext.setTenantInfo({
                            id: result.tenantId!,
                            tenancyName: value,
                            name: value,
                        });
                        location.reload();
                        return;
                    case TenantAvailabilityState.InActive:
                        this.messageService.add({
                            severity: 'warn',
                            summary: 'Warning',
                            detail: `Tenant "${value}" is not active.`,
                        });
                        break;
                    case TenantAvailabilityState.NotFound:
                        this.messageService.add({
                            severity: 'warn',
                            summary: 'Warning',
                            detail: `There is no tenant defined with name ${value}.`,
                        });
                        break;
                    default:
                        this.messageService.add({
                            severity: 'error',
                            summary: 'Error',
                            detail: 'Unable to change tenant.',
                        });
                        break;
                }
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to change tenant',
                });
            })
            .finally(() => {
                this.saving = false;
            });
    }
}
