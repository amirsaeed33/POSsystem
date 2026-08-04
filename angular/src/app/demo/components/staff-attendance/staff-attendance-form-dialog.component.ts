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
    AttendanceStatus,
    StaffAttendanceDto,
} from 'src/app/demo/api/staff-attendance';
import { StaffAttendanceService } from 'src/app/demo/service/staff-attendance.service';
import { StaffService } from 'src/app/demo/service/staff.service';

@Component({
    selector: 'app-staff-attendance-form-dialog',
    templateUrl: './staff-attendance-form-dialog.component.html',
})
export class StaffAttendanceFormDialogComponent implements OnChanges {
    @Input() visible = false;
    @Input() attendanceId: number | null = null;
    @Output() visibleChange = new EventEmitter<boolean>();
    @Output() saved = new EventEmitter<void>();

    record: StaffAttendanceDto = this.emptyRecord();
    staffOptions: StaffDto[] = [];
    saving = false;
    loading = false;

    statusOptions = [
        { label: 'Present', value: AttendanceStatus.Present },
        { label: 'Absent', value: AttendanceStatus.Absent },
        { label: 'Leave', value: AttendanceStatus.Leave },
        { label: 'Half Day', value: AttendanceStatus.HalfDay },
    ];

    constructor(
        private attendanceService: StaffAttendanceService,
        private staffService: StaffService,
        private messageService: MessageService
    ) {}

    get dialogTitle(): string {
        return this.attendanceId ? 'Edit Attendance' : 'Create Attendance';
    }

    get timesDisabled(): boolean {
        return (
            this.record.status === AttendanceStatus.Absent ||
            this.record.status === AttendanceStatus.Leave ||
            this.saving
        );
    }

    ngOnChanges(changes: SimpleChanges): void {
        if (changes['visible'] && this.visible) {
            this.resetForm();
            this.loadStaffOptions();
            if (this.attendanceId) {
                this.loadRecord(this.attendanceId);
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

    onStatusChange(): void {
        if (
            this.record.status === AttendanceStatus.Absent ||
            this.record.status === AttendanceStatus.Leave
        ) {
            this.record.checkInTime = null;
            this.record.checkOutTime = null;
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

        if (!this.record.attendanceDate) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Validation',
                detail: 'Attendance date is required',
            });
            return;
        }

        if (
            this.record.status === AttendanceStatus.Present &&
            !this.record.checkInTime
        ) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Validation',
                detail: 'Check-in time is required for Present status',
            });
            return;
        }

        this.saving = true;
        const payload = {
            staffId: this.record.staffId,
            attendanceDate: this.record.attendanceDate,
            checkInTime: this.toDateTimeIso(
                this.record.attendanceDate,
                this.record.checkInTime as string | null
            ),
            checkOutTime: this.toDateTimeIso(
                this.record.attendanceDate,
                this.record.checkOutTime as string | null
            ),
            status: this.record.status,
            remarks: this.record.remarks?.trim() || undefined,
        };

        const request = this.attendanceId
            ? this.attendanceService.update({
                  ...this.record,
                  ...payload,
                  id: this.attendanceId,
              })
            : this.attendanceService.create(payload);

        request
            .then(() => {
                this.messageService.add({
                    severity: 'success',
                    summary: 'Success',
                    detail: this.attendanceId
                        ? 'Attendance updated successfully'
                        : 'Attendance created successfully',
                });
                this.saved.emit();
                this.onHide();
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to save attendance',
                });
            })
            .finally(() => {
                this.saving = false;
            });
    }

    private emptyRecord(): StaffAttendanceDto {
        return {
            id: 0,
            branchId: 0,
            staffId: null as any,
            attendanceDate: this.toDateInputValue(),
            checkInTime: null,
            checkOutTime: null,
            status: AttendanceStatus.Present,
            workingHours: null,
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
        this.attendanceService
            .get(id)
            .then((record) => {
                this.record = {
                    ...record,
                    attendanceDate: this.toDateInputValue(record.attendanceDate),
                    checkInTime: this.toTimeInputValue(record.checkInTime),
                    checkOutTime: this.toTimeInputValue(record.checkOutTime),
                };
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to load attendance',
                });
                this.onHide();
            })
            .finally(() => {
                this.loading = false;
            });
    }

    private toDateInputValue(value?: string | Date | null): string {
        const date = value ? new Date(value) : new Date();
        if (isNaN(date.getTime())) {
            return this.toDateInputValue();
        }
        const year = date.getFullYear();
        const month = `${date.getMonth() + 1}`.padStart(2, '0');
        const day = `${date.getDate()}`.padStart(2, '0');
        return `${year}-${month}-${day}`;
    }

    private toTimeInputValue(value?: string | Date | null): string | null {
        if (!value) {
            return null;
        }
        const date = new Date(value);
        if (isNaN(date.getTime())) {
            return null;
        }
        const hours = `${date.getHours()}`.padStart(2, '0');
        const minutes = `${date.getMinutes()}`.padStart(2, '0');
        return `${hours}:${minutes}`;
    }

    private toDateTimeIso(
        dateValue: string | Date,
        timeValue?: string | null
    ): string | null {
        if (!timeValue) {
            return null;
        }
        const datePart = this.toDateInputValue(dateValue);
        return new Date(`${datePart}T${timeValue}:00`).toISOString();
    }
}
