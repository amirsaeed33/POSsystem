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
import { StaffService } from 'src/app/demo/service/staff.service';

@Component({
    selector: 'app-staff-login-dialog',
    templateUrl: './staff-login-dialog.component.html',
})
export class StaffLoginDialogComponent implements OnChanges {
    @Input() visible = false;
    @Input() staff: StaffDto | null = null;
    @Output() visibleChange = new EventEmitter<boolean>();
    @Output() saved = new EventEmitter<void>();

    email = '';
    password = '';
    saving = false;

    constructor(
        private staffService: StaffService,
        private messageService: MessageService
    ) {}

    get hasUserAccount(): boolean {
        return !!(this.staff?.userId || this.staff?.hasUserAccount);
    }

    get dialogTitle(): string {
        return this.hasUserAccount
            ? 'Change Staff Password'
            : 'Create Staff Login';
    }

    ngOnChanges(changes: SimpleChanges): void {
        if (changes['visible'] && this.visible) {
            this.email = this.staff?.email?.trim() || '';
            this.password = '';
            this.saving = false;
        }
    }

    onVisibleChange(visible: boolean): void {
        this.visible = visible;
        this.visibleChange.emit(visible);
    }

    onHide(): void {
        this.onVisibleChange(false);
    }

    get canSave(): boolean {
        if (this.saving || !this.staff?.id) {
            return false;
        }
        if (this.hasUserAccount) {
            return (this.password || '').trim().length >= 6;
        }
        return (
            !!(this.email || '').trim() &&
            (this.password || '').trim().length >= 6
        );
    }

    save(): void {
        if (!this.canSave || !this.staff?.id) {
            return;
        }

        this.saving = true;
        const request = this.hasUserAccount
            ? this.staffService.changeLoginPassword({
                  staffId: this.staff.id,
                  newPassword: this.password.trim(),
              })
            : this.staffService.createLogin({
                  staffId: this.staff.id,
                  email: this.email.trim(),
                  password: this.password.trim(),
              });

        request
            .then(() => {
                this.messageService.add({
                    severity: 'success',
                    summary: 'Success',
                    detail: this.hasUserAccount
                        ? 'Password updated successfully'
                        : 'Login account created successfully',
                });
                this.saved.emit();
                this.onHide();
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail:
                        error?.message ||
                        (this.hasUserAccount
                            ? 'Failed to change password'
                            : 'Failed to create login'),
                });
            })
            .finally(() => {
                this.saving = false;
            });
    }
}
