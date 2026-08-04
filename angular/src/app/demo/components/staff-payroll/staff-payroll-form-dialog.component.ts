import {
    Component,
    EventEmitter,
    Input,
    OnChanges,
    Output,
    SimpleChanges,
} from '@angular/core';
import { MessageService } from 'primeng/api';
import { StaffDto } from 'src/app/demo/api/staff';
import {
    PayrollPaymentStatus,
    StaffPayrollDto,
} from 'src/app/demo/api/staff-payroll';
import { StaffPayrollService } from 'src/app/demo/service/staff-payroll.service';
import { StaffService } from 'src/app/demo/service/staff.service';

@Component({
    selector: 'app-staff-payroll-form-dialog',
    templateUrl: './staff-payroll-form-dialog.component.html',
})
export class StaffPayrollFormDialogComponent implements OnChanges {
    @Input() visible = false;
    @Input() payrollId: number | null = null;
    @Output() visibleChange = new EventEmitter<boolean>();
    @Output() saved = new EventEmitter<void>();

    record: StaffPayrollDto = this.emptyRecord();
    staffOptions: StaffDto[] = [];
    saving = false;
    loading = false;

    monthOptions = Array.from({ length: 12 }, (_, i) => ({
        label: new Date(2000, i, 1).toLocaleString('default', { month: 'long' }),
        value: i + 1,
    }));

    paymentStatusOptions = [
        { label: 'Pending', value: PayrollPaymentStatus.Pending },
        { label: 'Paid', value: PayrollPaymentStatus.Paid },
        { label: 'Cancelled', value: PayrollPaymentStatus.Cancelled },
    ];

    constructor(
        private payrollService: StaffPayrollService,
        private staffService: StaffService,
        private messageService: MessageService
    ) {}

    get dialogTitle(): string {
        return this.payrollId ? 'Edit Payroll' : 'Create Payroll';
    }

    get netSalaryPreview(): number {
        return (
            (this.record.basicSalary || 0) +
            (this.record.allowance || 0) +
            (this.record.bonus || 0) +
            (this.record.overtimeAmount || 0) -
            (this.record.deduction || 0)
        );
    }

    ngOnChanges(changes: SimpleChanges): void {
        if (changes['visible'] && this.visible) {
            this.resetForm();
            this.loadStaffOptions();
            if (this.payrollId) {
                this.loadRecord(this.payrollId);
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

    onStaffChange(): void {
        if (this.payrollId) {
            return;
        }
        const staff = this.staffOptions.find((x) => x.id === this.record.staffId);
        if (staff?.basicSalary != null) {
            this.record.basicSalary = staff.basicSalary;
        }
    }

    save(): void {
        if (!this.record.staffId) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Validation',
                detail: 'Staff is required',
            });
            return;
        }

        if (!this.record.month || !this.record.year) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Validation',
                detail: 'Month and year are required',
            });
            return;
        }

        if (
            (this.record.basicSalary || 0) < 0 ||
            (this.record.allowance || 0) < 0 ||
            (this.record.bonus || 0) < 0 ||
            (this.record.deduction || 0) < 0 ||
            (this.record.overtimeAmount || 0) < 0
        ) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Validation',
                detail: 'Salary values cannot be negative',
            });
            return;
        }

        this.saving = true;
        const payload = {
            staffId: this.record.staffId,
            month: this.record.month,
            year: this.record.year,
            basicSalary: this.record.basicSalary ?? 0,
            allowance: this.record.allowance || 0,
            bonus: this.record.bonus || 0,
            deduction: this.record.deduction || 0,
            overtimeAmount: this.record.overtimeAmount || 0,
            paymentStatus: this.record.paymentStatus,
            paymentDate: this.record.paymentDate || null,
            remarks: this.record.remarks?.trim() || undefined,
        };

        const request = this.payrollId
            ? this.payrollService.update({
                  ...this.record,
                  ...payload,
                  id: this.payrollId,
                  netSalary: this.netSalaryPreview,
              })
            : this.payrollService.create(payload);

        request
            .then(() => {
                this.messageService.add({
                    severity: 'success',
                    summary: 'Success',
                    detail: this.payrollId
                        ? 'Payroll updated successfully'
                        : 'Payroll created successfully',
                });
                this.saved.emit();
                this.onHide();
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to save payroll',
                });
            })
            .finally(() => {
                this.saving = false;
            });
    }

    private emptyRecord(): StaffPayrollDto {
        const now = new Date();
        return {
            id: 0,
            branchId: 0,
            staffId: null as any,
            month: now.getMonth() + 1,
            year: now.getFullYear(),
            basicSalary: 0,
            allowance: 0,
            bonus: 0,
            deduction: 0,
            overtimeAmount: 0,
            netSalary: 0,
            paymentStatus: PayrollPaymentStatus.Pending,
            paymentDate: null,
            remarks: '',
        };
    }

    private resetForm(): void {
        this.record = this.emptyRecord();
        this.saving = false;
        this.loading = false;
    }

    private loadStaffOptions(): void {
        this.staffService
            .getAll({ isActive: true, skipCount: 0, maxResultCount: 1000 })
            .then((result) => {
                this.staffOptions = result.items || [];
            })
            .catch(() => {
                this.staffOptions = [];
            });
    }

    private loadRecord(id: number): void {
        this.loading = true;
        this.payrollService
            .get(id)
            .then((record) => {
                this.record = {
                    ...record,
                    paymentDate: this.toDateInputValue(record.paymentDate),
                };
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to load payroll',
                });
                this.onHide();
            })
            .finally(() => {
                this.loading = false;
            });
    }

    private toDateInputValue(value?: string | Date | null): string | null {
        if (!value) {
            return null;
        }
        const date = new Date(value);
        if (isNaN(date.getTime())) {
            return null;
        }
        const year = date.getFullYear();
        const month = `${date.getMonth() + 1}`.padStart(2, '0');
        const day = `${date.getDate()}`.padStart(2, '0');
        return `${year}-${month}-${day}`;
    }
}
