import {
    Component,
    EventEmitter,
    Input,
    OnChanges,
    Output,
    SimpleChanges,
} from '@angular/core';
import { MessageService } from 'primeng/api';
import { PaymentType, SaleDto } from 'src/app/demo/api/sale';
import { SaleService } from 'src/app/demo/service/sale.service';

@Component({
    selector: 'app-sale-view-dialog',
    templateUrl: './sale-view-dialog.component.html',
})
export class SaleViewDialogComponent implements OnChanges {
    @Input() visible = false;
    @Input() saleId: number | null = null;
    @Output() visibleChange = new EventEmitter<boolean>();
    @Output() printRequested = new EventEmitter<number>();

    sale: SaleDto | null = null;
    loading = false;

    constructor(
        private saleService: SaleService,
        private messageService: MessageService
    ) {}

    ngOnChanges(changes: SimpleChanges): void {
        if (changes['visible'] && this.visible && this.saleId) {
            this.loadSale(this.saleId);
        }
    }

    onVisibleChange(visible: boolean): void {
        this.visible = visible;
        this.visibleChange.emit(visible);
        if (!visible) {
            this.sale = null;
        }
    }

    onHide(): void {
        this.onVisibleChange(false);
    }

    printInvoice(): void {
        if (!this.sale?.id) {
            return;
        }
        this.printRequested.emit(this.sale.id);
    }

    paymentTypeLabel(paymentType: number): string {
        switch (paymentType) {
            case PaymentType.Cash:
                return 'Cash';
            case PaymentType.Card:
                return 'Card';
            case PaymentType.Credit:
                return 'Credit';
            case PaymentType.Mixed:
                return 'Mixed';
            default:
                return String(paymentType);
        }
    }

    private loadSale(id: number): void {
        this.loading = true;
        this.sale = null;
        this.saleService
            .get(id)
            .then((sale) => {
                this.sale = sale;
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to load sale',
                });
                this.onHide();
            })
            .finally(() => {
                this.loading = false;
            });
    }
}
