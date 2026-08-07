import {
    Component,
    EventEmitter,
    Input,
    OnChanges,
    Output,
    SimpleChanges,
} from '@angular/core';
import { MessageService } from 'primeng/api';
import { BranchDto } from 'src/app/demo/api/branch';
import { StaffDto } from 'src/app/demo/api/staff';
import { BranchService } from 'src/app/demo/service/branch.service';
import { BranchContextService } from 'src/app/demo/service/branch-context.service';
import { StaffService } from 'src/app/demo/service/staff.service';

@Component({
    selector: 'app-staff-form-dialog',
    templateUrl: './staff-form-dialog.component.html',
})
export class StaffFormDialogComponent implements OnChanges {
    @Input() visible = false;
    @Input() staffId: number | null = null;
    @Output() visibleChange = new EventEmitter<boolean>();
    @Output() saved = new EventEmitter<void>();

    staff: StaffDto = this.emptyStaff();
    branches: BranchDto[] = [];
    saving = false;
    loading = false;

    constructor(
        private staffService: StaffService,
        private branchService: BranchService,
        private branchContext: BranchContextService,
        private messageService: MessageService
    ) {}

    get dialogTitle(): string {
        return this.staffId ? 'Edit Staff' : 'Create Staff';
    }

    ngOnChanges(changes: SimpleChanges): void {
        if (changes['visible'] && this.visible) {
            this.resetForm();
            this.loadBranches();
            if (this.staffId) {
                this.loadStaff(this.staffId);
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
        const name = (this.staff.name || '').trim();
        if (!name) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Validation',
                detail: 'Name is required',
            });
            return;
        }

        if (!this.staff.joiningDate) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Validation',
                detail: 'Joining date is required',
            });
            return;
        }

        this.saving = true;
        const payload = {
            name,
            branchId: this.staff.branchId ?? null,
            employeeCode: this.staff.employeeCode?.trim() || undefined,
            phone: this.staff.phone?.trim() || undefined,
            email: this.staff.email?.trim() || undefined,
            address: this.staff.address?.trim() || undefined,
            designation: this.staff.designation?.trim() || undefined,
            joiningDate: this.staff.joiningDate,
            basicSalary: this.staff.basicSalary ?? null,
            isActive: this.staff.isActive ?? true,
        };

        const request = this.staffId
            ? this.staffService.update({
                  ...this.staff,
                  ...payload,
                  id: this.staffId,
              })
            : this.staffService.create(payload);

        request
            .then(() => {
                this.messageService.add({
                    severity: 'success',
                    summary: 'Success',
                    detail: this.staffId
                        ? 'Staff updated successfully'
                        : 'Staff created successfully',
                });
                this.saved.emit();
                this.onHide();
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to save staff',
                });
            })
            .finally(() => {
                this.saving = false;
            });
    }

    private emptyStaff(): StaffDto {
        return {
            id: 0,
            name: '',
            employeeCode: '',
            phone: '',
            email: '',
            address: '',
            designation: '',
            joiningDate: this.toDateInputValue(),
            basicSalary: null,
            branchId: null,
            isActive: true,
        };
    }

    private resetForm(): void {
        this.staff = this.emptyStaff();
        this.saving = false;
        this.loading = false;
    }

    private loadBranches(): void {
        this.branchService
            .getLookup()
            .then((branches) => {
                this.branches = branches || [];
                if (!this.staffId) {
                    this.selectDefaultBranch();
                }
            })
            .catch(() => {
                this.branches = [];
            });
    }

    /** Pre-select current / default location when creating staff. */
    private selectDefaultBranch(): void {
        if (!this.branches?.length || this.staff.branchId) {
            return;
        }

        const currentId = this.branchContext.getBranchId();
        const selected =
            this.branches.find((b) => b.id === currentId) ||
            this.branches.find((b) => b.isDefault) ||
            this.branches[0];

        if (selected?.id) {
            this.staff.branchId = selected.id;
        }
    }

    private loadStaff(id: number): void {
        this.loading = true;
        this.staffService
            .get(id)
            .then((staff) => {
                this.staff = {
                    ...staff,
                    joiningDate: this.toDateInputValue(staff.joiningDate),
                };
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to load staff',
                });
                this.onHide();
            })
            .finally(() => {
                this.loading = false;
            });
    }

    private toDateInputValue(value?: string | Date): string {
        const date = value ? new Date(value) : new Date();
        if (isNaN(date.getTime())) {
            return this.toDateInputValue();
        }
        const year = date.getFullYear();
        const month = `${date.getMonth() + 1}`.padStart(2, '0');
        const day = `${date.getDate()}`.padStart(2, '0');
        return `${year}-${month}-${day}`;
    }
}
