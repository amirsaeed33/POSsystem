import {
    Component,
    EventEmitter,
    Input,
    OnChanges,
    Output,
    SimpleChanges,
} from '@angular/core';
import { ConfirmationService, MessageService } from 'primeng/api';
import { PurchaseReturnDto } from 'src/app/demo/api/purchase-return';
import { PurchaseReturnService } from 'src/app/demo/service/purchase-return.service';

@Component({
    selector: 'app-purchase-return-view-dialog',
    templateUrl: './purchase-return-view-dialog.component.html',
})
export class PurchaseReturnViewDialogComponent implements OnChanges {
    @Input() visible = false;
    @Input() purchaseReturnId: number | null = null;
    @Output() visibleChange = new EventEmitter<boolean>();
    @Output() deleted = new EventEmitter<void>();

    purchaseReturn: PurchaseReturnDto | null = null;
    loading = false;

    constructor(
        private purchaseReturnService: PurchaseReturnService,
        private messageService: MessageService,
        private confirmationService: ConfirmationService
    ) {}

    ngOnChanges(changes: SimpleChanges): void {
        if (changes['visible'] && this.visible && this.purchaseReturnId) {
            this.load(this.purchaseReturnId);
        }
    }

    onVisibleChange(visible: boolean): void {
        this.visible = visible;
        this.visibleChange.emit(visible);
        if (!visible) {
            this.purchaseReturn = null;
        }
    }

    onHide(): void {
        this.onVisibleChange(false);
    }

    onDelete(): void {
        if (!this.purchaseReturn?.id) {
            return;
        }
        const id = this.purchaseReturn.id;
        this.confirmationService.confirm({
            message: `Are you sure you want to delete purchase return #${id}?`,
            header: 'Delete Confirmation',
            icon: 'pi pi-exclamation-triangle',
            acceptButtonStyleClass: 'p-button-danger',
            accept: () => {
                this.purchaseReturnService
                    .delete(id)
                    .then(() => {
                        this.messageService.add({
                            severity: 'success',
                            summary: 'Success',
                            detail: 'Purchase return deleted successfully',
                        });
                        this.deleted.emit();
                        this.onHide();
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
        this.purchaseReturn = null;
        this.purchaseReturnService
            .get(id)
            .then((result) => {
                this.purchaseReturn = result;
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to load purchase return',
                });
                this.onHide();
            })
            .finally(() => {
                this.loading = false;
            });
    }
}
