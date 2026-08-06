import { animate, style, transition, trigger } from '@angular/animations';
import { Component, OnDestroy, OnInit } from '@angular/core';
import { AbstractControl, FormBuilder, FormGroup, ValidationErrors, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { MessageService } from 'primeng/api';
import { Subscription } from 'rxjs';
import { LayoutService } from 'src/app/layout/service/app.layout.service';
import { AccountService } from 'src/app/demo/service/account.service';
import { AuthService } from 'src/app/demo/service/auth.service';
import { PermissionService } from 'src/app/demo/service/permission.service';
import { SessionService } from 'src/app/demo/service/session.service';
import { TenantContextService } from 'src/app/demo/service/tenant-context.service';

@Component({
    templateUrl: './signup.component.html',
    providers: [MessageService],
    animations: [
        trigger('slideIn', [
            transition(':enter', [
                style({ transform: 'translateX(48px)', opacity: 0 }),
                animate(
                    '420ms cubic-bezier(0.22, 1, 0.36, 1)',
                    style({ transform: 'translateX(0)', opacity: 1 })
                ),
            ]),
        ]),
    ],
})
export class SignupComponent implements OnInit, OnDestroy {
    signupForm: FormGroup;
    loading = false;
    private tenancyManuallyEdited = false;
    private nameSub?: Subscription;

    constructor(
        private layoutService: LayoutService,
        private fb: FormBuilder,
        private accountService: AccountService,
        private authService: AuthService,
        private sessionService: SessionService,
        private permissionService: PermissionService,
        private tenantContext: TenantContextService,
        private router: Router,
        private messageService: MessageService
    ) {
        this.signupForm = this.fb.group(
            {
                tenancyName: [
                    '',
                    [
                        Validators.required,
                        Validators.pattern(/^[a-zA-Z][a-zA-Z0-9_-]{1,}$/),
                        Validators.maxLength(64),
                    ],
                ],
                name: ['', [Validators.required, Validators.maxLength(128)]],
                adminName: ['', [Validators.required, Validators.maxLength(64)]],
                adminSurname: ['', [Validators.required, Validators.maxLength(64)]],
                adminEmailAddress: ['', [Validators.required, Validators.email]],
                adminUserName: ['admin', [Validators.required, Validators.maxLength(256)]],
                adminPassword: ['', [Validators.required, Validators.minLength(6)]],
                confirmPassword: ['', [Validators.required]],
            },
            { validators: this.passwordMatchValidator }
        );
    }

    ngOnInit(): void {
        // Signup creates a new tenant — clear any previously selected tenant cookie.
        this.tenantContext.setTenantId(null);
        this.tenantContext.setTenantInfo(null);

        this.nameSub = this.signupForm.get('name')?.valueChanges.subscribe((name) => {
            if (this.tenancyManuallyEdited) {
                return;
            }
            this.signupForm.get('tenancyName')?.setValue(this.toTenancyName(name || ''), {
                emitEvent: false,
            });
        });

        if (this.authService.isAuthenticated()) {
            this.router.navigateByUrl('/');
        }
    }

    ngOnDestroy(): void {
        this.nameSub?.unsubscribe();
    }

    get filledInput(): boolean {
        return this.layoutService.config().inputStyle === 'filled';
    }

    onTenancyNameInput(): void {
        this.tenancyManuallyEdited = true;
    }

    private toTenancyName(displayName: string): string {
        // Keep letters/digits/_/- only; strip leading non-letters so it matches tenancy rules.
        const cleaned = (displayName || '')
            .trim()
            .replace(/[^a-zA-Z0-9_-]+/g, '')
            .replace(/^[^a-zA-Z]+/, '');
        return cleaned.substring(0, 64);
    }

    onSubmit(): void {
        if (this.signupForm.invalid) {
            this.signupForm.markAllAsTouched();
            return;
        }

        this.loading = true;
        const value = this.signupForm.value;

        this.accountService
            .signUpTenant({
                tenancyName: value.tenancyName,
                name: value.name,
                adminName: value.adminName,
                adminSurname: value.adminSurname,
                adminEmailAddress: value.adminEmailAddress,
                adminUserName: value.adminUserName,
                adminPassword: value.adminPassword,
            })
            .then(async (result) => {
                this.tenantContext.setTenantId(result.tenantId);
                this.tenantContext.setTenantInfo({
                    id: result.tenantId,
                    tenancyName: result.tenancyName,
                    name: result.name,
                });

                await this.authService.authenticate({
                    userNameOrEmailAddress: result.adminUserName,
                    password: value.adminPassword,
                    rememberClient: true,
                });

                const sessionInfo = await this.sessionService.getCurrentLoginInformations();
                if (sessionInfo?.user) {
                    this.authService.setUserInfo(sessionInfo.user);
                }
                if (sessionInfo?.tenant) {
                    this.tenantContext.setTenantInfo(sessionInfo.tenant);
                    if (sessionInfo.tenant.id) {
                        this.tenantContext.setTenantId(sessionInfo.tenant.id);
                    }
                }

                await this.permissionService.load();
                this.messageService.add({
                    severity: 'success',
                    summary: 'Welcome',
                    detail: `Tenant "${result.tenancyName}" created. You are signed in.`,
                });
                this.router.navigateByUrl('/');
            })
            .catch((error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Signup failed',
                    detail: error?.message || 'Could not create your account.',
                });
            })
            .finally(() => {
                this.loading = false;
            });
    }

    private passwordMatchValidator(group: AbstractControl): ValidationErrors | null {
        const password = group.get('adminPassword')?.value;
        const confirm = group.get('confirmPassword')?.value;
        if (!password || !confirm) {
            return null;
        }
        return password === confirm ? null : { passwordMismatch: true };
    }
}
