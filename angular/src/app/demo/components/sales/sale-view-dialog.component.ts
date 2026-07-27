import {
    Component,
    EventEmitter,
    Input,
    OnChanges,
    Output,
    SimpleChanges,
} from '@angular/core';
import { ConfirmationService, MessageService } from 'primeng/api';
import { PaymentType, SaleDto } from 'src/app/demo/api/sale';
import { SaleReturnDto } from 'src/app/demo/api/sale-return';
import { SaleService } from 'src/app/demo/service/sale.service';
import { SaleReturnService } from 'src/app/demo/service/sale-return.service';

@Component({
    selector: 'app-sale-view-dialog',
    templateUrl: './sale-view-dialog.component.html',
})
export class SaleViewDialogComponent implements OnChanges {
    @Input() visible = false;
    @Input() saleId: number | null = null;
    @Output() visibleChange = new EventEmitter<boolean>();
    @Output() printRequested = new EventEmitter<number>();
    @Output() returnRequested = new EventEmitter<number>();
    @Output() changed = new EventEmitter<void>();

    sale: SaleDto | null = null;
    returns: SaleReturnDto[] = [];
    loading = false;

    constructor(
        private saleService: SaleService,
        private saleReturnService: SaleReturnService,
        private messageService: MessageService,
        private confirmationService: ConfirmationService
    ) {}

    ngOnChanges(changes: SimpleChanges): void {
        if (changes['visible'] && this.visible && this.saleId) {
            this.load(this.saleId);
        }
    }

    onVisibleChange(visible: boolean): void {
        this.visible = visible;
        this.visibleChange.emit(visible);
        if (!visible) {
            this.sale = null;
            this.returns = [];
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

    returnProducts(): void {
        if (!this.sale?.id) {
            return;
        }
        const saleId = this.sale.id;
        this.onHide();
        this.returnRequested.emit(saleId);
    }

    deleteReturn(saleReturn: SaleReturnDto): void {
        this.confirmationService.confirm({
            message: `Are you sure you want to delete sale return #${saleReturn.id}?`,
            header: 'Delete Confirmation',
            icon: 'pi pi-exclamation-triangle',
            acceptButtonStyleClass: 'p-button-danger',
            accept: () => {
                this.saleReturnService
                    .delete(saleReturn.id)
                    .then(() => {
                        this.messageService.add({
                            severity: 'success',
                            summary: 'Success',
                            detail: 'Sale return deleted successfully',
                        });
                        if (this.saleId) {
                            this.load(this.saleId);
                        }
                        this.changed.emit();
                    })
                    .catch((error) => {
                        this.messageService.add({
                            severity: 'error',
                            summary: 'Error',
                            detail:
                                error?.message || 'Failed to delete sale return',
                        });
                    });
            },
        });
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

    private load(id: number): void {
        this.loading = true;
        this.sale = null;
        this.returns = [];

        Promise.all([
            this.saleService.get(id),
            this.saleReturnService.getAll({
                saleId: id,
                skipCount: 0,
                maxResultCount: 100,
            }),
        ])
            .then(([sale, returnsResult]) => {
                this.sale = sale;
                this.returns = returnsResult.items || [];
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
