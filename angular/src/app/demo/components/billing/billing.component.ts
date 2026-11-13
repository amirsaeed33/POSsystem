import { Component, OnInit } from '@angular/core';
import { MessageService } from 'primeng/api';
import {
    AssignablePlanDto,
    MySubscriptionDto,
    PaymentMethodDto,
    SubscriptionService,
    TenantSubscriptionDto,
} from 'src/app/demo/service/subscription.service';

@Component({
    selector: 'app-billing',
    templateUrl: './billing.component.html',
    styleUrls: ['./billing.component.scss'],
    providers: [MessageService],
})
export class BillingComponent implements OnInit {
    loading = false;
    submitting = false;
    data: MySubscriptionDto | null = null;
    payVisible = false;
    selectedMethod: PaymentMethodDto | null = null;
    selectedPackageId: number | null = null;
    selectedCycle = 0;
    selectedAmount = 0;
    cycleOptions: { label: string; value: number }[] = [];
    receiptBase64: string | null = null;
    receiptContentType: string | null = null;
    receiptFileName = '';
    receiptPreview: string | null = null;
    showInvoices = false;

    private readonly allCycles = [
        { label: 'Monthly', value: 0 },
        { label: 'Yearly', value: 1 },
    ];
    private readonly emptyPlans: AssignablePlanDto[] = [];

    constructor(
        private billing: SubscriptionService,
        private messageService: MessageService
    ) {}

    ngOnInit(): void {
        void this.load();
    }

    get subscription(): TenantSubscriptionDto | null {
        return this.data?.subscription || null;
    }

    get availablePlans(): AssignablePlanDto[] {
        return this.data?.availablePlans?.length ? this.data.availablePlans : this.emptyPlans;
    }

    get canChangePlan(): boolean {
        return !this.subscription?.hasPendingPayment;
    }

    get selectedPlan(): AssignablePlanDto | null {
        if (this.selectedPackageId == null) {
            return null;
        }
        return this.availablePlans.find((p) => p.id === this.selectedPackageId) || null;
    }

    async load(): Promise<void> {
        this.loading = true;
        try {
            this.data = await this.billing.getMine();
        } catch (err: any) {
            this.messageService.add({
                severity: 'error',
                summary: 'Billing',
                detail: err?.message || 'Failed to load billing',
            });
        } finally {
            this.loading = false;
        }
    }

    openPay(): void {
        const sub = this.subscription;
        const plans = this.availablePlans;
        const preferred =
            plans.find((p) => sub?.packageId != null && p.id === sub.packageId) ||
            plans.find((p) => !!sub?.planName && p.name === sub.planName) ||
            plans[0] ||
            null;

        this.selectedPackageId = preferred ? preferred.id : sub?.packageId ?? null;
        this.selectedCycle = this.pickCycle(preferred, sub?.cycle ?? 0);
        this.refreshCycleOptions();
        this.refreshSelectedAmount();
        if (this.selectedAmount <= 0 && sub?.amount) {
            this.selectedCycle = sub.cycle ?? 0;
            this.refreshCycleOptions();
            this.refreshSelectedAmount();
        }

        this.selectedMethod = this.data?.paymentMethods?.[0] || null;
        this.receiptBase64 = null;
        this.receiptContentType = null;
        this.receiptFileName = '';
        this.receiptPreview = null;
        this.payVisible = true;
    }

    onPlanChange(): void {
        this.selectedCycle = this.pickCycle(this.selectedPlan, this.selectedCycle);
        this.refreshCycleOptions();
        this.refreshSelectedAmount();
    }

    onCycleChange(): void {
        this.refreshSelectedAmount();
    }

    selectMethod(method: PaymentMethodDto): void {
        this.selectedMethod = method;
    }

    async onReceiptSelected(event: Event): Promise<void> {
        const file = (event.target as HTMLInputElement).files?.[0];
        if (!file) {
            return;
        }
        if (!/^image\/(png|jpeg|jpg|webp)$/i.test(file.type)) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Receipt',
                detail: 'Choose a PNG, JPG, or WEBP image.',
            });
            return;
        }
        if (file.size > 2 * 1024 * 1024) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Receipt',
                detail: 'Receipt must be under 2 MB.',
            });
            return;
        }
        const image = await new Promise<{ base64: string; contentType: string }>((resolve, reject) => {
            const reader = new FileReader();
            reader.onload = () => {
                const result = String(reader.result || '');
                resolve({
                    base64: result.includes(',') ? result.split(',')[1] : result,
                    contentType: file.type === 'image/jpg' ? 'image/jpeg' : file.type,
                });
            };
            reader.onerror = () => reject(new Error('Could not read the receipt.'));
            reader.readAsDataURL(file);
        });
        this.receiptBase64 = image.base64;
        this.receiptContentType = image.contentType;
        this.receiptFileName = file.name;
        this.receiptPreview = `data:${image.contentType};base64,${image.base64}`;
    }

    async submit(): Promise<void> {
        const sub = this.subscription;
        const canRenewCurrent =
            !!sub?.planName &&
            (sub.amount || 0) > 0 &&
            (this.selectedPackageId == null ||
                this.selectedPackageId === 0 ||
                this.selectedPackageId === sub.packageId ||
                this.selectedPlan?.name === sub.planName);

        if (this.selectedPackageId == null && !canRenewCurrent) {
            this.messageService.add({ severity: 'warn', summary: 'Payment', detail: 'Choose a plan.' });
            return;
        }
        if (this.selectedAmount <= 0) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Payment',
                detail: 'That billing cycle is not available for the selected plan.',
            });
            return;
        }
        if (!this.selectedMethod) {
            this.messageService.add({ severity: 'warn', summary: 'Payment', detail: 'Choose how you paid.' });
            return;
        }
        if (!this.receiptBase64 || !this.receiptContentType) {
            this.messageService.add({ severity: 'warn', summary: 'Payment', detail: 'Upload the payment receipt.' });
            return;
        }

        this.submitting = true;
        try {
            await this.billing.submitPayment({
                packageId: this.selectedPackageId ?? 0,
                cycle: this.selectedCycle,
                paymentMethodId: this.selectedMethod.id,
                receiptBase64: this.receiptBase64,
                receiptContentType: this.receiptContentType,
                receiptFileName: this.receiptFileName,
            });
            this.payVisible = false;
            this.messageService.add({
                severity: 'success',
                summary: 'Submitted',
                detail: 'Your receipt is waiting for approval.',
            });
            await this.load();
        } catch (err: any) {
            this.messageService.add({
                severity: 'error',
                summary: 'Payment',
                detail: err?.message || 'Failed to submit the receipt',
            });
        } finally {
            this.submitting = false;
        }
    }

    money(amount?: number): string {
        return 'PKR ' + (amount || 0).toLocaleString('en-PK', {
            minimumFractionDigits: 2,
            maximumFractionDigits: 2,
        });
    }

    qrSrc(method: PaymentMethodDto | null): string | null {
        if (!method?.qrBase64) {
            return null;
        }
        return `data:${method.qrContentType || 'image/png'};base64,${method.qrBase64}`;
    }

    statusClass(status?: string): string {
        if (status === 'Paid') {
            return 'billing-status billing-status--paid';
        }
        if (status === 'Expiring' || status === 'Waiting') {
            return 'billing-status billing-status--wait';
        }
        if (status === 'Expired') {
            return 'billing-status billing-status--expired';
        }
        return 'billing-status';
    }

    statusLabel(sub: TenantSubscriptionDto | null): string {
        if (!sub || sub.status === 'Unassigned') {
            return 'No plan';
        }
        if (sub.status === 'Waiting') {
            return 'Waiting for approval';
        }
        return sub.status;
    }

    payButtonLabel(sub: TenantSubscriptionDto | null): string {
        if (!sub?.planName || sub.status === 'Expired' || sub.status === 'Unassigned') {
            return 'Purchase';
        }
        if (sub.canPay) {
            return 'Renew';
        }
        return 'Buy more';
    }

    private refreshCycleOptions(): void {
        // Always offer Monthly and Yearly when the client can purchase.
        this.cycleOptions = this.allCycles;
    }

    private refreshSelectedAmount(): void {
        const plan = this.selectedPlan;
        if (plan) {
            const fromPlan = this.amountForCycle(plan.monthlyAmount, plan.yearlyAmount, this.selectedCycle);
            if (fromPlan > 0) {
                this.selectedAmount = fromPlan;
                return;
            }
        }

        const sub = this.subscription;
        const amount = sub?.amount ?? 0;
        if (
            sub &&
            amount > 0 &&
            (this.selectedPackageId == null ||
                this.selectedPackageId === sub.packageId ||
                plan?.name === sub.planName)
        ) {
            this.selectedAmount = this.amountFromSubscription(sub.cycle ?? 0, amount, this.selectedCycle);
            return;
        }

        this.selectedAmount = 0;
    }

    /** Prefer explicit cycle price; otherwise yearly = monthly x 12 (and the reverse). */
    private amountForCycle(monthly: number, yearly: number, cycle: number): number {
        if (cycle === 1) {
            if (yearly > 0) {
                return yearly;
            }
            return monthly > 0 ? monthly * 12 : 0;
        }
        if (monthly > 0) {
            return monthly;
        }
        return yearly > 0 ? Math.round((yearly / 12) * 100) / 100 : 0;
    }

    private amountFromSubscription(subCycle: number, amount: number, cycle: number): number {
        if (amount <= 0) {
            return 0;
        }
        if (subCycle === cycle) {
            return amount;
        }
        if (cycle === 1 && subCycle === 0) {
            return amount * 12;
        }
        if (cycle === 0 && subCycle === 1) {
            return Math.round((amount / 12) * 100) / 100;
        }
        return amount;
    }

    private pickCycle(plan: AssignablePlanDto | null | undefined, preferred: number): number {
        if (!plan) {
            return preferred;
        }
        if (preferred === 1 && plan.yearlyAmount > 0) {
            return 1;
        }
        if (preferred === 0 && plan.monthlyAmount > 0) {
            return 0;
        }
        if (plan.monthlyAmount > 0) {
            return 0;
        }
        if (plan.yearlyAmount > 0) {
            return 1;
        }
        return preferred;
    }
}
