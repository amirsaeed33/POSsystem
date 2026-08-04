import { Component, OnInit } from '@angular/core';
import { ConfirmationService, MessageService } from 'primeng/api';
import { Table } from 'primeng/table';
import { PermissionNames } from 'src/app/demo/api/permission-names';
import { StaffDto } from 'src/app/demo/api/staff';
import {
    AttendanceStatus,
    StaffAttendanceDto,
} from 'src/app/demo/api/staff-attendance';
import { PermissionService } from 'src/app/demo/service/permission.service';
import { StaffAttendanceService } from 'src/app/demo/service/staff-attendance.service';
import { StaffService } from 'src/app/demo/service/staff.service';

@Component({
    templateUrl: './staff-attendance-list.component.html',
    providers: [MessageService, ConfirmationService],
})
export class StaffAttendanceListComponent implements OnInit {
    records: StaffAttendanceDto[] = [];
    staffOptions: StaffDto[] = [];
    loading = false;
    totalRecords = 0;
    keyword = '';
    staffFilter: number | null = null;
    statusFilter: AttendanceStatus | null = null;
    fromDate: string | null = null;
    toDate: string | null = null;

    statusOptions = [
        { label: 'All', value: null },
        { label: 'Present', value: AttendanceStatus.Present },
        { label: 'Absent', value: AttendanceStatus.Absent },
        { label: 'Leave', value: AttendanceStatus.Leave },
        { label: 'Half Day', value: AttendanceStatus.HalfDay },
    ];

    dialogVisible = false;
    editingId: number | null = null;

    readonly canCreate =
        this.permissionService.isGranted(PermissionNames.StaffAttendanceCreate) ||
        this.permissionService.isGranted(PermissionNames.StaffAttendance);
    readonly canEdit =
        this.permissionService.isGranted(PermissionNames.StaffAttendanceEdit) ||
        this.permissionService.isGranted(PermissionNames.StaffAttendance);
    readonly canDelete =
        this.permissionService.isGranted(PermissionNames.StaffAttendanceDelete) ||
        this.permissionService.isGranted(PermissionNames.StaffAttendance);

    constructor(
        private attendanceService: StaffAttendanceService,
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
        this.attendanceService
            .getAll({
                keyword: this.keyword?.trim() || undefined,
                staffId: this.staffFilter,
                status: this.statusFilter,
                fromDate: this.fromDate || undefined,
                toDate: this.toDate || undefined,
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
                    detail: error?.message || 'Failed to load attendance',
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

    openEditDialog(record: StaffAttendanceDto): void {
        this.editingId = record.id;
        this.dialogVisible = true;
    }

    onDialogSaved(): void {
        this.loadRecords();
    }

    onDelete(record: StaffAttendanceDto): void {
        this.confirmationService.confirm({
            message: `Delete attendance for ${record.staffName || 'staff'} on ${this.formatDate(record.attendanceDate)}?`,
            header: 'Confirm',
            icon: 'pi pi-exclamation-triangle',
            accept: () => {
                this.attendanceService
                    .delete(record.id)
                    .then(() => {
                        this.messageService.add({
                            severity: 'success',
                            summary: 'Success',
                            detail: 'Attendance deleted',
                        });
                        this.loadRecords();
                    })
                    .catch((error) => {
                        this.messageService.add({
                            severity: 'error',
                            summary: 'Error',
                            detail: error?.message || 'Failed to delete attendance',
                        });
                    });
            },
        });
    }

    statusLabel(status: AttendanceStatus): string {
        switch (status) {
            case AttendanceStatus.Present:
                return 'Present';
            case AttendanceStatus.Absent:
                return 'Absent';
            case AttendanceStatus.Leave:
                return 'Leave';
            case AttendanceStatus.HalfDay:
                return 'Half Day';
            default:
                return '—';
        }
    }

    statusSeverity(status: AttendanceStatus): string {
        switch (status) {
            case AttendanceStatus.Present:
                return 'success';
            case AttendanceStatus.Absent:
                return 'danger';
            case AttendanceStatus.Leave:
                return 'warning';
            case AttendanceStatus.HalfDay:
                return 'info';
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

    formatTime(value?: string | Date | null): string {
        if (!value) {
            return '—';
        }
        const date = new Date(value);
        if (isNaN(date.getTime())) {
            return '—';
        }
        return date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
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
