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
import { PurchaseService } from 'src/app/demo/service/purchase.service';

@Component({
    selector: 'app-purchase-view-dialog',
    templateUrl: './purchase-view-dialog.component.html',
})
export class PurchaseViewDialogComponent implements OnChanges {
    @Input() visible = false;
    @Input() purchaseId: number | null = null;
    @Output() visibleChange = new EventEmitter<boolean>();
    @Output() printRequested = new EventEmitter<number>();

    purchase: PurchaseDto | null = null;
    loading = false;

    constructor(
        private purchaseService: PurchaseService,
        private messageService: MessageService
    ) {}

    ngOnChanges(changes: SimpleChanges): void {
        if (changes['visible'] && this.visible && this.purchaseId) {
            this.loadPurchase(this.purchaseId);
        }
    }

    onVisibleChange(visible: boolean): void {
        this.visible = visible;
        this.visibleChange.emit(visible);
        if (!visible) {
            this.purchase = null;
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

    private loadPurchase(id: number): void {
        this.loading = true;
        this.purchase = null;
        this.purchaseService
            .get(id)
            .then((purchase) => {
                this.purchase = purchase;
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
