import { AppFormatService } from 'src/app/demo/service/app-format.service';
import { Component, Input, OnInit } from '@angular/core';
import { ConfirmationService, MessageService } from 'primeng/api';
import { Table } from 'primeng/table';
import { PermissionNames } from 'src/app/demo/api/permission-names';
import {
    AttendanceStatus,
    DailyAttendanceSheetItemDto,
    StaffAttendanceDto,
} from 'src/app/demo/api/staff-attendance';
import { PermissionService } from 'src/app/demo/service/permission.service';
import { StaffAttendanceService } from 'src/app/demo/service/staff-attendance.service';
import { RowActionItem } from 'src/app/shared/components/row-actions/row-actions.component';

export interface StaffAttendanceRow {
    staffId: number;
    staffName: string;
    branchName?: string;
    attendanceDate: string;
    attendance?: StaffAttendanceDto | null;
}

@Component({
    selector: 'app-staff-attendance-list',
    templateUrl: './staff-attendance-list.component.html',
    providers: [MessageService, ConfirmationService],
})
export class StaffAttendanceListComponent implements OnInit {
    /** When true, omit outer card wrapper (used inside Staff hub tabs). */
    @Input() embedded = false;

    rows: StaffAttendanceRow[] = [];
    staffOptions: { id: number; name: string }[] = [];
    loading = false;
    totalRecords = 0;
    keyword = '';
    staffFilter: number | null = null;
    statusFilter: AttendanceStatus | null = null;
    attendanceDate: string = this.toDateInputValue();

    dialogVisible = false;
    editingId: number | null = null;
    dialogStaffId: number | null = null;
    dialogDate: string | null = null;

    statusOptions = [
        { label: 'All', value: null },
        { label: 'Present', value: AttendanceStatus.Present },
        { label: 'Absent', value: AttendanceStatus.Absent },
        { label: 'Leave', value: AttendanceStatus.Leave },
        { label: 'Half Day', value: AttendanceStatus.HalfDay },
        { label: 'Not marked', value: -1 as any },
    ];

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
        public formatService: AppFormatService,
        private attendanceService: StaffAttendanceService,
        private permissionService: PermissionService,
        private messageService: MessageService,
        private confirmationService: ConfirmationService
    ) {}

    ngOnInit(): void {
        this.loadAllEmployees();
    }

    getRowActions(row: StaffAttendanceRow): RowActionItem[] {
        const actions: RowActionItem[] = [];
        const att = row.attendance;

        if (!att) {
            if (this.canCreate) {
                actions.push({
                    label: 'Mark',
                    icon: 'pi pi-check',
                    color: 'blue',
                    action: () => this.openMarkDialog(row),
                });
            }
            return actions;
        }

        if (this.canEdit) {
            actions.push({
                label: 'Edit',
                icon: 'pi pi-pencil',
                color: 'blue',
                action: () => this.openEditDialog(att),
            });
        }
        if (this.canDelete) {
            actions.push({
                label: 'Delete',
                icon: 'pi pi-trash',
                color: 'red',
                action: () => this.onDelete(att),
            });
        }
        return actions;
    }

    loadAllEmployees(): void {
        this.loading = true;
        const date = this.attendanceDate || this.toDateInputValue();

        this.attendanceService
            .getDailySheet({
                date,
                staffId: this.staffFilter,
                keyword: this.keyword?.trim() || undefined,
            })
            .then((items) => {
                this.staffOptions = (items || []).map((x) => ({
                    id: x.staffId,
                    name: x.staffName || `Staff #${x.staffId}`,
                }));

                let rows = (items || []).map((item) =>
                    this.toRow(item, date)
                );

                if (this.statusFilter === (-1 as any)) {
                    rows = rows.filter((r) => !r.attendance);
                } else if (this.statusFilter != null) {
                    rows = rows.filter(
                        (r) =>
                            r.attendance != null &&
                            Number(r.attendance.status) ===
                                Number(this.statusFilter)
                    );
                }

                this.rows = rows;
                this.totalRecords = rows.length;
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to load attendance',
                });
                this.rows = [];
                this.totalRecords = 0;
            })
            .finally(() => {
                this.loading = false;
            });
    }

    onSearch(): void {
        this.loadAllEmployees();
    }

    onGlobalFilter(table: Table, event: Event): void {
        table.filterGlobal((event.target as HTMLInputElement).value, 'contains');
    }

    openCreateDialog(): void {
        this.editingId = null;
        this.dialogStaffId = null;
        this.dialogDate = this.attendanceDate;
        this.dialogVisible = true;
    }

    openMarkDialog(row: StaffAttendanceRow): void {
        this.editingId = null;
        this.dialogStaffId = row.staffId;
        this.dialogDate = row.attendanceDate;
        this.dialogVisible = true;
    }

    openEditDialog(record: StaffAttendanceDto): void {
        this.editingId = record.id;
        this.dialogStaffId = record.staffId;
        this.dialogDate = this.toDateInputValue(record.attendanceDate);
        this.dialogVisible = true;
    }

    onDialogSaved(): void {
        this.loadAllEmployees();
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
                        this.loadAllEmployees();
                    })
                    .catch((error) => {
                        this.messageService.add({
                            severity: 'error',
                            summary: 'Error',
                            detail:
                                error?.message || 'Failed to delete attendance',
                        });
                    });
            },
        });
    }

    statusLabel(status?: AttendanceStatus | null): string {
        if (status === null || status === undefined) {
            return 'Not marked';
        }
        switch (Number(status)) {
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

    formatDate(value?: string | Date | null): string {
        return this.formatService.formatDate(value);
    }

    formatTime(value?: string | Date | null): string {
        if (!value) {
            return '—';
        }
        const date = new Date(value);
        if (isNaN(date.getTime())) {
            return '—';
        }
        return date.toLocaleTimeString([], {
            hour: '2-digit',
            minute: '2-digit',
        });
    }

    private toRow(
        item: DailyAttendanceSheetItemDto,
        date: string
    ): StaffAttendanceRow {
        const attendance =
            item.hasAttendance && item.attendanceId
                ? ({
                      id: Number(item.attendanceId),
                      branchId: Number(item.branchId || 0),
                      branchName: item.branchName,
                      staffId: Number(item.staffId),
                      staffName: item.staffName,
                      attendanceDate: item.attendanceDate || date,
                      checkInTime: item.checkInTime ?? null,
                      checkOutTime: item.checkOutTime ?? null,
                      status: Number(item.status ?? 0) as AttendanceStatus,
                      workingHours: item.workingHours ?? null,
                      remarks: item.remarks,
                  } as StaffAttendanceDto)
                : null;

        return {
            staffId: Number(item.staffId),
            staffName: item.staffName || '—',
            branchName: item.branchName,
            attendanceDate: date,
            attendance,
        };
    }

    private toDateInputValue(value?: string | Date | null): string {
        if (typeof value === 'string' && /^\d{4}-\d{2}-\d{2}$/.test(value.trim())) {
            return value.trim();
        }
        if (typeof value === 'string') {
            const match = value.trim().match(/^(\d{4}-\d{2}-\d{2})/);
            if (match && !/[zZ]|[+-]\d{2}:\d{2}/.test(value)) {
                return match[1];
            }
        }
        const date = value ? new Date(value) : new Date();
        if (isNaN(date.getTime())) {
            const now = new Date();
            return `${now.getFullYear()}-${`${now.getMonth() + 1}`.padStart(2, '0')}-${`${now.getDate()}`.padStart(2, '0')}`;
        }
        return `${date.getFullYear()}-${`${date.getMonth() + 1}`.padStart(2, '0')}-${`${date.getDate()}`.padStart(2, '0')}`;
    }
}
