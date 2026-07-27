import {
    Component,
    EventEmitter,
    Input,
    OnChanges,
    Output,
    SimpleChanges,
} from '@angular/core';
import { MessageService } from 'primeng/api';
import { SaleDto } from 'src/app/demo/api/sale';
import { SaleReturnableDto } from 'src/app/demo/api/sale-return';
import { SaleReturnService } from 'src/app/demo/service/sale-return.service';
import { SaleService } from 'src/app/demo/service/sale.service';

export interface SaleReturnLineRow {
    saleLineId: number;
    productName: string;
    soldQuantity: number;
    returnedQuantity: number;
    returnableQuantity: number;
    unitPrice: number;
    returnQuantity: number;
}

@Component({
    selector: 'app-sale-return-form-dialog',
    templateUrl: './sale-return-form-dialog.component.html',
})
export class SaleReturnFormDialogComponent implements OnChanges {
    @Input() visible = false;
    /** When set, skip sale picker and load this sale's returnable lines. */
    @Input() saleId: number | null = null;
    @Output() visibleChange = new EventEmitter<boolean>();
    @Output() saved = new EventEmitter<void>();

    saving = false;
    loading = false;
    loadingSales = false;
    selectedSaleId: number | null = null;
    sales: SaleDto[] = [];
    saleOptions: { label: string; value: number }[] = [];
    saleInfo: SaleReturnableDto | null = null;
    lines: SaleReturnLineRow[] = [];
    returnDate = '';
    notes = '';

    constructor(
        private saleReturnService: SaleReturnService,
        private saleService: SaleService,
        private messageService: MessageService
    ) {}

    get needsSalePicker(): boolean {
        return !this.saleId;
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
            if (this.saleId) {
                this.selectedSaleId = this.saleId;
                this.loadReturnable(this.saleId);
            } else {
                this.loadSales();
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

    onSaleSelected(): void {
        if (this.selectedSaleId) {
            this.loadReturnable(this.selectedSaleId);
        } else {
            this.saleInfo = null;
            this.lines = [];
        }
    }

    lineTotal(line: SaleReturnLineRow): number {
        return (line.returnQuantity || 0) * (line.unitPrice || 0);
    }

    save(): void {
        const saleId = this.saleId || this.selectedSaleId;
        if (!saleId || !this.canSave) {
            return;
        }

        const payloadLines = this.lines
            .filter((line) => (line.returnQuantity || 0) > 0)
            .map((line) => ({
                saleLineId: line.saleLineId,
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

        this.saving = true;
        this.saleReturnService
            .create({
                saleId,
                returnDate: this.returnDate,
                notes: this.notes?.trim() || undefined,
                lines: payloadLines,
            })
            .then(() => {
                this.messageService.add({
                    severity: 'success',
                    summary: 'Success',
                    detail: 'Sale return created successfully',
                });
                this.saved.emit();
                this.onHide();
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to create sale return',
                });
            })
            .finally(() => {
                this.saving = false;
            });
    }

    private resetForm(): void {
        this.saving = false;
        this.loading = false;
        this.selectedSaleId = this.saleId;
        this.saleInfo = null;
        this.lines = [];
        this.notes = '';
        this.returnDate = this.toDateInputValue();
    }

    private loadSales(): void {
        this.loadingSales = true;
        this.saleService
            .getAll({ skipCount: 0, maxResultCount: 1000 })
            .then((result) => {
                this.sales = result.items || [];
                this.saleOptions = this.sales.map((sale) => ({
                    label: `${sale.invoiceNo || '#' + sale.id} — ${sale.customerName || '—'}`,
                    value: sale.id,
                }));
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to load sales',
                });
            })
            .finally(() => {
                this.loadingSales = false;
            });
    }

    private loadReturnable(saleId: number): void {
        this.loading = true;
        this.saleInfo = null;
        this.lines = [];
        this.saleReturnService
            .getReturnableSale(saleId)
            .then((result) => {
                this.saleInfo = result;
                this.lines = (result.lines || []).map((line) => ({
                    saleLineId: line.saleLineId,
                    productName: line.productName || `#${line.productId}`,
                    soldQuantity: line.soldQuantity,
                    returnedQuantity: line.returnedQuantity,
                    returnableQuantity: line.returnableQuantity,
                    unitPrice: line.unitPrice,
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
                this.saleInfo = null;
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
