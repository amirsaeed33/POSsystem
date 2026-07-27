import { Component, OnInit } from '@angular/core';
import {
    AbstractControl,
    FormBuilder,
    FormGroup,
    ValidationErrors,
    Validators,
} from '@angular/forms';
import { Router } from '@angular/router';
import { MessageService } from 'primeng/api';
import { PASSWORD_COMPLEXITY_PATTERN } from 'src/app/demo/api/user-management';
import { AuthService } from 'src/app/demo/service/auth.service';
import { UserService } from 'src/app/demo/service/user.service';
import { LayoutService } from 'src/app/layout/service/app.layout.service';

@Component({
    templateUrl: './changepassword.component.html',
    providers: [MessageService],
})
export class ChangePasswordComponent implements OnInit {
    form: FormGroup;
    saving = false;

    constructor(
        private layoutService: LayoutService,
        private fb: FormBuilder,
        private userService: UserService,
        private authService: AuthService,
        private router: Router,
        private messageService: MessageService
    ) {
        this.form = this.fb.group(
            {
                currentPassword: [
                    '',
                    [
                        Validators.required,
                        Validators.minLength(2),
                        Validators.maxLength(32),
                    ],
                ],
                newPassword: [
                    '',
                    [
                        Validators.required,
                        Validators.minLength(2),
                        Validators.maxLength(32),
                        Validators.pattern(PASSWORD_COMPLEXITY_PATTERN),
                    ],
                ],
                confirmNewPassword: [
                    '',
                    [
                        Validators.required,
                        Validators.minLength(2),
                        Validators.maxLength(32),
                    ],
                ],
            },
            { validators: this.passwordsMatchValidator }
        );
    }

    ngOnInit(): void {
        if (!this.authService.isAuthenticated()) {
            this.router.navigate(['/auth/login'], {
                queryParams: { returnUrl: '/auth/changepassword' },
            });
        }
    }

    get filledInput(): boolean {
        return this.layoutService.config().inputStyle === 'filled';
    }

    onSubmit(): void {
        if (this.form.invalid) {
            this.form.markAllAsTouched();
            return;
        }

        this.saving = true;
        this.userService
            .changePassword({
                currentPassword: this.form.value.currentPassword,
                newPassword: this.form.value.newPassword,
            })
            .then((success) => {
                if (success) {
                    this.messageService.add({
                        severity: 'success',
                        summary: 'Success',
                        detail: 'Password changed successfully',
                    });
                    this.router.navigate(['/']);
                }
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.message || 'Failed to change password',
                });
            })
            .finally(() => {
                this.saving = false;
            });
    }

    private passwordsMatchValidator(
        group: AbstractControl
    ): ValidationErrors | null {
        const newPassword = group.get('newPassword')?.value;
        const confirm = group.get('confirmNewPassword')?.value;
        if (!confirm) {
            return null;
        }
        return newPassword === confirm ? null : { passwordsDoNotMatch: true };
    }
}
