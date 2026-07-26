import {
    Component,
    EventEmitter,
    Input,
    OnChanges,
    Output,
    SimpleChanges,
} from '@angular/core';
import { MessageService } from 'primeng/api';
import {
    StockAdjustmentDto,
    StockAdjustmentReasons,
} from 'src/app/demo/api/stock-adjustment';
import { StockAdjustmentService } from 'src/app/demo/service/stock-adjustment.service';

@Component({
    selector: 'app-stock-adjustment-view-dialog',
    templateUrl: './stock-adjustment-view-dialog.component.html',
})
export class StockAdjustmentViewDialogComponent implements OnChanges {
    @Input() visible = false;
    @Input() adjustmentId: number | null = null;
    @Output() visibleChange = new EventEmitter<boolean>();

    adjustment: StockAdjustmentDto | null = null;
    loading = false;

    constructor(
        private stockAdjustmentService: StockAdjustmentService,
        private messageService: MessageService
    ) {}

    ngOnChanges(changes: SimpleChanges): void {
        if (changes['visible'] && this.visible && this.adjustmentId) {
            this.loadAdjustment(this.adjustmentId);
        }
    }

    onVisibleChange(visible: boolean): void {
        this.visible = visible;
        this.visibleChange.emit(visible);
        if (!visible) {
            this.adjustment = null;
        }
    }

    onHide(): void {
        this.onVisibleChange(false);
    }

    reasonLabel(reason: number): string {
        switch (reason) {
            case StockAdjustmentReasons.Opening:
                return 'Opening';
            case StockAdjustmentReasons.Damage:
                return 'Damage';
            case StockAdjustmentReasons.Loss:
                return 'Loss';
            case StockAdjustmentReasons.Recount:
                return 'Recount';
            default:
                return 'Other';
        }
    }

    private loadAdjustment(id: number): void {
        this.loading = true;
        this.adjustment = null;
        this.stockAdjustmentService
            .get(id)
            .then((adjustment) => {
                this.adjustment = adjustment;
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to load stock adjustment',
                });
                this.onHide();
            })
            .finally(() => {
                this.loading = false;
            });
    }
}
