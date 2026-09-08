import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges } from '@angular/core';
import { MessageService } from 'primeng/api';
import { PurchaseDto } from 'src/app/demo/api/purchase';
import { BusinessAccountDto } from 'src/app/demo/api/business-account';
import { PurchaseService } from 'src/app/demo/service/purchase.service';
import { BusinessAccountService } from 'src/app/demo/service/business-account.service';

@Component({
    selector: 'app-purchase-pay-dialog',
    templateUrl: './purchase-pay-dialog.component.html',
})
export class PurchasePayDialogComponent implements OnChanges {
    @Input() visible = false;
    @Input() purchase: PurchaseDto | null = null;
    @Output() visibleChange = new EventEmitter<boolean>();
    @Output() saved = new EventEmitter<void>();

    paymentAccounts: BusinessAccountDto[] = [];
    selectedAccountId: number | null = null;
    amountToPay = 0;
    description = '';
    saving = false;
    loading = false;

    constructor(
        private purchaseService: PurchaseService,
        private businessAccountService: BusinessAccountService,
        private messageService: MessageService
    ) {}

    ngOnChanges(changes: SimpleChanges): void {
        if (changes['visible'] && this.visible && this.purchase) {
            this.amountToPay = this.purchase.dueAmount || (this.purchase.totalAmount - (this.purchase.amountPaid || 0));
            this.description = `Payment for Purchase ${this.purchase.invoiceNo || '#' + this.purchase.id}`;
            this.loadPaymentAccounts();
        }
    }

    onVisibleChange(visible: boolean): void {
        this.visible = visible;
        this.visibleChange.emit(visible);
    }

    onHide(): void {
        this.onVisibleChange(false);
    }

    submitPayment(): void {
        if (!this.purchase?.id) return;
        if (!this.selectedAccountId) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Validation',
                detail: 'Please select a payment account',
            });
            return;
        }
        if (this.amountToPay <= 0) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Validation',
                detail: 'Amount to pay must be greater than zero',
            });
            return;
        }

        this.saving = true;
        this.purchaseService
            .payPurchase({
                purchaseId: this.purchase.id,
                paymentAccountId: this.selectedAccountId,
                amount: this.amountToPay,
                description: this.description,
            })
            .then(() => {
                this.messageService.add({
                    severity: 'success',
                    summary: 'Success',
                    detail: 'Payment recorded successfully',
                });
                this.saved.emit();
                this.onHide();
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to record payment',
                });
            })
            .finally(() => {
                this.saving = false;
            });
    }

    private loadPaymentAccounts(): void {
        this.loading = true;
        this.businessAccountService
            .getAll({ skipCount: 0, maxResultCount: 1000 })
            .then((res) => {
                const items = res.items || [];
                const allowedTypes = ['Cash', 'Bank', 'Mobile Wallet'];
                let filtered = items.filter((a) => {
                    if (a.isActive === false) return false;
                    const type = a.accountTypeName || a.accountType || '';
                    return allowedTypes.some((t) => type.toLowerCase().includes(t.toLowerCase())) ||
                           a.code === 'CASH' || a.code === 'BANK';
                });
                if (!filtered.length) {
                    filtered = items.filter((a) => a.isActive !== false);
                }
                this.paymentAccounts = filtered;
                if (this.paymentAccounts.length > 0) {
                    this.selectedAccountId = this.paymentAccounts[0].id;
                }
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to load payment accounts',
                });
            })
            .finally(() => {
                this.loading = false;
            });
    }
}
