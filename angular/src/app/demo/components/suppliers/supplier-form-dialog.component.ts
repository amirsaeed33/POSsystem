import {
    Component,
    EventEmitter,
    Input,
    OnChanges,
    Output,
    SimpleChanges,
} from '@angular/core';
import { SupplierDto } from 'src/app/demo/api/supplier';
import { SupplierService } from 'src/app/demo/service/supplier.service';
import { MessageService } from 'primeng/api';

@Component({
    selector: 'app-supplier-form-dialog',
    templateUrl: './supplier-form-dialog.component.html',
})
export class SupplierFormDialogComponent implements OnChanges {
    @Input() visible = false;
    @Input() supplierId: number | null = null;
    @Output() visibleChange = new EventEmitter<boolean>();
    @Output() saved = new EventEmitter<void>();

    supplier: SupplierDto = this.emptySupplier();
    saving = false;
    loading = false;

    constructor(
        private supplierService: SupplierService,
        private messageService: MessageService
    ) {}

    get dialogTitle(): string {
        return this.supplierId ? 'Edit Supplier' : 'Create Supplier';
    }

    ngOnChanges(changes: SimpleChanges): void {
        if (changes['visible'] && this.visible) {
            this.resetForm();
            if (this.supplierId) {
                this.loadSupplier(this.supplierId);
            }
        }
    }

    onVisibleChange(visible: boolean): void {
        this.visible = visible;
        this.visibleChange.emit(visible);
    }

    onHide(): void {
        this.onVisibleChange(false);
    }

    save(): void {
        const name = (this.supplier.name || '').trim();
        if (!name) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Validation',
                detail: 'Name is required',
            });
            return;
        }

        this.saving = true;
        const payload = {
            name,
            phone: this.supplier.phone?.trim() || undefined,
            email: this.supplier.email?.trim() || undefined,
            address: this.supplier.address?.trim() || undefined,
            description: this.supplier.description?.trim() || undefined,
        };

        const request = this.supplierId
            ? this.supplierService.update({
                  ...this.supplier,
                  ...payload,
                  id: this.supplierId,
              })
            : this.supplierService.create(payload);

        request
            .then(() => {
                this.messageService.add({
                    severity: 'success',
                    summary: 'Success',
                    detail: this.supplierId
                        ? 'Supplier updated successfully'
                        : 'Supplier created successfully',
                });
                this.saved.emit();
                this.onHide();
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to save supplier',
                });
            })
            .finally(() => {
                this.saving = false;
            });
    }

    private emptySupplier(): SupplierDto {
        return {
            id: 0,
            name: '',
            phone: '',
            email: '',
            address: '',
            description: '',
            balance: 0,
        };
    }

    private resetForm(): void {
        this.supplier = this.emptySupplier();
        this.saving = false;
        this.loading = false;
    }

    private loadSupplier(id: number): void {
        this.loading = true;
        this.supplierService
            .get(id)
            .then((supplier) => {
                this.supplier = { ...supplier };
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to load supplier',
                });
                this.onHide();
            })
            .finally(() => {
                this.loading = false;
            });
    }
}
