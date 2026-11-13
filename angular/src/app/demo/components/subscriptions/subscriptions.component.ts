import { Component, OnInit } from '@angular/core';
import { ConfirmationService, MessageService } from 'primeng/api';
import {
    PaymentMethodDto,
    SubscriptionPaymentDto,
    SubscriptionService,
} from 'src/app/demo/service/subscription.service';

@Component({
    selector: 'app-subscriptions',
    templateUrl: './subscriptions.component.html',
    providers: [MessageService, ConfirmationService],
})
export class SubscriptionsComponent implements OnInit {
    methods: PaymentMethodDto[] = [];
    pending: SubscriptionPaymentDto[] = [];
    loadingMethods = false;
    loadingPending = false;

    methodDialog = false;
    methodSaving = false;
    editingMethod: PaymentMethodDto | null = null;
    kind = 0;
    accountTitle = '';
    accountNumber = '';
    bankName = '';
    instructions = '';
    isActive = true;
    qrBase64: string | null = null;
    qrContentType: string | null = null;
    clearQr = false;
    qrPreview: string | null = null;

    receiptVisible = false;
    receiptSrc: string | null = null;
    receiptLoading = false;

    rejectVisible = false;
    rejectNote = '';
    rejecting: SubscriptionPaymentDto | null = null;
    reviewing = false;

    readonly kinds = [
        { label: 'Easypaisa', value: 0 },
        { label: 'JazzCash', value: 1 },
        { label: 'Nayapay', value: 2 },
        { label: 'Bank account', value: 3 },
    ];

    constructor(
        private billing: SubscriptionService,
        private messageService: MessageService,
        private confirmationService: ConfirmationService
    ) {}

    ngOnInit(): void {
        void this.loadMethods();
        void this.loadPending();
    }

    async loadMethods(): Promise<void> {
        this.loadingMethods = true;
        try {
            this.methods = await this.billing.getPaymentMethods();
        } catch (err: any) {
            this.toast('error', err?.message || 'Failed to load payment methods');
        } finally {
            this.loadingMethods = false;
        }
    }

    async loadPending(): Promise<void> {
        this.loadingPending = true;
        try {
            this.pending = await this.billing.getPendingPayments();
        } catch (err: any) {
            this.toast('error', err?.message || 'Failed to load the waiting list');
        } finally {
            this.loadingPending = false;
        }
    }

    openCreate(): void {
        this.editingMethod = null;
        this.kind = 0;
        this.accountTitle = '';
        this.accountNumber = '';
        this.bankName = '';
        this.instructions = '';
        this.isActive = true;
        this.qrBase64 = null;
        this.qrContentType = null;
        this.clearQr = false;
        this.qrPreview = null;
        this.methodDialog = true;
    }

    openEdit(row: PaymentMethodDto): void {
        this.editingMethod = row;
        this.kind = row.kind;
        this.accountTitle = row.accountTitle;
        this.accountNumber = row.accountNumber;
        this.bankName = row.bankName || '';
        this.instructions = row.instructions || '';
        this.isActive = row.isActive;
        this.qrBase64 = null;
        this.qrContentType = null;
        this.clearQr = false;
        this.qrPreview = row.qrBase64 ? `data:${row.qrContentType || 'image/png'};base64,${row.qrBase64}` : null;
        this.methodDialog = true;
    }

    async onQrSelected(event: Event): Promise<void> {
        const file = (event.target as HTMLInputElement).files?.[0];
        if (!file) {
            return;
        }
        try {
            const image = await this.readImage(file, 1024 * 1024);
            this.qrBase64 = image.base64;
            this.qrContentType = image.contentType;
            this.clearQr = false;
            this.qrPreview = `data:${image.contentType};base64,${image.base64}`;
        } catch (err: any) {
            this.toast('error', err?.message || 'Could not read the QR image');
        }
    }

    removeQr(): void {
        this.qrBase64 = null;
        this.qrContentType = null;
        this.qrPreview = null;
        this.clearQr = true;
    }

    async saveMethod(): Promise<void> {
        if (!this.accountTitle.trim() || !this.accountNumber.trim()) {
            this.toast('warn', 'Account title and number are required.');
            return;
        }
        this.methodSaving = true;
        try {
            await this.billing.savePaymentMethod({
                id: this.editingMethod?.id,
                kind: this.kind,
                accountTitle: this.accountTitle.trim(),
                accountNumber: this.accountNumber.trim(),
                bankName: this.bankName.trim(),
                instructions: this.instructions.trim(),
                isActive: this.isActive,
                qrBase64: this.qrBase64,
                qrContentType: this.qrContentType,
                clearQr: this.clearQr,
            });
            this.methodDialog = false;
            this.toast('success', 'Payment method saved.');
            await this.loadMethods();
        } catch (err: any) {
            this.toast('error', err?.message || 'Failed to save payment method');
        } finally {
            this.methodSaving = false;
        }
    }

    deleteMethod(row: PaymentMethodDto): void {
        this.confirmationService.confirm({
            message: `Remove the ${row.kindName} account?`,
            header: 'Delete payment method',
            icon: 'pi pi-exclamation-triangle',
            acceptButtonStyleClass: 'p-button-danger',
            accept: () => {
                this.billing.deletePaymentMethod(row.id)
                    .then(() => {
                        this.toast('success', 'Payment method removed.');
                        return this.loadMethods();
                    })
                    .catch((err) => this.toast('error', err?.message || 'Failed to delete'));
            },
        });
    }

    async openReceipt(row: SubscriptionPaymentDto): Promise<void> {
        this.receiptVisible = true;
        this.receiptSrc = null;
        this.receiptLoading = true;
        try {
            const file = await this.billing.getReceipt(row.id);
            this.receiptSrc = `data:${file.contentType};base64,${file.base64}`;
        } catch (err: any) {
            this.receiptVisible = false;
            this.toast('error', err?.message || 'Failed to open the receipt');
        } finally {
            this.receiptLoading = false;
        }
    }

    approve(row: SubscriptionPaymentDto): void {
        this.confirmationService.confirm({
            message: `Approve this ${row.methodName} receipt and extend ${row.tenantName || 'the business'}?`,
            header: 'Approve payment',
            icon: 'pi pi-check',
            accept: () => {
                this.reviewing = true;
                this.billing.approvePayment(row.id)
                    .then(() => {
                        this.toast('success', 'Payment approved. The paid-until date was updated.');
                        return this.loadPending();
                    })
                    .catch((err) => this.toast('error', err?.message || 'Failed to approve'))
                    .finally(() => {
                        this.reviewing = false;
                    });
            },
        });
    }

    openReject(row: SubscriptionPaymentDto): void {
        this.rejecting = row;
        this.rejectNote = '';
        this.rejectVisible = true;
    }

    async confirmReject(): Promise<void> {
        if (!this.rejecting) {
            return;
        }
        if (!this.rejectNote.trim()) {
            this.toast('warn', 'Add a note so the business knows why this was rejected.');
            return;
        }
        this.reviewing = true;
        try {
            await this.billing.rejectPayment(this.rejecting.id, this.rejectNote.trim());
            this.rejectVisible = false;
            this.toast('success', 'Payment rejected. The plan date was left unchanged.');
            await this.loadPending();
        } catch (err: any) {
            this.toast('error', err?.message || 'Failed to reject');
        } finally {
            this.reviewing = false;
        }
    }

    money(amount: number): string {
        return 'PKR ' + (amount || 0).toLocaleString('en-PK', {
            minimumFractionDigits: 2,
            maximumFractionDigits: 2,
        });
    }

    private readImage(file: File, maxBytes: number): Promise<{ base64: string; contentType: string }> {
        if (!/^image\/(png|jpeg|jpg|webp)$/i.test(file.type)) {
            return Promise.reject(new Error('Choose a PNG, JPG, or WEBP image.'));
        }
        if (file.size > maxBytes) {
            return Promise.reject(new Error('Image must be under ' + Math.round(maxBytes / (1024 * 1024)) + ' MB.'));
        }
        return new Promise((resolve, reject) => {
            const reader = new FileReader();
            reader.onload = () => {
                const result = String(reader.result || '');
                const base64 = result.includes(',') ? result.split(',')[1] : result;
                resolve({ base64, contentType: file.type === 'image/jpg' ? 'image/jpeg' : file.type });
            };
            reader.onerror = () => reject(new Error('Could not read the image.'));
            reader.readAsDataURL(file);
        });
    }

    private toast(severity: string, detail: string): void {
        this.messageService.add({ severity, summary: severity === 'success' ? 'Saved' : 'Billing', detail });
    }
}
