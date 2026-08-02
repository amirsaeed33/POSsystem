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
import { BranchService } from 'src/app/demo/service/branch.service';

@Component({
    selector: 'app-branch-form-dialog',
    templateUrl: './branch-form-dialog.component.html',
})
export class BranchFormDialogComponent implements OnChanges {
    @Input() visible = false;
    @Input() branchId: number | null = null;
    @Output() visibleChange = new EventEmitter<boolean>();
    @Output() saved = new EventEmitter<void>();

    branch: BranchDto = {
        id: 0,
        name: '',
        code: '',
        isActive: true,
        isDefault: false,
    };
    saving = false;
    loading = false;

    constructor(
        private branchService: BranchService,
        private messageService: MessageService
    ) {}

    get dialogTitle(): string {
        return this.branchId ? 'Edit Branch' : 'Create Branch';
    }

    ngOnChanges(changes: SimpleChanges): void {
        if (changes['visible'] && this.visible) {
            this.resetForm();
            if (this.branchId) {
                this.loadBranch(this.branchId);
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
        const name = (this.branch.name || '').trim();
        const code = (this.branch.code || '').trim().toUpperCase();
        if (!name || !code) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Validation',
                detail: 'Name and code are required',
            });
            return;
        }

        this.saving = true;
        const request = this.branchId
            ? this.branchService.update({
                  id: this.branchId,
                  name,
                  code,
                  isActive: this.branch.isActive,
                  isDefault: this.branch.isDefault,
              })
            : this.branchService.create({
                  name,
                  code,
                  isActive: this.branch.isActive,
              });

        request
            .then(() => {
                this.messageService.add({
                    severity: 'success',
                    summary: 'Success',
                    detail: this.branchId
                        ? 'Branch updated successfully'
                        : 'Branch created successfully',
                });
                this.saved.emit();
                this.onHide();
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to save branch',
                });
            })
            .finally(() => {
                this.saving = false;
            });
    }

    private resetForm(): void {
        this.branch = {
            id: 0,
            name: '',
            code: '',
            isActive: true,
            isDefault: false,
        };
        this.saving = false;
        this.loading = false;
    }

    private loadBranch(id: number): void {
        this.loading = true;
        this.branchService
            .get(id)
            .then((branch) => {
                this.branch = { ...branch };
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to load branch',
                });
                this.onHide();
            })
            .finally(() => {
                this.loading = false;
            });
    }
}
