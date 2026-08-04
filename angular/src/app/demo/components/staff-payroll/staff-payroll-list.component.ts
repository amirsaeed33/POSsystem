import { Component, OnInit } from '@angular/core';
import { ConfirmationService, MessageService } from 'primeng/api';
import { Table } from 'primeng/table';
import { PermissionNames } from 'src/app/demo/api/permission-names';
import { StaffDto } from 'src/app/demo/api/staff';
import {
    PayrollPaymentStatus,
    StaffPayrollDto,
} from 'src/app/demo/api/staff-payroll';
import { PermissionService } from 'src/app/demo/service/permission.service';
import { StaffPayrollService } from 'src/app/demo/service/staff-payroll.service';
import { StaffService } from 'src/app/demo/service/staff.service';

@Component({
    templateUrl: './staff-payroll-list.component.html',
    providers: [MessageService, ConfirmationService],
})
export class StaffPayrollListComponent implements OnInit {
    records: StaffPayrollDto[] = [];
    staffOptions: StaffDto[] = [];
    loading = false;
    totalRecords = 0;
    keyword = '';
    staffFilter: number | null = null;
    monthFilter: number | null = null;
    yearFilter: number | null = new Date().getFullYear();
    paymentStatusFilter: PayrollPaymentStatus | null = null;

    monthOptions = [
        { label: 'All', value: null },
        ...Array.from({ length: 12 }, (_, i) => ({
            label: new Date(2000, i, 1).toLocaleString('default', { month: 'long' }),
            value: i + 1,
        })),
    ];

    paymentStatusOptions = [
        { label: 'All', value: null },
        { label: 'Pending', value: PayrollPaymentStatus.Pending },
        { label: 'Paid', value: PayrollPaymentStatus.Paid },
        { label: 'Cancelled', value: PayrollPaymentStatus.Cancelled },
    ];

    dialogVisible = false;
    editingId: number | null = null;

    readonly canCreate =
        this.permissionService.isGranted(PermissionNames.StaffPayrollCreate) ||
        this.permissionService.isGranted(PermissionNames.StaffPayroll);
    readonly canEdit =
        this.permissionService.isGranted(PermissionNames.StaffPayrollEdit) ||
        this.permissionService.isGranted(PermissionNames.StaffPayroll);
    readonly canDelete =
        this.permissionService.isGranted(PermissionNames.StaffPayrollDelete) ||
        this.permissionService.isGranted(PermissionNames.StaffPayroll);

    constructor(
        private payrollService: StaffPayrollService,
        private staffService: StaffService,
        private permissionService: PermissionService,
        private messageService: MessageService,
        private confirmationService: ConfirmationService
    ) {}

    ngOnInit(): void {
        this.loadStaffOptions();
        this.loadRecords();
    }

    loadRecords(): void {
        this.loading = true;
        this.payrollService
            .getAll({
                keyword: this.keyword?.trim() || undefined,
                staffId: this.staffFilter,
                month: this.monthFilter,
                year: this.yearFilter,
                paymentStatus: this.paymentStatusFilter,
                skipCount: 0,
                maxResultCount: 1000,
            })
            .then((result) => {
                this.records = result.items;
                this.totalRecords = result.totalCount;
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to load payroll',
                });
            })
            .finally(() => {
                this.loading = false;
            });
    }

    onSearch(): void {
        this.loadRecords();
    }

    onGlobalFilter(table: Table, event: Event): void {
        table.filterGlobal((event.target as HTMLInputElement).value, 'contains');
    }

    openCreateDialog(): void {
        this.editingId = null;
        this.dialogVisible = true;
    }

    openEditDialog(record: StaffPayrollDto): void {
        this.editingId = record.id;
        this.dialogVisible = true;
    }

    onDialogSaved(): void {
        this.loadRecords();
    }

    onDelete(record: StaffPayrollDto): void {
        this.confirmationService.confirm({
            message: `Delete payroll for ${record.staffName || 'staff'} (${record.month}/${record.year})?`,
            header: 'Confirm',
            icon: 'pi pi-exclamation-triangle',
            accept: () => {
                this.payrollService
                    .delete(record.id)
                    .then(() => {
                        this.messageService.add({
                            severity: 'success',
                            summary: 'Success',
                            detail: 'Payroll deleted',
                        });
                        this.loadRecords();
                    })
                    .catch((error) => {
                        this.messageService.add({
                            severity: 'error',
                            summary: 'Error',
                            detail: error?.message || 'Failed to delete payroll',
                        });
                    });
            },
        });
    }

    monthLabel(month: number): string {
        if (!month || month < 1 || month > 12) {
            return '—';
        }
        return new Date(2000, month - 1, 1).toLocaleString('default', {
            month: 'short',
        });
    }

    paymentStatusLabel(status: PayrollPaymentStatus): string {
        switch (status) {
            case PayrollPaymentStatus.Pending:
                return 'Pending';
            case PayrollPaymentStatus.Paid:
                return 'Paid';
            case PayrollPaymentStatus.Cancelled:
                return 'Cancelled';
            default:
                return '—';
        }
    }

    paymentStatusSeverity(status: PayrollPaymentStatus): string {
        switch (status) {
            case PayrollPaymentStatus.Pending:
                return 'warning';
            case PayrollPaymentStatus.Paid:
                return 'success';
            case PayrollPaymentStatus.Cancelled:
                return 'danger';
            default:
                return 'secondary';
        }
    }

    formatDate(value?: string | Date | null): string {
        if (!value) {
            return '—';
        }
        const date = new Date(value);
        if (isNaN(date.getTime())) {
            return '—';
        }
        return date.toLocaleDateString();
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
}
