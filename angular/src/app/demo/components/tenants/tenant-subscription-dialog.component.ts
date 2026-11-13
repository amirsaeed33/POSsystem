import { Component, EventEmitter, Input, Output } from '@angular/core';
import { MessageService } from 'primeng/api';
import {
    AssignablePlanDto,
    SubscriptionService,
} from 'src/app/demo/service/subscription.service';

@Component({
    selector: 'app-tenant-subscription-dialog',
    templateUrl: './tenant-subscription-dialog.component.html',
    styles: [`
        :host ::ng-deep .p-dropdown,
        :host ::ng-deep .p-inputnumber,
        :host ::ng-deep .p-calendar {
            display: flex;
            width: 100%;
        }
        :host ::ng-deep .p-inputnumber-input,
        :host ::ng-deep .p-calendar .p-inputtext {
            width: 100%;
        }
    `],
})
export class TenantSubscriptionDialogComponent {
    @Input() tenantName = '';
    @Output() saved = new EventEmitter<void>();

    visible = false;
    loading = false;
    saving = false;
    tenantId: number | null = null;
    plans: AssignablePlanDto[] = [];
    packageId: number | null = null;
    cycle = 1;
    amount: number | null = null;
    paidUntil: Date | null = null;
    clearPaidUntil = false;

    readonly cycles = [
        { label: 'Monthly', value: 0 },
        { label: 'Yearly', value: 1 },
    ];

    constructor(
        private billing: SubscriptionService,
        private messageService: MessageService
    ) {}

    async open(tenantId: number, name: string): Promise<void> {
        this.tenantId = tenantId;
        this.tenantName = name;
        this.visible = true;
        this.loading = true;
        this.clearPaidUntil = false;
        try {
            const [plans, current] = await Promise.all([
                this.billing.getAssignablePlans(),
                this.billing.getTenantSubscription(tenantId),
            ]);
            this.plans = plans;
            this.packageId = current.packageId ?? plans[0]?.id ?? null;
            this.cycle = current.planName ? current.cycle : 1;
            this.amount = current.planName ? current.amount : null;
            this.paidUntil = current.paidUntil ? new Date(current.paidUntil) : null;
        } catch (err: any) {
            this.messageService.add({
                severity: 'error',
                summary: 'Subscription',
                detail: err?.message || 'Failed to load the plan',
            });
        } finally {
            this.loading = false;
        }
    }

    async save(): Promise<void> {
        if (!this.tenantId || !this.packageId) {
            this.messageService.add({ severity: 'warn', summary: 'Subscription', detail: 'Choose a plan.' });
            return;
        }
        if (this.amount == null || this.amount < 0) {
            this.messageService.add({ severity: 'warn', summary: 'Subscription', detail: 'Enter the PKR amount.' });
            return;
        }
        this.saving = true;
        try {
            await this.billing.saveTenantSubscription({
                tenantId: this.tenantId,
                packageId: this.packageId,
                cycle: this.cycle,
                amount: this.amount,
                paidUntil: this.clearPaidUntil ? null : this.toLocalDate(this.paidUntil),
                clearPaidUntil: this.clearPaidUntil || !this.paidUntil,
            });
            this.visible = false;
            this.messageService.add({
                severity: 'success',
                summary: 'Saved',
                detail: 'Subscription updated.',
            });
            this.saved.emit();
        } catch (err: any) {
            this.messageService.add({
                severity: 'error',
                summary: 'Subscription',
                detail: err?.message || 'Failed to save the plan',
            });
        } finally {
            this.saving = false;
        }
    }

    private toLocalDate(date: Date | null): string | null {
        if (!date) {
            return null;
        }
        const y = date.getFullYear();
        const m = String(date.getMonth() + 1).padStart(2, '0');
        const d = String(date.getDate()).padStart(2, '0');
        return `${y}-${m}-${d}T00:00:00`;
    }
}
