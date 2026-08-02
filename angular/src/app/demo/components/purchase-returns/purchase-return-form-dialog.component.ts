import {
    Component,
    EventEmitter,
    Input,
    OnChanges,
    Output,
    SimpleChanges,
} from '@angular/core';
import { MessageService } from 'primeng/api';
import { PurchaseDto } from 'src/app/demo/api/purchase';
import { PurchaseReturnableDto } from 'src/app/demo/api/purchase-return';
import { PurchaseReturnService } from 'src/app/demo/service/purchase-return.service';
import { BranchContextService } from 'src/app/demo/service/branch-context.service';
import { PurchaseService } from 'src/app/demo/service/purchase.service';

export interface PurchaseReturnLineRow {
    purchaseLineId: number;
    productName: string;
    purchasedQuantity: number;
    returnedQuantity: number;
    returnableQuantity: number;
    unitCost: number;
    returnQuantity: number;
}

@Component({
    selector: 'app-purchase-return-form-dialog',
    templateUrl: './purchase-return-form-dialog.component.html',
})
export class PurchaseReturnFormDialogComponent implements OnChanges {
    @Input() visible = false;
    /** When set, skip purchase picker and load this purchase's returnable lines. */
    @Input() purchaseId: number | null = null;
    @Output() visibleChange = new EventEmitter<boolean>();
    @Output() saved = new EventEmitter<void>();

    saving = false;
    loading = false;
    loadingPurchases = false;
    selectedPurchaseId: number | null = null;
    purchases: PurchaseDto[] = [];
    purchaseOptions: { label: string; value: number }[] = [];
    purchaseInfo: PurchaseReturnableDto | null = null;
    lines: PurchaseReturnLineRow[] = [];
    returnDate = '';
    notes = '';

    constructor(
        private purchaseReturnService: PurchaseReturnService,
        private purchaseService: PurchaseService,
        private branchContext: BranchContextService,
        private messageService: MessageService
    ) {}

    get needsPurchasePicker(): boolean {
        return !this.purchaseId;
    }

    get grandTotal(): number {
        return this.lines.reduce((sum, line) => sum + this.lineTotal(line), 0);
    }

    get hasReturnQty(): boolean {
        return this.lines.some((line) => (line.returnQuantity || 0) > 0);
    }

    get canSave(): boolean {
        return (
            !this.loading &&
            !this.saving &&
            !!this.returnDate &&
            this.lines.length > 0 &&
            this.hasReturnQty &&
            this.lines.every(
                (line) =>
                    (line.returnQuantity || 0) >= 0 &&
                    (line.returnQuantity || 0) <= (line.returnableQuantity || 0)
            )
        );
    }

    ngOnChanges(changes: SimpleChanges): void {
        if (changes['visible'] && this.visible) {
            this.resetForm();
            if (this.purchaseId) {
                this.selectedPurchaseId = this.purchaseId;
                this.loadReturnable(this.purchaseId);
            } else {
                this.loadPurchases();
            }
        }
    }

    onVisibleChange(visible: boolean): void {
        this.visible = visible;
        this.visibleChange.emit(visible);
        if (!visible) {
            this.resetForm();
        }
    }

    onHide(): void {
        this.onVisibleChange(false);
    }

    onPurchaseSelected(): void {
        if (this.selectedPurchaseId) {
            this.loadReturnable(this.selectedPurchaseId);
        } else {
            this.purchaseInfo = null;
            this.lines = [];
        }
    }

    lineTotal(line: PurchaseReturnLineRow): number {
        return (line.returnQuantity || 0) * (line.unitCost || 0);
    }

    save(): void {
        const purchaseId = this.purchaseId || this.selectedPurchaseId;
        if (!purchaseId || !this.canSave) {
            return;
        }

        const payloadLines = this.lines
            .filter((line) => (line.returnQuantity || 0) > 0)
            .map((line) => ({
                purchaseLineId: line.purchaseLineId,
                quantity: line.returnQuantity,
            }));

        if (!payloadLines.length) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Warning',
                detail: 'Enter return quantity for at least one product',
            });
            return;
        }

        let branchId: number;
        try {
            branchId = this.branchContext.requireBranchId();
        } catch (e: any) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Validation',
                detail: e?.message || 'Please select a branch from the top navigation.',
            });
            return;
        }

        this.saving = true;
        this.purchaseReturnService
            .create({
                purchaseId,
                branchId,
                returnDate: this.returnDate,
                notes: this.notes?.trim() || undefined,
                lines: payloadLines,
            })
            .then(() => {
                this.messageService.add({
                    severity: 'success',
                    summary: 'Success',
                    detail: 'Purchase return created successfully',
                });
                this.saved.emit();
                this.onHide();
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail:
                        error?.message || 'Failed to create purchase return',
                });
            })
            .finally(() => {
                this.saving = false;
            });
    }

    private resetForm(): void {
        this.saving = false;
        this.loading = false;
        this.selectedPurchaseId = this.purchaseId;
        this.purchaseInfo = null;
        this.lines = [];
        this.notes = '';
        this.returnDate = this.toDateInputValue();
    }

    private loadPurchases(): void {
        this.loadingPurchases = true;
        this.purchaseService
            .getAll({ skipCount: 0, maxResultCount: 1000 })
            .then((result) => {
                this.purchases = result.items || [];
                this.purchaseOptions = this.purchases.map((purchase) => ({
                    label: `${purchase.invoiceNo || '#' + purchase.id} — ${purchase.supplierName || '—'}`,
                    value: purchase.id,
                }));
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to load purchases',
                });
            })
            .finally(() => {
                this.loadingPurchases = false;
            });
    }

    private loadReturnable(purchaseId: number): void {
        this.loading = true;
        this.purchaseInfo = null;
        this.lines = [];
        this.purchaseReturnService
            .getReturnablePurchase(purchaseId)
            .then((result) => {
                this.purchaseInfo = result;
                this.lines = (result.lines || []).map((line) => ({
                    purchaseLineId: line.purchaseLineId,
                    productName: line.productName || `#${line.productId}`,
                    purchasedQuantity: line.purchasedQuantity,
                    returnedQuantity: line.returnedQuantity,
                    returnableQuantity: line.returnableQuantity,
                    unitCost: line.unitCost,
                    returnQuantity: 0,
                }));
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail:
                        error?.message || 'Failed to load returnable products',
                });
                this.purchaseInfo = null;
                this.lines = [];
            })
            .finally(() => {
                this.loading = false;
            });
    }

    private toDateInputValue(date: Date = new Date()): string {
        const year = date.getFullYear();
        const month = `${date.getMonth() + 1}`.padStart(2, '0');
        const day = `${date.getDate()}`.padStart(2, '0');
        return `${year}-${month}-${day}`;
    }
}
