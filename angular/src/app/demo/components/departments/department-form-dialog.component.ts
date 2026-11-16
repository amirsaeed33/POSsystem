import {
    Component,
    EventEmitter,
    HostListener,
    Input,
    OnChanges,
    Output,
    SimpleChanges,
} from '@angular/core';
import { DepartmentDto } from 'src/app/demo/api/department';
import { DepartmentService } from 'src/app/demo/service/department.service';
import {
    isEntityNameValid,
    validateEntityName,
} from 'src/app/shared/utils/name-validation';
import { MessageService } from 'primeng/api';

@Component({
    selector: 'app-department-form-dialog',
    templateUrl: './department-form-dialog.component.html',
})
export class DepartmentFormDialogComponent implements OnChanges {
    @Input() visible = false;
    @Input() departmentId: number | null = null;
    @Output() visibleChange = new EventEmitter<boolean>();
    @Output() saved = new EventEmitter<void>();

    department: DepartmentDto = { id: 0, name: '', description: '', isActive: true };
    saving = false;
    loading = false;

    @HostListener('window:keydown', ['$event'])
    handleKeyboardEvent(event: KeyboardEvent) {
        if (!this.visible || this.saving || this.loading) {
            return;
        }

        if (
            (event.altKey && (event.key === 's' || event.key === 'S')) ||
            (event.ctrlKey && (event.key === 's' || event.key === 'S'))
        ) {
            event.preventDefault();
            this.save();
        }
    }

    constructor(
        private departmentService: DepartmentService,
        private messageService: MessageService
    ) {}

    get dialogTitle(): string {
        return this.departmentId ? 'Edit Department' : 'Create Department';
    }

    get canSave(): boolean {
        return isEntityNameValid(this.department?.name);
    }

    ngOnChanges(changes: SimpleChanges): void {
        if (changes['visible'] && this.visible) {
            this.resetForm();
            if (this.departmentId) {
                this.loadDepartment(this.departmentId);
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
        if (this.saving) {
            return;
        }

        const name = (this.department.name || '').trim();
        const nameError = validateEntityName(name, 'Department name');
        if (nameError) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Validation',
                detail: nameError,
            });
            return;
        }

        this.saving = true;
        const request = this.departmentId
            ? this.departmentService.update({
                  id: this.departmentId,
                  branchId: this.department.branchId,
                  name,
                  description: this.department.description?.trim() || undefined,
                  isActive: this.department.isActive ?? true,
              })
            : this.departmentService.create({
                  name,
                  description: this.department.description?.trim() || undefined,
                  isActive: this.department.isActive ?? true,
              });

        request
            .then(() => {
                this.messageService.add({
                    severity: 'success',
                    summary: 'Success',
                    detail: this.departmentId
                        ? 'Department updated successfully'
                        : 'Department created successfully',
                });
                this.saved.emit();
                this.onHide();
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to save department',
                });
            })
            .finally(() => {
                this.saving = false;
            });
    }

    private loadDepartment(id: number): void {
        this.loading = true;
        this.departmentService
            .get(id)
            .then((dep) => {
                this.department = { ...dep };
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to load department',
                });
                this.onHide();
            })
            .finally(() => {
                this.loading = false;
            });
    }

    private resetForm(): void {
        this.department = {
            id: 0,
            name: '',
            description: '',
            isActive: true,
        };
        this.saving = false;
        this.loading = false;
    }
}

