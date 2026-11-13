import { Component, OnInit } from '@angular/core';
import { MessageService } from 'primeng/api';
import {
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
    receiptBase64: string | null = null;
    receiptContentType: string | null = null;
    receiptFileName = '';
    receiptPreview: string | null = null;
    showInvoices = false;

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
        this.selectedMethod = this.data?.paymentMethods?.[0] || null;
        this.receiptBase64 = null;
        this.receiptContentType = null;
        this.receiptFileName = '';
        this.receiptPreview = null;
        this.payVisible = true;
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
}
