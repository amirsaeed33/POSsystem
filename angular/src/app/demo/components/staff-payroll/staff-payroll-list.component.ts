import { Component, Input, OnDestroy, OnInit } from '@angular/core';
import { ConfirmationService, MessageService } from 'primeng/api';
import { Table } from 'primeng/table';
import { Subscription } from 'rxjs';
import { BusinessAccountDto } from 'src/app/demo/api/business-account';
import { PermissionNames } from 'src/app/demo/api/permission-names';
import { StaffDto } from 'src/app/demo/api/staff';
import {
    PayrollPaymentStatus,
    StaffPayrollDto,
} from 'src/app/demo/api/staff-payroll';
import { BranchContextService } from 'src/app/demo/service/branch-context.service';
import { AppFormatService } from 'src/app/demo/service/app-format.service';
import { BusinessAccountService } from 'src/app/demo/service/business-account.service';
import { PermissionService } from 'src/app/demo/service/permission.service';
import { StaffPayrollService } from 'src/app/demo/service/staff-payroll.service';
import { StaffService } from 'src/app/demo/service/staff.service';
import { RowActionItem } from 'src/app/shared/components/row-actions/row-actions.component';

@Component({
    selector: 'app-staff-payroll-list',
    templateUrl: './staff-payroll-list.component.html',
    providers: [MessageService, ConfirmationService],
})
export class StaffPayrollListComponent implements OnInit, OnDestroy {
    /** When true, omit outer card wrapper (used inside Staff hub tabs). */
    @Input() embedded = false;

    records: StaffPayrollDto[] = [];
    staffOptions: StaffDto[] = [];
    loading = false;
    totalRecords = 0;
    keyword = '';
    staffFilter: number | null = null;
    monthFilter: number | null = null;
    yearFilter: number | null = new Date().getFullYear();
    paymentStatusFilter: PayrollPaymentStatus | null = null;
    private branchSub?: Subscription;

    getRowActions(row: StaffPayrollDto): RowActionItem[] {
        const actions: RowActionItem[] = [];
        if (row.paymentStatus === 0 && this.canEdit) {
            actions.push({
                label: 'Pay salary',
                icon: 'pi pi-dollar',
                color: 'green',
                action: () => this.openPayDialog(row),
            });
        }
        if (this.canEdit) {
            actions.push({
                label: 'Edit',
                icon: 'pi pi-pencil',
                color: 'blue',
                action: () => this.openEditDialog(row),
            });
        }
        if (this.canDelete) {
            actions.push({
                label: 'Delete',
                icon: 'pi pi-trash',
                color: 'red',
                action: () => this.onDelete(row),
            });
        }
        return actions;
    }

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

    // Payment confirmation dialog state
    payDialogVisible = false;
    selectedPayrollForPay: StaffPayrollDto | null = null;
    payRemarks = '';
    selectedAccountId: number | null = null;
    paymentAccounts: BusinessAccountDto[] = [];
    payLoading = false;

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
        private businessAccountService: BusinessAccountService,
        private permissionService: PermissionService,
        private branchContext: BranchContextService,
        public formatService: AppFormatService,
        private messageService: MessageService,
        private confirmationService: ConfirmationService
    ) {}

    ngOnInit(): void {
        this.loadStaffOptions();
        this.loadPaymentAccounts();
        this.loadRecords();

        this.branchSub = this.branchContext.currentBranch$.subscribe((branch) => {
            if (branch) {
                this.loadStaffOptions();
                this.loadRecords();
            }
        });
    }

    ngOnDestroy(): void {
        this.branchSub?.unsubscribe();
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

    openPayDialog(record: StaffPayrollDto): void {
        this.selectedPayrollForPay = record;
        this.payRemarks = '';
        if (this.paymentAccounts.length > 0) {
            this.selectedAccountId = this.paymentAccounts[0].id;
        } else {
            this.selectedAccountId = null;
        }
        this.payDialogVisible = true;
    }

    onPayHide(): void {
        if (this.payLoading) {
            return;
        }
        this.payDialogVisible = false;
        this.selectedPayrollForPay = null;
        this.payRemarks = '';
    }

    submitPay(): void {
        if (!this.selectedPayrollForPay) {
            return;
        }

        this.payLoading = true;
        this.payrollService
            .processPayment({
                payrollId: this.selectedPayrollForPay.id,
                paymentAccountId: this.selectedAccountId,
                remarks: this.payRemarks,
            })
            .then(() => {
                this.messageService.add({
                    severity: 'success',
                    summary: 'Success',
                    detail: 'Salary payment processed successfully.',
                });
                this.payDialogVisible = false;
                this.selectedPayrollForPay = null;
                this.payRemarks = '';
                this.loadRecords();
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Payment Error',
                    detail: error?.message || 'Failed to process salary payment.',
                });
            })
            .finally(() => {
                this.payLoading = false;
            });
    }

    private readonly excludedAccountTypes = [
        'Purchase',
        'Sale',
        'Expense',
        'Customer',
        'Supplier',
    ];

    private loadPaymentAccounts(): void {
        this.businessAccountService
            .getAll({ skipCount: 0, maxResultCount: 100 })
            .then((result) => {
                const items = result.items || [];
                let accounts = items.filter(
                    (a) =>
                        a.isActive !== false &&
                        this.excludedAccountTypes.indexOf(a.accountType || '') < 0 &&
                        !a.code?.startsWith('SYS-EXPENSE') &&
                        !a.code?.startsWith('SYS-PURCHASE') &&
                        !a.code?.startsWith('SYS-SALE')
                );
                if (!accounts.length) {
                    accounts = items.filter((a) => a.isActive !== false);
                }
                this.paymentAccounts = accounts;
                if (this.paymentAccounts.length > 0 && !this.selectedAccountId) {
                    this.selectedAccountId = this.paymentAccounts[0].id;
                }
            })
            .catch(() => {
                this.paymentAccounts = [];
            });
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
        return this.formatService.formatDate(value);
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
