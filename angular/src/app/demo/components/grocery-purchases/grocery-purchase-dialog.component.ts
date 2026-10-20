import {
    Component,
    EventEmitter,
    Input,
    OnChanges,
    OnInit,
    Output,
    SimpleChanges,
} from '@angular/core';
import { MessageService } from 'primeng/api';
import {
    CreateGroceryPurchaseDto,
    GroceryPurchaseDto,
    UpdateGroceryPurchaseDto,
} from 'src/app/demo/api/grocery-purchase';
import { GroceryPurchaseService } from 'src/app/demo/service/grocery-purchase.service';
import { UnitService } from 'src/app/demo/service/unit.service';
import { BranchContextService } from 'src/app/demo/service/branch-context.service';

@Component({
    selector: 'app-grocery-purchase-dialog',
    templateUrl: './grocery-purchase-dialog.component.html',
})
export class GroceryPurchaseDialogComponent implements OnInit, OnChanges {
    @Input() visible = false;
    @Input() purchaseId: number | null = null;
    @Output() visibleChange = new EventEmitter<boolean>();
    @Output() saved = new EventEmitter<void>();

    loading = false;
    saving = false;

    item: Partial<GroceryPurchaseDto> = {
        productName: '',
        purchasePrice: undefined,
        unit: 'kg',
    };

    purchaseDateString = '';
    selectedUnit = 'kg';

    unitOptions: { label: string; value: string; unitId?: number }[] = [
        { label: 'kg', value: 'kg' },
        { label: 'gram', value: 'gram' },
        { label: 'litre', value: 'litre' },
        { label: 'ml', value: 'ml' },
        { label: 'piece', value: 'piece' },
        { label: 'dozen', value: 'dozen' },
        { label: 'pack', value: 'pack' },
        { label: 'box', value: 'box' },
        { label: 'bag', value: 'bag' },
        { label: 'bundle', value: 'bundle' },
    ];

    constructor(
        private groceryPurchaseService: GroceryPurchaseService,
        private unitService: UnitService,
        private branchContext: BranchContextService,
        private messageService: MessageService
    ) {}

    get dialogTitle(): string {
        return this.purchaseId ? 'Edit Grocery Purchase' : 'Record Grocery Purchase';
    }

    ngOnInit(): void {
        this.loadUnitOptions();
    }

    ngOnChanges(changes: SimpleChanges): void {
        if (changes['visible'] && this.visible) {
            this.initForm();
        }
    }

    private async loadUnitOptions(): Promise<void> {
        try {
            const units = await this.unitService.getLookup();
            if (units?.length) {
                const map = new Map<string, { label: string; value: string; unitId?: number }>();
                for (const u of units) {
                    const val = u.symbol || u.name;
                    if (val) {
                        map.set(val.toLowerCase(), {
                            label: `${u.name}${u.symbol ? ' (' + u.symbol + ')' : ''}`,
                            value: val,
                            unitId: u.id,
                        });
                    }
                }
                // Add default standard culinary units if not present
                for (const opt of this.unitOptions) {
                    if (!map.has(opt.value.toLowerCase())) {
                        map.set(opt.value.toLowerCase(), opt);
                    }
                }
                this.unitOptions = Array.from(map.values());
            }
        } catch {
            // Keep default standard options
        }
    }

    private async initForm(): Promise<void> {
        if (this.purchaseId) {
            this.loading = true;
            try {
                const data = await this.groceryPurchaseService.get(this.purchaseId);
                this.item = { ...data };
                this.selectedUnit = data.unit || 'kg';
                this.purchaseDateString = this.formatDateForInput(data.purchaseDate);
            } catch (err: any) {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: err?.message || 'Failed to load grocery purchase details',
                });
                this.onHide();
            } finally {
                this.loading = false;
            }
        } else {
            this.item = {
                productName: '',
                purchasePrice: undefined,
                unit: 'kg',
            };
            this.selectedUnit = 'kg';
            this.purchaseDateString = this.formatDateForInput(new Date());
        }
    }

    onUnitChange(event: any): void {
        const val = event.value || event;
        this.selectedUnit = typeof val === 'string' ? val : (val?.value || val?.label || 'kg');
        this.item.unit = this.selectedUnit;
        const matched = this.unitOptions.find(
            (u) => u.value.toLowerCase() === this.selectedUnit.toLowerCase()
        );
        this.item.unitId = matched?.unitId ?? null;
    }

    async save(): Promise<void> {
        const productName = this.item.productName?.trim();
        if (!productName) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Validation',
                detail: 'Product name is required.',
            });
            return;
        }

        const price = Number(this.item.purchasePrice);
        if (isNaN(price) || price <= 0) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Validation',
                detail: 'Please enter a valid purchase price greater than zero.',
            });
            return;
        }

        const unit = (this.selectedUnit || this.item.unit || '').trim();
        if (!unit) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Validation',
                detail: 'Unit of measurement is required.',
            });
            return;
        }

        if (!this.purchaseDateString) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Validation',
                detail: 'Purchase date is required.',
            });
            return;
        }

        const purchaseDate = new Date(this.purchaseDateString);
        if (isNaN(purchaseDate.getTime())) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Validation',
                detail: 'Purchase date is invalid.',
            });
            return;
        }

        this.saving = true;
        try {
            if (this.purchaseId) {
                const updateDto: UpdateGroceryPurchaseDto = {
                    id: this.purchaseId,
                    productName,
                    purchasePrice: price,
                    unit,
                    unitId: this.item.unitId,
                    purchaseDate,
                };
                await this.groceryPurchaseService.update(updateDto);
                this.messageService.add({
                    severity: 'success',
                    summary: 'Success',
                    detail: 'Grocery purchase updated successfully.',
                });
            } else {
                const createDto: CreateGroceryPurchaseDto = {
                    branchId: this.branchContext.getBranchId(),
                    productName,
                    purchasePrice: price,
                    unit,
                    unitId: this.item.unitId,
                    purchaseDate,
                };
                await this.groceryPurchaseService.create(createDto);
                this.messageService.add({
                    severity: 'success',
                    summary: 'Success',
                    detail: 'Grocery purchase recorded successfully.',
                });
            }

            this.saved.emit();
            this.onHide();
        } catch (err: any) {
            this.messageService.add({
                severity: 'error',
                summary: 'Error',
                detail: err?.message || 'Failed to save grocery purchase.',
            });
        } finally {
            this.saving = false;
        }
    }

    onHide(): void {
        this.visible = false;
        this.visibleChange.emit(false);
    }

    private formatDateForInput(d: string | Date | undefined): string {
        if (!d) return '';
        const date = d instanceof Date ? d : new Date(d);
        if (isNaN(date.getTime())) return '';
        const y = date.getFullYear();
        const m = String(date.getMonth() + 1).padStart(2, '0');
        const day = String(date.getDate()).padStart(2, '0');
        return `${y}-${m}-${day}`;
    }
}
