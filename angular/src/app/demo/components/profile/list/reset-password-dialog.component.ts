import {
    Component,
    EventEmitter,
    Input,
    OnChanges,
    Output,
    SimpleChanges,
} from '@angular/core';
import { MessageService } from 'primeng/api';
import { UserService } from 'src/app/demo/service/user.service';

@Component({
    selector: 'app-reset-password-dialog',
    templateUrl: './reset-password-dialog.component.html',
})
export class ResetPasswordDialogComponent implements OnChanges {
    @Input() visible = false;
    @Input() userId: number | null = null;
    @Input() userName = '';
    @Output() visibleChange = new EventEmitter<boolean>();
    @Output() reset = new EventEmitter<void>();

    saving = false;
    adminPassword = '';
    newPassword = '';

    constructor(
        private userService: UserService,
        private messageService: MessageService
    ) {}

    ngOnChanges(changes: SimpleChanges): void {
        if (changes['visible'] && this.visible) {
            this.adminPassword = '';
            this.newPassword = Math.random().toString(36).substr(2, 10);
        }
    }

    onVisibleChange(visible: boolean): void {
        this.visible = visible;
        this.visibleChange.emit(visible);
        if (!visible) {
            this.adminPassword = '';
            this.saving = false;
        }
    }

    onHide(): void {
        this.onVisibleChange(false);
    }

    get canSave(): boolean {
        return (
            !this.saving &&
            !!this.userId &&
            !!this.adminPassword?.trim() &&
            !!this.newPassword
        );
    }

    save(): void {
        if (!this.canSave || !this.userId) {
            return;
        }

        this.saving = true;
        this.userService
            .resetPassword({
                adminPassword: this.adminPassword,
                userId: this.userId,
                newPassword: this.newPassword,
            })
            .then(() => {
                this.messageService.add({
                    severity: 'success',
                    summary: 'Success',
                    detail: 'Password reset',
                });
                this.reset.emit();
                this.onHide();
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to reset password',
                });
            })
            .finally(() => {
                this.saving = false;
            });
    }
}
