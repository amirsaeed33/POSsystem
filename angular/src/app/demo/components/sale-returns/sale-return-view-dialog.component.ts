import {
    Component,
    EventEmitter,
    Input,
    OnChanges,
    Output,
    SimpleChanges,
} from '@angular/core';
import { ConfirmationService, MessageService } from 'primeng/api';
import { SaleReturnDto } from 'src/app/demo/api/sale-return';
import { SaleReturnService } from 'src/app/demo/service/sale-return.service';

@Component({
    selector: 'app-sale-return-view-dialog',
    templateUrl: './sale-return-view-dialog.component.html',
})
export class SaleReturnViewDialogComponent implements OnChanges {
    @Input() visible = false;
    @Input() saleReturnId: number | null = null;
    @Output() visibleChange = new EventEmitter<boolean>();
    @Output() deleted = new EventEmitter<void>();

    saleReturn: SaleReturnDto | null = null;
    loading = false;

    constructor(
        private saleReturnService: SaleReturnService,
        private messageService: MessageService,
        private confirmationService: ConfirmationService
    ) {}

    ngOnChanges(changes: SimpleChanges): void {
        if (changes['visible'] && this.visible && this.saleReturnId) {
            this.load(this.saleReturnId);
        }
    }

    onVisibleChange(visible: boolean): void {
        this.visible = visible;
        this.visibleChange.emit(visible);
        if (!visible) {
            this.saleReturn = null;
        }
    }

    onHide(): void {
        this.onVisibleChange(false);
    }

    onDelete(): void {
        if (!this.saleReturn?.id) {
            return;
        }
        const id = this.saleReturn.id;
        this.confirmationService.confirm({
            message: `Are you sure you want to delete sale return #${id}?`,
            header: 'Delete Confirmation',
            icon: 'pi pi-exclamation-triangle',
            acceptButtonStyleClass: 'p-button-danger',
            accept: () => {
                this.saleReturnService
                    .delete(id)
                    .then(() => {
                        this.messageService.add({
                            severity: 'success',
                            summary: 'Success',
                            detail: 'Sale return deleted successfully',
                        });
                        this.deleted.emit();
                        this.onHide();
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

    private load(id: number): void {
        this.loading = true;
        this.saleReturn = null;
        this.saleReturnService
            .get(id)
            .then((result) => {
                this.saleReturn = result;
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to load sale return',
                });
                this.onHide();
            })
            .finally(() => {
                this.loading = false;
            });
    }
}
