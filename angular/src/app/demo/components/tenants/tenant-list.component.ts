import { Component, OnInit, ViewChild } from '@angular/core';
import { Table } from 'primeng/table';
import { ConfirmationService, MessageService } from 'primeng/api';
import { TenantDto } from 'src/app/demo/api/tenant';
import { SubscriptionService, TenantSubscriptionDto } from 'src/app/demo/service/subscription.service';
import { TenantService } from 'src/app/demo/service/tenant.service';
import { RowActionItem } from 'src/app/shared/components/row-actions/row-actions.component';
import { TenantSubscriptionDialogComponent } from './tenant-subscription-dialog.component';

@Component({
    templateUrl: './tenant-list.component.html',
    providers: [MessageService, ConfirmationService],
})
export class TenantListComponent implements OnInit {
    @ViewChild(TenantSubscriptionDialogComponent) subscriptionDialog?: TenantSubscriptionDialogComponent;
    tenants: TenantDto[] = [];
    loading = false;
    totalRecords = 0;
    keyword = '';

    dialogVisible = false;
    editingTenantId: number | null = null;
    subscriptions = new Map<number, TenantSubscriptionDto>();

    getRowActions(tenant: TenantDto): RowActionItem[] {
        return [
            {
                label: 'Subscription',
                icon: 'pi pi-credit-card',
                color: 'green',
                action: () => this.openSubscription(tenant)
            },
            {
                label: 'Edit',
                icon: 'pi pi-pencil',
                color: 'blue',
                action: () => this.openEditDialog(tenant)
            },
            {
                label: 'Delete',
                icon: 'pi pi-trash',
                color: 'red',
                action: () => this.onDelete(tenant)
            }
        ];
    }

    constructor(
        private tenantService: TenantService,
        private billing: SubscriptionService,
        private messageService: MessageService,
        private confirmationService: ConfirmationService
    ) {}

    ngOnInit(): void {
        this.loadTenants();
        void this.loadSubscriptions();
    }

    planLabel(tenant: TenantDto): string {
        const row = this.subscriptions.get(tenant.id);
        if (!row?.planName) {
            return 'No plan';
        }
        const until = row.paidUntil
            ? new Date(row.paidUntil).toLocaleDateString('en-GB')
            : 'unpaid';
        return `${row.planName} · ${until} · ${row.daysLeft}d`;
    }

    openSubscription(tenant: TenantDto): void {
        void this.subscriptionDialog?.open(tenant.id, tenant.name || tenant.tenancyName);
    }

    onSubscriptionSaved(): void {
        void this.loadSubscriptions();
    }

    private async loadSubscriptions(): Promise<void> {
        try {
            const rows = await this.billing.getSubscriptions();
            this.subscriptions = new Map(rows.map((row) => [row.tenantId, row]));
        } catch {
            this.subscriptions = new Map();
        }
    }

    loadTenants(): void {
        this.loading = true;
        this.tenantService
            .getAll({
                keyword: this.keyword?.trim() || undefined,
                skipCount: 0,
                maxResultCount: 1000,
            })
            .then((result) => {
                this.tenants = result.items;
                this.totalRecords = result.totalCount;
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to load tenants',
                });
            })
            .finally(() => {
                this.loading = false;
            });
    }

    onSearch(): void {
        this.loadTenants();
    }

    onGlobalFilter(table: Table, event: Event): void {
        table.filterGlobal(
            (event.target as HTMLInputElement).value,
            'contains'
        );
    }

    openCreateDialog(): void {
        this.editingTenantId = null;
        this.dialogVisible = true;
    }

    openEditDialog(tenant: TenantDto): void {
        this.editingTenantId = tenant.id;
        this.dialogVisible = true;
    }

    onDialogSaved(): void {
        this.loadTenants();
    }

    onDelete(tenant: TenantDto): void {
        this.confirmationService.confirm({
            message: `Are you sure you want to delete tenant "${tenant.tenancyName}"?`,
            header: 'Delete Confirmation',
            icon: 'pi pi-exclamation-triangle',
            acceptButtonStyleClass: 'p-button-danger',
            accept: () => {
                this.tenantService
                    .delete(tenant.id)
                    .then(() => {
                        this.messageService.add({
                            severity: 'success',
                            summary: 'Success',
                            detail: 'Tenant deleted successfully',
                        });
                        this.loadTenants();
                    })
                    .catch((error) => {
                        this.messageService.add({
                            severity: 'error',
                            summary: 'Error',
                            detail:
                                error?.message || 'Failed to delete tenant',
                        });
                    });
            },
        });
    }
}
