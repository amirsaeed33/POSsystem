import { Component, Input, OnInit } from '@angular/core';
import { MessageService } from 'primeng/api';
import {
    AttendanceStatus,
    DailyAttendanceSheetItemDto,
    MonthlyAttendanceSheetDto,
} from 'src/app/demo/api/staff-attendance';
import { AppFormatService } from 'src/app/demo/service/app-format.service';
import { StaffAttendanceService } from 'src/app/demo/service/staff-attendance.service';
import { StaffService } from 'src/app/demo/service/staff.service';

@Component({
    selector: 'app-staff-monthly-attendance-list',
    templateUrl: './staff-monthly-attendance-list.component.html',
    providers: [MessageService],
})
export class StaffMonthlyAttendanceListComponent implements OnInit {
    /** When true, omit outer card wrapper (used inside Staff hub tabs). */
    @Input() embedded = false;

    staffOptions: { id: number; name: string }[] = [];
    selectedStaffId: number | null = null;
    selectedMonth = this.toMonthInputValue();
    loading = false;
    loadingStaff = false;
    sheet: MonthlyAttendanceSheetDto | null = null;

    constructor(
        public formatService: AppFormatService,
        private attendanceService: StaffAttendanceService,
        private staffService: StaffService,
        private messageService: MessageService
    ) {}

    ngOnInit(): void {
        this.loadStaffOptions();
    }

    get days(): DailyAttendanceSheetItemDto[] {
        return this.sheet?.days || [];
    }

    loadStaffOptions(): void {
        this.loadingStaff = true;
        this.staffService
            .getAll({ isActive: true, skipCount: 0, maxResultCount: 500 })
            .then((result) => {
                this.staffOptions = (result.items || []).map((s) => ({
                    id: s.id,
                    name: s.name || `Staff #${s.id}`,
                }));
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to load staff list',
                });
                this.staffOptions = [];
            })
            .finally(() => {
                this.loadingStaff = false;
            });
    }

    onStaffChange(): void {
        this.loadMonthly();
    }

    onMonthChange(): void {
        this.loadMonthly();
    }

    onSearch(): void {
        this.loadMonthly();
    }

    loadMonthly(): void {
        if (!this.selectedStaffId) {
            this.sheet = null;
            return;
        }

        const month = (this.selectedMonth || this.toMonthInputValue()).slice(0, 7);
        this.loading = true;
        this.attendanceService
            .getMonthlySheet({
                staffId: this.selectedStaffId,
                month,
            })
            .then((sheet) => {
                this.sheet = sheet;
            })
            .catch((error) => {
                this.sheet = null;
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to load monthly attendance',
                });
            })
            .finally(() => {
                this.loading = false;
            });
    }

    statusLabel(status?: AttendanceStatus | null, hasAttendance?: boolean): string {
        if (!hasAttendance || status === null || status === undefined) {
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

    formatDayName(value?: string | Date | null): string {
        const key =
            typeof value === 'string' && /^\d{4}-\d{2}-\d{2}/.test(value.trim())
                ? value.trim().slice(0, 10)
                : null;
        const date = key
            ? new Date(+key.slice(0, 4), +key.slice(5, 7) - 1, +key.slice(8, 10))
            : value
              ? new Date(value)
              : null;
        if (!date || isNaN(date.getTime())) {
            return '—';
        }
        return date.toLocaleDateString(undefined, { weekday: 'short' });
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

    private toMonthInputValue(value?: string | Date | null): string {
        if (typeof value === 'string' && /^\d{4}-\d{2}/.test(value.trim())) {
            return value.trim().slice(0, 7);
        }
        const date = value ? new Date(value) : new Date();
        if (isNaN(date.getTime())) {
            const now = new Date();
            return `${now.getFullYear()}-${`${now.getMonth() + 1}`.padStart(2, '0')}`;
        }
        return `${date.getFullYear()}-${`${date.getMonth() + 1}`.padStart(2, '0')}`;
    }
}
