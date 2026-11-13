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
    styles: [
        `
            :host ::ng-deep .attendance-punch.p-button {
                width: 2rem;
                height: 2rem;
                padding: 0;
                display: inline-flex;
                align-items: center;
                justify-content: center;
            }
            :host ::ng-deep .attendance-punch svg {
                width: 15px;
                height: 15px;
                display: block;
                fill: none;
                stroke: currentColor;
                stroke-width: 2;
                stroke-linecap: round;
                stroke-linejoin: round;
            }
            :host ::ng-deep .attendance-punch--in.p-button {
                color: #15803d;
                border-color: #86efac;
                background: #f0fdf4;
            }
            :host ::ng-deep .attendance-punch--out.p-button {
                color: #c2410c;
                border-color: #fdba74;
                background: #fff7ed;
            }
        `,
    ],
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

    punchVisible = false;
    punchSaving = false;
    punchKind: 'in' | 'out' = 'in';
    punchTime = '';
    punchRow: StaffAttendanceRow | null = null;

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

        if (!att?.checkInTime && this.canCreate) {
            actions.push({
                label: 'Check In',
                icon: 'pi pi-check-circle',
                color: 'blue',
                action: () => this.openPunch(row, 'in'),
            });
        } else if (att?.checkInTime && this.canEdit) {
            actions.push({
                label: 'Check In',
                icon: 'pi pi-check-circle',
                color: 'blue',
                action: () => this.openPunch(row, 'in'),
            });
        }

        if (att?.checkInTime && !att.checkOutTime && this.canPunchOut(row)) {
            actions.push({
                label: 'Check Out',
                icon: 'pi pi-arrow-up-right',
                color: 'blue',
                action: () => this.openPunch(row, 'out'),
            });
        } else if (att?.checkOutTime && this.canEdit) {
            actions.push({
                label: 'Check Out',
                icon: 'pi pi-arrow-up-right',
                color: 'blue',
                action: () => this.openPunch(row, 'out'),
            });
        }

        if (att && this.canDelete) {
            actions.push({
                label: 'Delete',
                icon: 'pi pi-trash',
                color: 'red',
                action: () => this.onDelete(att),
            });
        }
        return actions;
    }

    canPunchOut(row: StaffAttendanceRow): boolean {
        if (!row.attendance?.checkInTime) {
            return false;
        }
        return row.attendance.checkOutTime ? this.canEdit : this.canEdit || this.canCreate;
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

    openPunch(row: StaffAttendanceRow, kind: 'in' | 'out'): void {
        if (kind === 'out' && !row.attendance?.checkInTime) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Check in first',
                detail: `${row.staffName} has no check-in time yet.`,
            });
            return;
        }

        this.punchRow = row;
        this.punchKind = kind;
        const existing =
            kind === 'in'
                ? row.attendance?.checkInTime
                : row.attendance?.checkOutTime;
        this.punchTime = this.toTimeInputValue(existing) || this.nowTime();
        this.punchVisible = true;
    }

    savePunch(): void {
        const row = this.punchRow;
        if (!row || !this.punchTime) {
            return;
        }

        const date = this.toDateInputValue(row.attendanceDate);
        const timeIso = this.toDateTimeIso(date, this.punchTime);
        const existing = row.attendance;
        this.punchSaving = true;

        const request = existing
            ? this.attendanceService.update({
                  ...existing,
                  attendanceDate: date,
                  status: AttendanceStatus.Present,
                  checkInTime:
                      this.punchKind === 'in' ? timeIso : existing.checkInTime,
                  checkOutTime:
                      this.punchKind === 'out' ? timeIso : existing.checkOutTime,
              })
            : this.attendanceService.create({
                  staffId: row.staffId,
                  attendanceDate: date,
                  status: AttendanceStatus.Present,
                  checkInTime: timeIso,
                  checkOutTime: null,
              });

        request
            .then(() => {
                this.messageService.add({
                    severity: 'success',
                    summary: 'Saved',
                    detail:
                        this.punchKind === 'out'
                            ? 'Check out saved'
                            : 'Check in saved',
                });
                this.punchVisible = false;
                this.loadAllEmployees();
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to save attendance',
                });
            })
            .finally(() => {
                this.punchSaving = false;
            });
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

    dutyHours(row: StaffAttendanceRow): number | null {
        const att = row.attendance;
        if (!att?.checkInTime || !att.checkOutTime) {
            return null;
        }
        const start = new Date(att.checkInTime).getTime();
        const end = new Date(att.checkOutTime).getTime();
        if (!isFinite(start) || !isFinite(end) || end < start) {
            const stored = Number(att.workingHours);
            return Number.isFinite(stored) && stored > 0 ? stored : null;
        }
        return (end - start) / 3600000;
    }

    formatDuty(row: StaffAttendanceRow): string {
        return this.formatHours(this.dutyHours(row));
    }

    get totalDutyLabel(): string {
        const total = this.rows.reduce(
            (sum, row) => sum + (this.dutyHours(row) || 0),
            0
        );
        return total > 0 ? this.formatHours(total) : '—';
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

    private formatHours(hours: number | null): string {
        if (hours == null || hours <= 0) {
            return '—';
        }
        const totalMinutes = Math.round(hours * 60);
        const wholeHours = Math.floor(totalMinutes / 60);
        const minutes = totalMinutes % 60;
        if (wholeHours <= 0) {
            return `${minutes}m`;
        }
        if (minutes === 0) {
            return `${wholeHours}h`;
        }
        return `${wholeHours}h ${minutes}m`;
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

    private nowTime(): string {
        const now = new Date();
        return `${`${now.getHours()}`.padStart(2, '0')}:${`${now.getMinutes()}`.padStart(2, '0')}`;
    }

    private toTimeInputValue(value?: string | Date | null): string | null {
        if (!value) {
            return null;
        }
        const date = new Date(value);
        if (isNaN(date.getTime())) {
            return null;
        }
        return `${`${date.getHours()}`.padStart(2, '0')}:${`${date.getMinutes()}`.padStart(2, '0')}`;
    }

    private toDateTimeIso(dateValue: string, timeValue: string): string {
        return new Date(`${dateValue}T${timeValue}:00`).toISOString();
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
