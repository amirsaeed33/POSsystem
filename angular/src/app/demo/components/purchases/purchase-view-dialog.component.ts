import { AppFormatService } from 'src/app/demo/service/app-format.service';
import {
    Component,
    EventEmitter,
    Input,
    OnChanges,
    Output,
    SimpleChanges,
} from '@angular/core';
import { ConfirmationService, MessageService } from 'primeng/api';
import { PurchaseDto } from 'src/app/demo/api/purchase';
import { PurchaseReturnDto } from 'src/app/demo/api/purchase-return';
import { PurchaseService } from 'src/app/demo/service/purchase.service';
import { PurchaseReturnService } from 'src/app/demo/service/purchase-return.service';

@Component({
    selector: 'app-purchase-view-dialog',
    templateUrl: './purchase-view-dialog.component.html',
})
export class PurchaseViewDialogComponent implements OnChanges {
    @Input() visible = false;
    @Input() purchaseId: number | null = null;
    @Output() visibleChange = new EventEmitter<boolean>();
    @Output() printRequested = new EventEmitter<number>();
    @Output() returnRequested = new EventEmitter<number>();
    @Output() changed = new EventEmitter<void>();

    purchase: PurchaseDto | null = null;
    returns: PurchaseReturnDto[] = [];
    loading = false;

    constructor(public formatService: AppFormatService, 
        private purchaseService: PurchaseService,
        private purchaseReturnService: PurchaseReturnService,
        private messageService: MessageService,
        private confirmationService: ConfirmationService
    ) {}

    ngOnChanges(changes: SimpleChanges): void {
        if (changes['visible'] && this.visible && this.purchaseId) {
            this.load(this.purchaseId);
        }
    }

    onVisibleChange(visible: boolean): void {
        this.visible = visible;
        this.visibleChange.emit(visible);
        if (!visible) {
            this.purchase = null;
            this.returns = [];
        }
    }

    onHide(): void {
        this.onVisibleChange(false);
    }

    printInvoice(): void {
        if (!this.purchase?.id) {
            return;
        }
        this.printRequested.emit(this.purchase.id);
    }

    returnProducts(): void {
        if (!this.purchase?.id) {
            return;
        }
        const purchaseId = this.purchase.id;
        this.onHide();
        this.returnRequested.emit(purchaseId);
    }

    deleteReturn(purchaseReturn: PurchaseReturnDto): void {
        this.confirmationService.confirm({
            message: `Are you sure you want to delete purchase return #${purchaseReturn.id}?`,
            header: 'Delete Confirmation',
            icon: 'pi pi-exclamation-triangle',
            acceptButtonStyleClass: 'p-button-danger',
            accept: () => {
                this.purchaseReturnService
                    .delete(purchaseReturn.id)
                    .then(() => {
                        this.messageService.add({
                            severity: 'success',
                            summary: 'Success',
                            detail: 'Purchase return deleted successfully',
                        });
                        if (this.purchaseId) {
                            this.load(this.purchaseId);
                        }
                        this.changed.emit();
                    })
                    .catch((error) => {
                        this.messageService.add({
                            severity: 'error',
                            summary: 'Error',
                            detail:
                                error?.message ||
                                'Failed to delete purchase return',
                        });
                    });
            },
        });
    }

    private load(id: number): void {
        this.loading = true;
        this.purchase = null;
        this.returns = [];

        Promise.all([
            this.purchaseService.get(id),
            this.purchaseReturnService.getAll({
                purchaseId: id,
                skipCount: 0,
                maxResultCount: 100,
            }),
        ])
            .then(([purchase, returnsResult]) => {
                this.purchase = purchase;
                this.returns = returnsResult.items || [];
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to load purchase',
                });
                this.onHide();
            })
            .finally(() => {
                this.loading = false;
            });
    }
}
