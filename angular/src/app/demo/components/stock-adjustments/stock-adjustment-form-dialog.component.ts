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
    CreateStockAdjustmentDto,
    CreateStockAdjustmentLineDto,
    StockAdjustmentReasons,
} from 'src/app/demo/api/stock-adjustment';
import { ProductDto } from 'src/app/demo/api/product';
import { StockAdjustmentService } from 'src/app/demo/service/stock-adjustment.service';
import { BranchContextService } from 'src/app/demo/service/branch-context.service';
import { ProductService } from 'src/app/demo/service/product.service';

@Component({
    selector: 'app-stock-adjustment-form-dialog',
    templateUrl: './stock-adjustment-form-dialog.component.html',
})
export class StockAdjustmentFormDialogComponent implements OnChanges {
    @Input() visible = false;
    @Input() adjustmentId: number | null = null;
    @Output() visibleChange = new EventEmitter<boolean>();
    @Output() saved = new EventEmitter<void>();

    adjustment: CreateStockAdjustmentDto = this.emptyAdjustment();
    products: ProductDto[] = [];
    saving = false;
    loading = false;

    reasons = [
        { value: StockAdjustmentReasons.Opening, label: 'Opening' },
        { value: StockAdjustmentReasons.Damage, label: 'Damage' },
        { value: StockAdjustmentReasons.Loss, label: 'Loss' },
        { value: StockAdjustmentReasons.Recount, label: 'Recount' },
        { value: StockAdjustmentReasons.Other, label: 'Other' },
    ];

    constructor(
        private stockAdjustmentService: StockAdjustmentService,
        private productService: ProductService,
        private branchContext: BranchContextService,
        private messageService: MessageService
    ) {}

    get dialogTitle(): string {
        return this.adjustmentId
            ? 'Edit Stock Adjustment'
            : 'Create Stock Adjustment';
    }

    ngOnChanges(changes: SimpleChanges): void {
        if (changes['visible'] && this.visible) {
            this.resetForm();
            this.loadLookups().then(() => {
                if (this.adjustmentId) {
                    this.loadAdjustment(this.adjustmentId);
                }
            });
        }
    }

    onVisibleChange(visible: boolean): void {
        this.visible = visible;
        this.visibleChange.emit(visible);
    }

    onHide(): void {
        this.onVisibleChange(false);
    }

    addLine(): void {
        this.adjustment.lines.push(this.emptyLine());
    }

    removeLine(index: number): void {
        if (this.adjustment.lines.length > 1) {
            this.adjustment.lines.splice(index, 1);
        }
    }

    onProductSelected(line: CreateStockAdjustmentLineDto): void {
        if (!line.productId) {
            return;
        }
        this.mergeDuplicateProductLine(line);
    }

    save(): void {
        if (!this.adjustment.adjustmentDate) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Validation',
                detail: 'Adjustment date is required',
            });
            return;
        }
        if (this.adjustment.reason == null) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Validation',
                detail: 'Reason is required',
            });
            return;
        }

        const lines = this.mergeLinesByProduct(
            (this.adjustment.lines || []).filter(
                (line) => line.productId && line.quantityChange !== 0
            )
        );
        this.adjustment.lines = lines.length ? [...lines] : [this.emptyLine()];
        if (!lines.length) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Validation',
                detail: 'Add at least one product line with a non-zero quantity change',
            });
            return;
        }

        for (const line of lines) {
            if (line.quantityChange === 0) {
                this.messageService.add({
                    severity: 'warn',
                    summary: 'Validation',
                    detail: 'Quantity change cannot be zero',
                });
                return;
            }
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

        const payload: CreateStockAdjustmentDto = {
            branchId,
            adjustmentDate: this.adjustment.adjustmentDate,
            reason: this.adjustment.reason,
            notes: this.adjustment.notes?.trim() || undefined,
            lines: lines.map((line) => ({
                productId: line.productId,
                quantityChange: line.quantityChange,
            })),
        };

        this.saving = true;
        const request = this.adjustmentId
            ? this.stockAdjustmentService.replace(this.adjustmentId, payload)
            : this.stockAdjustmentService.create(payload);

        request
            .then(() => {
                this.messageService.add({
                    severity: 'success',
                    summary: 'Success',
                    detail: this.adjustmentId
                        ? 'Stock adjustment updated successfully'
                        : 'Stock adjustment created successfully',
                });
                this.saved.emit();
                this.onHide();
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail:
                        error?.message ||
                        (this.adjustmentId
                            ? 'Failed to update stock adjustment'
                            : 'Failed to create stock adjustment'),
                });
            })
            .finally(() => {
                this.saving = false;
            });
    }

    private emptyLine(): CreateStockAdjustmentLineDto {
        return {
            productId: null as any,
            quantityChange: 1,
        };
    }

    private mergeDuplicateProductLine(line: CreateStockAdjustmentLineDto): void {
        const lines = this.adjustment.lines || [];
        const existing = lines.find(
            (l) => l !== line && l.productId === line.productId
        );
        if (!existing) {
            return;
        }

        existing.quantityChange = Number(
            (
                (existing.quantityChange || 0) + (line.quantityChange || 0)
            ).toFixed(2)
        );

        const index = lines.indexOf(line);
        if (index < 0) {
            return;
        }
        if (lines.length > 1) {
            lines.splice(index, 1);
        } else {
            Object.assign(line, this.emptyLine());
        }

        this.messageService.add({
            severity: 'info',
            summary: 'Merged',
            detail: 'Same product combined into one line',
        });
    }

    private mergeLinesByProduct(
        lines: CreateStockAdjustmentLineDto[]
    ): CreateStockAdjustmentLineDto[] {
        const merged = new Map<number, CreateStockAdjustmentLineDto>();
        for (const line of lines) {
            const existing = merged.get(line.productId);
            if (existing) {
                existing.quantityChange = Number(
                    (
                        (existing.quantityChange || 0) +
                        (line.quantityChange || 0)
                    ).toFixed(2)
                );
            } else {
                merged.set(line.productId, {
                    productId: line.productId,
                    quantityChange: line.quantityChange || 0,
                });
            }
        }
        return Array.from(merged.values()).filter(
            (line) => line.quantityChange !== 0
        );
    }

    private emptyAdjustment(): CreateStockAdjustmentDto {
        return {
            branchId: 0,
            adjustmentDate: this.toDateInputValue(),
            reason: StockAdjustmentReasons.Other,
            notes: '',
            lines: [this.emptyLine()],
        };
    }

    private resetForm(): void {
        this.adjustment = this.emptyAdjustment();
        this.saving = false;
        this.loading = false;
    }

    private toDateInputValue(value?: string | Date): string {
        const date = value ? new Date(value) : new Date();
        if (isNaN(date.getTime())) {
            return this.toDateInputValue();
        }
        const year = date.getFullYear();
        const month = `${date.getMonth() + 1}`.padStart(2, '0');
        const day = `${date.getDate()}`.padStart(2, '0');
        return `${year}-${month}-${day}`;
    }

    private async loadLookups(): Promise<void> {
        this.loading = true;
        try {
            const products = await this.productService.getAll({
                skipCount: 0,
                maxResultCount: 1000,
            });
            this.products = products.items;
        } catch (error: any) {
            this.messageService.add({
                severity: 'error',
                summary: 'Error',
                detail: error?.message || 'Failed to load products',
            });
        } finally {
            if (!this.adjustmentId) {
                this.loading = false;
            }
        }
    }

    private loadAdjustment(id: number): void {
        this.loading = true;
        this.stockAdjustmentService
            .get(id)
            .then((item) => {
                this.adjustment = {
                    branchId: item.branchId ?? 0,
                    adjustmentDate: this.toDateInputValue(item.adjustmentDate),
                    reason: item.reason,
                    notes: item.notes || '',
                    lines:
                        item.lines?.length
                            ? item.lines.map((line) => ({
                                  productId: line.productId,
                                  quantityChange: line.quantityChange,
                              }))
                            : [this.emptyLine()],
                };
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
