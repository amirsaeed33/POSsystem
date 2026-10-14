import { Component, Input, OnInit } from '@angular/core';
import { Table } from 'primeng/table';
import { ConfirmationService, MessageService } from 'primeng/api';
import { StaffDto } from 'src/app/demo/api/staff';
import { StaffService } from 'src/app/demo/service/staff.service';
import {
    PayrollPaymentStatus,
    StaffPayrollDto,
} from 'src/app/demo/api/staff-payroll';
import { StaffPayrollService } from 'src/app/demo/service/staff-payroll.service';

@Component({
    selector: 'app-staff-list',
    templateUrl: './staff-list.component.html',
    providers: [MessageService, ConfirmationService],
    styles: [
        `
            :host ::ng-deep .staff-table .p-datatable-wrapper {
                min-height: 260px;
            }
        `,
    ],
})
export class StaffListComponent implements OnInit {
    /** When true, omit outer card wrapper (used inside Staff hub tabs). */
    @Input() embedded = false;

    staffList: StaffDto[] = [];
    loading = false;
    totalRecords = 0;
    keyword = '';
    statusFilter: boolean | null = null;

    statusOptions = [
        { label: 'All', value: null },
        { label: 'Active', value: true },
        { label: 'Inactive', value: false },
    ];

    dialogVisible = false;
    editingStaffId: number | null = null;

    loginDialogVisible = false;
    loginStaff: StaffDto | null = null;

    // Payroll action state
    checkingPayroll = false;
    selectedStaffForPayroll: StaffDto | null = null;
    existingPayroll: StaffPayrollDto | null = null;
    payrollInfoDialogVisible = false;
    createPayrollDialogVisible = false;
    isEditingPayroll = false;
    savingPayroll = false;

    payrollFormRecord = this.emptyPayrollRecord();

    monthNames = [
        'January',
        'February',
        'March',
        'April',
        'May',
        'June',
        'July',
        'August',
        'September',
        'October',
        'November',
        'December',
    ];

    monthOptions = Array.from({ length: 12 }, (_, i) => ({
        label: this.monthNames[i],
        value: i + 1,
    }));

    paymentStatusOptions = [
        { label: 'Pending', value: PayrollPaymentStatus.Pending },
        { label: 'Paid', value: PayrollPaymentStatus.Paid },
        { label: 'Cancelled', value: PayrollPaymentStatus.Cancelled },
    ];

    constructor(
        private staffService: StaffService,
        private staffPayrollService: StaffPayrollService,
        private messageService: MessageService,
        private confirmationService: ConfirmationService
    ) {}

    ngOnInit(): void {
        this.loadStaff();
    }

    loadStaff(): void {
        this.loading = true;
        this.staffService
            .getAll({
                keyword: this.keyword?.trim() || undefined,
                isActive: this.statusFilter,
                skipCount: 0,
                maxResultCount: 1000,
            })
            .then((result) => {
                this.staffList = result.items;
                this.totalRecords = result.totalCount;
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to load staff',
                });
            })
            .finally(() => {
                this.loading = false;
            });
    }

    onSearch(): void {
        this.loadStaff();
    }

    onGlobalFilter(table: Table, event: Event): void {
        table.filterGlobal((event.target as HTMLInputElement).value, 'contains');
    }

    openCreateDialog(): void {
        this.editingStaffId = null;
        this.dialogVisible = true;
    }

    openEditDialog(staff: StaffDto): void {
        this.editingStaffId = staff.id;
        this.dialogVisible = true;
    }

    onDialogSaved(): void {
        this.loadStaff();
    }

    openLoginDialog(staff: StaffDto): void {
        this.loginStaff = staff;
        this.loginDialogVisible = true;
    }

    onLoginSaved(): void {
        this.loadStaff();
    }

    formatDate(value: string | Date | undefined): string {
        if (!value) {
            return '—';
        }
        const date = new Date(value);
        if (isNaN(date.getTime())) {
            return '—';
        }
        return date.toLocaleDateString();
    }

    onDelete(staff: StaffDto): void {
        this.confirmationService.confirm({
            message: `Are you sure you want to delete staff "${staff.name}"?`,
            header: 'Delete Confirmation',
            icon: 'pi pi-exclamation-triangle',
            acceptButtonStyleClass: 'p-button-danger',
            accept: () => {
                this.staffService
                    .delete(staff.id)
                    .then(() => {
                        this.messageService.add({
                            severity: 'success',
                            summary: 'Success',
                            detail: 'Staff deleted successfully',
                        });
                        this.loadStaff();
                    })
                    .catch((error) => {
                        this.messageService.add({
                            severity: 'error',
                            summary: 'Error',
                            detail: error?.message || 'Failed to delete staff',
                        });
                    });
            },
        });
    }

    get netSalaryPreview(): number {
        return (
            (this.payrollFormRecord.basicSalary || 0) +
            (this.payrollFormRecord.allowance || 0) +
            (this.payrollFormRecord.bonus || 0) +
            (this.payrollFormRecord.overtimeAmount || 0) -
            (this.payrollFormRecord.deduction || 0)
        );
    }

    getMonthName(monthNum: number): string {
        if (monthNum >= 1 && monthNum <= 12) {
            return this.monthNames[monthNum - 1];
        }
        return '';
    }

    getPaymentStatusLabel(status: PayrollPaymentStatus | undefined): string {
        switch (status) {
            case PayrollPaymentStatus.Paid:
                return 'Paid';
            case PayrollPaymentStatus.Pending:
                return 'Pending';
            case PayrollPaymentStatus.Cancelled:
                return 'Cancelled';
            default:
                return '—';
        }
    }

    getPaymentStatusSeverity(status: PayrollPaymentStatus | undefined): string {
        switch (status) {
            case PayrollPaymentStatus.Paid:
                return 'success';
            case PayrollPaymentStatus.Pending:
                return 'warning';
            case PayrollPaymentStatus.Cancelled:
                return 'danger';
            default:
                return 'info';
        }
    }

    emptyPayrollRecord() {
        const now = new Date();
        return {
            id: undefined as number | undefined,
            branchId: undefined as number | undefined,
            staffId: 0,
            month: now.getMonth() + 1,
            year: now.getFullYear(),
            basicSalary: 0,
            allowance: 0,
            bonus: 0,
            deduction: 0,
            overtimeAmount: 0,
            paymentStatus: PayrollPaymentStatus.Pending,
            paymentDate: null as string | null,
            remarks: '',
        };
    }

    formatDateForInput(dateVal: any): string {
        if (!dateVal) return '';
        const d = new Date(dateVal);
        if (isNaN(d.getTime())) return '';
        const year = d.getFullYear();
        const month = String(d.getMonth() + 1).padStart(2, '0');
        const day = String(d.getDate()).padStart(2, '0');
        return `${year}-${month}-${day}`;
    }

    async onCreatePayroll(staff: StaffDto): Promise<void> {
        this.selectedStaffForPayroll = staff;
        const now = new Date();
        const currentMonth = now.getMonth() + 1;
        const currentYear = now.getFullYear();

        this.checkingPayroll = true;
        try {
            const result = await this.staffPayrollService.getAll({
                staffId: staff.id,
                month: currentMonth,
                year: currentYear,
                maxResultCount: 1,
            });

            if (result && result.items && result.items.length > 0) {
                // Payroll already exists for this staff for the current month!
                this.existingPayroll = result.items[0];
                this.payrollInfoDialogVisible = true;
            } else {
                // Does not exist -> Show popup with basic information
                this.isEditingPayroll = false;
                this.payrollFormRecord = {
                    id: undefined,
                    branchId: undefined,
                    staffId: staff.id,
                    month: currentMonth,
                    year: currentYear,
                    basicSalary: staff.basicSalary || 0,
                    allowance: 0,
                    bonus: 0,
                    deduction: 0,
                    overtimeAmount: 0,
                    paymentStatus: PayrollPaymentStatus.Pending,
                    paymentDate: null,
                    remarks: '',
                };
                this.createPayrollDialogVisible = true;
            }
        } catch (error: any) {
            this.messageService.add({
                severity: 'error',
                summary: 'Error',
                detail: error?.message || 'Failed to check payroll status',
            });
        } finally {
            this.checkingPayroll = false;
        }
    }

    openEditExistingPayroll(): void {
        if (!this.existingPayroll) {
            return;
        }
        this.payrollInfoDialogVisible = false;
        this.isEditingPayroll = true;
        this.payrollFormRecord = {
            id: this.existingPayroll.id,
            branchId: this.existingPayroll.branchId,
            staffId: this.existingPayroll.staffId,
            month: this.existingPayroll.month,
            year: this.existingPayroll.year,
            basicSalary: this.existingPayroll.basicSalary || 0,
            allowance: this.existingPayroll.allowance || 0,
            bonus: this.existingPayroll.bonus || 0,
            deduction: this.existingPayroll.deduction || 0,
            overtimeAmount: this.existingPayroll.overtimeAmount || 0,
            paymentStatus: this.existingPayroll.paymentStatus,
            paymentDate: this.existingPayroll.paymentDate
                ? this.formatDateForInput(this.existingPayroll.paymentDate)
                : null,
            remarks: this.existingPayroll.remarks || '',
        };
        this.createPayrollDialogVisible = true;
    }

    onPaymentStatusChange(): void {
        if (
            this.payrollFormRecord.paymentStatus === PayrollPaymentStatus.Paid &&
            !this.payrollFormRecord.paymentDate
        ) {
            const today = new Date();
            this.payrollFormRecord.paymentDate = this.formatDateForInput(today);
        }
    }

    async savePayroll(): Promise<void> {
        if (!this.payrollFormRecord.staffId) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Validation',
                detail: 'Staff is required',
            });
            return;
        }

        if (
            (this.payrollFormRecord.basicSalary || 0) < 0 ||
            (this.payrollFormRecord.allowance || 0) < 0 ||
            (this.payrollFormRecord.bonus || 0) < 0 ||
            (this.payrollFormRecord.deduction || 0) < 0 ||
            (this.payrollFormRecord.overtimeAmount || 0) < 0
        ) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Validation',
                detail: 'Salary values cannot be negative',
            });
            return;
        }

        this.savingPayroll = true;
        const payload = {
            staffId: this.payrollFormRecord.staffId,
            month: this.payrollFormRecord.month,
            year: this.payrollFormRecord.year,
            basicSalary: this.payrollFormRecord.basicSalary || 0,
            allowance: this.payrollFormRecord.allowance || 0,
            bonus: this.payrollFormRecord.bonus || 0,
            deduction: this.payrollFormRecord.deduction || 0,
            overtimeAmount: this.payrollFormRecord.overtimeAmount || 0,
            paymentStatus: this.payrollFormRecord.paymentStatus,
            paymentDate: this.payrollFormRecord.paymentDate || null,
            remarks: this.payrollFormRecord.remarks?.trim() || undefined,
        };

        try {
            if (this.isEditingPayroll && this.payrollFormRecord.id) {
                await this.staffPayrollService.update({
                    id: this.payrollFormRecord.id,
                    branchId: this.payrollFormRecord.branchId || 0,
                    ...payload,
                    netSalary: this.netSalaryPreview,
                });
                this.messageService.add({
                    severity: 'success',
                    summary: 'Success',
                    detail: `Payroll updated successfully for ${this.selectedStaffForPayroll?.name || 'staff'}`,
                });
            } else {
                await this.staffPayrollService.create(payload);
                this.messageService.add({
                    severity: 'success',
                    summary: 'Success',
                    detail: `Payroll created successfully for ${this.selectedStaffForPayroll?.name || 'staff'}`,
                });
            }
            this.createPayrollDialogVisible = false;
        } catch (error: any) {
            this.messageService.add({
                severity: 'error',
                summary: 'Error',
                detail: error?.message || 'Failed to save payroll',
            });
        } finally {
            this.savingPayroll = false;
        }
    }
}

