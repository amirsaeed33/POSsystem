import {
    Component,
    EventEmitter,
    Input,
    OnChanges,
    Output,
    SimpleChanges,
} from '@angular/core';
import { CustomerDto, CustomerType } from 'src/app/demo/api/customer';
import { CustomerService } from 'src/app/demo/service/customer.service';
import { MessageService } from 'primeng/api';

@Component({
    selector: 'app-customer-form-dialog',
    templateUrl: './customer-form-dialog.component.html',
})
export class CustomerFormDialogComponent implements OnChanges {
    @Input() visible = false;
    @Input() customerId: number | null = null;
    @Output() visibleChange = new EventEmitter<boolean>();
    @Output() saved = new EventEmitter<void>();

    customer: CustomerDto = this.emptyCustomer();
    saving = false;
    loading = false;

    customerTypeOptions = [
        { label: 'Direct', value: CustomerType.Direct },
        { label: 'Wholesaler', value: CustomerType.Wholesaler },
    ];

    constructor(
        private customerService: CustomerService,
        private messageService: MessageService
    ) {}

    get dialogTitle(): string {
        return this.customerId ? 'Edit Customer' : 'Create Customer';
    }

    ngOnChanges(changes: SimpleChanges): void {
        if (changes['visible'] && this.visible) {
            this.resetForm();
            if (this.customerId) {
                this.loadCustomer(this.customerId);
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
        const name = (this.customer.name || '').trim();
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
            customerType: this.customer.customerType ?? CustomerType.Direct,
            phone: this.customer.phone?.trim() || undefined,
            email: this.customer.email?.trim() || undefined,
            address: this.customer.address?.trim() || undefined,
            description: this.customer.description?.trim() || undefined,
        };

        const request = this.customerId
            ? this.customerService.update({
                  ...this.customer,
                  ...payload,
                  id: this.customerId,
              })
            : this.customerService.create(payload);

        request
            .then(() => {
                this.messageService.add({
                    severity: 'success',
                    summary: 'Success',
                    detail: this.customerId
                        ? 'Customer updated successfully'
                        : 'Customer created successfully',
                });
                this.saved.emit();
                this.onHide();
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to save customer',
                });
            })
            .finally(() => {
                this.saving = false;
            });
    }

    private emptyCustomer(): CustomerDto {
        return {
            id: 0,
            branchId: 0,
            name: '',
            customerType: CustomerType.Direct,
            phone: '',
            email: '',
            address: '',
            description: '',
            balance: 0,
        };
    }

    private resetForm(): void {
        this.customer = this.emptyCustomer();
        this.saving = false;
        this.loading = false;
    }

    private loadCustomer(id: number): void {
        this.loading = true;
        this.customerService
            .get(id)
            .then((customer) => {
                this.customer = { ...customer };
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to load customer',
                });
                this.onHide();
            })
            .finally(() => {
                this.loading = false;
            });
    }
}
